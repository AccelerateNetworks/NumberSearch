using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;

using NumberSearch.DataAccess;

using System.Collections.Generic;
using System.Text;

namespace NumberSearch.Mvc
{
    public static class Cookie
    {
        /// <summary>
        /// Gets all of values out of the Session cookie.
        /// </summary>
        /// <param name="session"> The session cookie. </param>
        /// <returns> A list of items that are in the Cart. </returns>
        public static IEnumerable<ProductOrder> GetCart(ISession session)
        {
            if (session != null && session.TryGetValue("Cart", out var cookie))
            {
                var entries = Encoding.ASCII.GetString(cookie);

                // TODO: Replace the use of Newtonsoft.Json here with System.Text.Json for better performance.
                var items = JsonConvert.DeserializeObject<List<ProductOrder>>(entries);

                return items;
            }
            else
            {
                return new List<ProductOrder>();
            }
        }

        /// <summary>
        /// Puts a list of items into the session cookie.
        /// </summary>
        /// <param name="session"> The session cookie. </param>
        /// <param name="products"> A list of items to put into the cookie. </param>
        /// <returns> Wether or not it was sucessful. </returns>
        public static bool SetCart(ISession session, IEnumerable<ProductOrder> products)
        {
            if (session != null)
            {
                var json = JsonConvert.SerializeObject(products);
                var cart = Encoding.ASCII.GetBytes(json);
                session.Set("Cart", cart);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}