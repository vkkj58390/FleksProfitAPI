using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace FleksProfitAPI.Services
{
    public abstract class EnergiNetBaseService
    {
        protected readonly HttpClient _httpClient;

        protected EnergiNetBaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected async Task<List<T>> FetchDataAsync<T>(string datasetName, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            // Page through the Energi Data Service API. limit=0 returns 0 rows.
            const int pageSize = 10000;
            var offset = 0;
            var all = new List<T>();

            while (true)
            {
                var url =
                    $"https://api.energidataservice.dk/dataset/{datasetName}" +
                    $"?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}" +
                    $"&limit={pageSize}&offset={offset}&timezone=utc";

                using var resp = await _httpClient.GetAsync(url, cancellationToken);
                resp.EnsureSuccessStatusCode();

                var payload = await resp.Content.ReadFromJsonAsync<Models.EnergiNetResponse<T>>(cancellationToken: cancellationToken);
                var batch = payload?.Records ?? new List<T>();

                if (batch.Count == 0)
                    break;

                all.AddRange(batch);
                offset += batch.Count;

                if (batch.Count < pageSize)
                    break;
            }

            return all;
        }
    }
}
