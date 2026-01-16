using FleksProfitAPI.Data;
using FleksProfitAPI.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace FleksProfitAPI.Services
{
    public class StromPriceDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IQuestDbRepository _repo;
        private readonly ILogger<StromPriceDataService> _logger;

        public StromPriceDataService(HttpClient httpClient, IQuestDbRepository repo, ILogger<StromPriceDataService> logger)
        {
            _httpClient = httpClient;
            _repo = repo;
            _logger = logger;
        }

        // Henter elpriser i 30-dages chunks og stopper ved sidste hele UTC time (UtcNow - 1 time).
        public async Task<int> SyncElectricityPricesAsync(string priceArea, DateTime start, DateTime end, CancellationToken ct = default)
        {
            if (start >= end) return 0;

            // Clamp til sidste hele time
            var lastFullHourUtc = DateTime.UtcNow.AddHours(-1);
            lastFullHourUtc = new DateTime(lastFullHourUtc.Year, lastFullHourUtc.Month, lastFullHourUtc.Day, lastFullHourUtc.Hour, 0, 0, DateTimeKind.Utc);
            if (end > lastFullHourUtc) end = lastFullHourUtc;

            // Align start til hel time
            if (start.Minute != 0 || start.Second != 0)
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, DateTimeKind.Utc);

            if (start >= end) return 0;

            int totalInserted = 0;
            var cursor = start;
            const int chunkDays = 30;

            while (cursor < end && !ct.IsCancellationRequested)
            {
                var chunkEnd = cursor.AddDays(chunkDays);
                if (chunkEnd > end) chunkEnd = end;

                // Align chunkEnd til hel time
                if (chunkEnd.Minute != 0 || chunkEnd.Second != 0)
                    chunkEnd = new DateTime(chunkEnd.Year, chunkEnd.Month, chunkEnd.Day, chunkEnd.Hour, 0, 0, DateTimeKind.Utc);

                if (cursor >= chunkEnd)
                {
                    cursor = chunkEnd.AddHours(1);
                    continue;
                }

                var fromParam = cursor.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var toParam = chunkEnd.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var url = $"https://www.stromligning.dk/api/prices?from={fromParam}&to={toParam}&priceArea={priceArea}&aggregation=1h&aggregationMethod=mean";

                _logger.LogInformation("Stromligning chunk request: {Url}", url);

                HttpResponseMessage httpResp;
                try
                {
                    httpResp = await _httpClient.GetAsync(url, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Netværksfejl for chunk {From} - {To}", cursor, chunkEnd);
                    break;
                }

                var body = await httpResp.Content.ReadAsStringAsync(ct);

                if (!httpResp.IsSuccessStatusCode)
                {
                    var status = (int)httpResp.StatusCode;

                    if (status == 429)
                    {
                        _logger.LogWarning("Rate limit (429). Stopper sync nu; fortsætter ved næste cyklus.");
                        break;
                    }

                    _logger.LogWarning("Fejlstatus {Status} for chunk {From}-{To}. Body snippet: {Body}",
                        status, cursor, chunkEnd, body[..Math.Min(body.Length, 300)]);

                    // 400: slut skal ligge før den ufuldstændige buckets start
                    if (status == 400 && body.TrimStart().StartsWith("{"))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.TryGetProperty("details", out var details) &&
                                details.TryGetProperty("lastPriceStart", out var lastPriceStartProp))
                            {
                                var lastStartStr = lastPriceStartProp.GetString();
                                if (DateTime.TryParse(lastStartStr, out var lastStart))
                                {
                                    // Sæt to = (lastPriceStart - 1 sekund) for at ekskludere den ufuldstændige time
                                    var adjustedEnd = lastStart.AddSeconds(-1);
                                    var retryTo = adjustedEnd.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                    var retryUrl = $"https://www.stromligning.dk/api/prices?from={fromParam}&to={retryTo}&priceArea={priceArea}&aggregation=1h&aggregationMethod=mean";
                                    _logger.LogInformation("Retry med justeret slut (før lastPriceStart): {Url}", retryUrl);

                                    var retryResp = await _httpClient.GetAsync(retryUrl, ct);
                                    var retryBody = await retryResp.Content.ReadAsStringAsync(ct);

                                    if (retryResp.IsSuccessStatusCode && retryBody.TrimStart().StartsWith("{"))
                                    {
                                        await ParseAndInsertAsync(retryBody, priceArea, cursor, adjustedEnd, ct);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Retry mislykkedes ({Status}).", (int)retryResp.StatusCode);
                                    }

                                    cursor = adjustedEnd.AddHours(1);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Kunne ikke parse 400-detajler.");
                        }
                    }

                    cursor = chunkEnd.AddHours(1);
                    continue;
                }

                // Success
                if (!body.TrimStart().StartsWith("{"))
                {
                    _logger.LogWarning("Respons ikke JSON for chunk {From}-{To}. Springer.", cursor, chunkEnd);
                    cursor = chunkEnd.AddHours(1);
                    continue;
                }

                await ParseAndInsertAsync(body, priceArea, cursor, chunkEnd, ct);
                cursor = chunkEnd.AddHours(1);
            }

            return totalInserted;

            // lokal helper:
            async Task ParseAndInsertAsync(string json, string area, DateTime chunkStart, DateTime chunkEndIncl, CancellationToken token)
            {
                ResponseStromligningApi? resp;
                try
                {
                    resp = JsonSerializer.Deserialize<ResponseStromligningApi>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JSON parse fejl for chunk {From}-{To}", chunkStart, chunkEndIncl);
                    return;
                }

                if (resp?.Prices == null || resp.Prices.Count == 0)
                {
                    _logger.LogInformation("Ingen priser i chunk {From}-{To}", chunkStart, chunkEndIncl);
                    return;
                }

                var existingLast = await _repo.GetLastElectricityPriceHourUtcAsync(area, token);

                var fresh = resp.Prices
                    .Where(p => existingLast == null || p.Date > existingLast.Value)
                    .Select(p => new ElectricityPriceRecord
                    {
                        HourUTC = p.Date,
                        PriceArea = resp.PriceArea,
                        TotalPriceDKKPerKWh = p.Price.Total, // købspris inkl. alt
                        SpotPriceDKKPerKWh  = p.Details?.Electricity?.Value ?? p.Price.Value // rå spotpris
                    })
                    .OrderBy(r => r.HourUTC)
                    .ToList();

                if (fresh.Count == 0)
                {
                    _logger.LogInformation("Ingen nye rækker i chunk {From}-{To}", chunkStart, chunkEndIncl);
                    return;
                }

                var inserted = await _repo.InsertElectricityPricesAsync(fresh, token);
                totalInserted += inserted; // <- VIGTIGT: opdater totalen
                _logger.LogInformation("Inserted {Count} rows (chunk {From}-{To}) first={First} last={Last}",
                    inserted, chunkStart, chunkEndIncl, fresh.First().HourUTC, fresh.Last().HourUTC);
            }
        }
    }
}