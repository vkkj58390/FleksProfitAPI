using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FleksProfitAPI.Services
{
    public abstract class EnergiNetBaseService
    {
        protected readonly HttpClient _httpClient;

        protected EnergiNetBaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected async Task<List<T>> FetchDataAsync<T>(string datasetName, DateTime start, DateTime end)
        {
            string url = $"https://api.energidataservice.dk/dataset/{datasetName}?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&limit=0";

            var response = await _httpClient.GetFromJsonAsync<Models.EnergiNetResponse<T>>(url);
            return response?.Records ?? new List<T>();
        }
    }
}
