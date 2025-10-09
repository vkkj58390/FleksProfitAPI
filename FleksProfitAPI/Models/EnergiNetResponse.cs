using System.Collections.Generic;

namespace FleksProfitAPI.Models
{
    public class EnergiNetResponse<T>
    {
        public int Total { get; set; }
        public int Limit { get; set; }
        public List<T> Records { get; set; } = new List<T>();
    }
}
