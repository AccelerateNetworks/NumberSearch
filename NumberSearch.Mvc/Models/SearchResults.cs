using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Models
{
    public class SearchResults
    {
        public string Query { get; set; }
        public IEnumerable<LocalNumber.Did> Dids { get; set; }
        public int Count { get; set; }

        public static async Task<SearchResults> GetAsync(string query, Guid token)
        {
            var results = await LocalNumber.GetAsync(query, token);

            return new SearchResults
            {
                Query = query,
                Dids = results.data.dids,
                Count = results.data.count
            };
        }
    }
}
