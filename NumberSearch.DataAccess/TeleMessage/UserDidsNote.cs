using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleMesssage
{
    public class UserDidsNote
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public string data { get; set; }

        public static async Task<UserDidsNote> SetNote(string note, string didId, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/note";
            string tokenParameter = $"?token={token}";
            string didIdParameter = $"&did_id={didId}";
            string noteParameter = $"&note={note}";
            //string typeParameter = $"&type=true";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{didIdParameter}{noteParameter}";
            return await route.GetJsonAsync<UserDidsNote>().ConfigureAwait(false);
        }
    }
}
