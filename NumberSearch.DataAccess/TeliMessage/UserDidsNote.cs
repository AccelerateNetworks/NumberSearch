using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class UserDidsNote
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public string data { get; set; } = string.Empty;
            
        public static async Task<UserDidsNote> SetNote(string note, string didId, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/note";
            string tokenParameter = $"?token={token}";
            string didIdParameter = $"&did_id={didId}";
            string noteParameter = $"&note={note}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{didIdParameter}{noteParameter}";
            return await route.GetJsonAsync<UserDidsNote>().ConfigureAwait(false);
        }
    }
}
