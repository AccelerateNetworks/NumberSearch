using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    /// <summary>
    /// An address where a provider has told us a product can be sold, loaded from their building lists by Ziply/import_ziply_building_list.py.
    /// </summary>
    public class ServiceAddress
    {
        public long ServiceAddressId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string StreetKey { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Postal { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MaxSpeed { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; }

        private const string Columns = "\"ServiceAddressId\", \"Provider\", \"Product\", \"Status\", \"HouseNumber\", \"StreetKey\", \"StreetAddress\", \"City\", \"State\", \"Postal\", \"Latitude\", \"Longitude\", \"MaxSpeed\", \"SourceFile\", \"DateIngested\"";

        /// <summary>
        /// How far a geocoded point may be from a listed building and still count as that building, when the street address doesn't match.
        /// </summary>
        public const double MaxDistanceMeters = 30;

        /// <summary>
        /// Lowercase the street name and drop everything but letters and digits, ex. "Pine Cliff" -> "pinecliff". Must match street_key() in the import script.
        /// </summary>
        public static string ToStreetKey(string streetName)
        {
            var key = new StringBuilder(streetName.Length);
            foreach (var c in streetName)
            {
                if (char.IsAsciiLetterOrDigit(c))
                {
                    key.Append(char.ToLowerInvariant(c));
                }
            }
            return key.ToString();
        }

        /// <summary>
        /// Find the listed addresses matching a street address, or failing that the nearest listed building within MaxDistanceMeters of the point.
        /// </summary>
        public static async Task<IEnumerable<ServiceAddress>> LookupAsync(string houseNumber, string streetName, string postal, double latitude, double longitude, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var streetKey = ToStreetKey(streetName);
            if (!string.IsNullOrWhiteSpace(houseNumber) && !string.IsNullOrWhiteSpace(streetKey) && !string.IsNullOrWhiteSpace(postal))
            {
                var matches = await connection
                    .QueryAsync<ServiceAddress>($"SELECT {Columns} FROM public.\"ServiceAddresses\" " +
                    "WHERE \"Postal\" = @postal AND \"HouseNumber\" = @houseNumber AND \"StreetKey\" = @streetKey",
                    new { postal = postal.Trim(), houseNumber = houseNumber.Trim(), streetKey })
                    .ConfigureAwait(false);

                if (matches.Any())
                {
                    return matches;
                }
            }

            if (latitude is 0 && longitude is 0)
            {
                return [];
            }

            // Search a small box around the point using the index, then keep only the closest building's rows.
            var latDelta = MaxDistanceMeters / 111_320d;
            var lonDelta = MaxDistanceMeters / (111_320d * Math.Cos(latitude * Math.PI / 180));
            var nearby = await connection
                .QueryAsync<ServiceAddress>($"SELECT {Columns} FROM public.\"ServiceAddresses\" " +
                "WHERE \"Latitude\" BETWEEN @minLat AND @maxLat AND \"Longitude\" BETWEEN @minLon AND @maxLon",
                new { minLat = latitude - latDelta, maxLat = latitude + latDelta, minLon = longitude - lonDelta, maxLon = longitude + lonDelta })
                .ConfigureAwait(false);

            var closest = nearby
                .Select(x => (Address: x, Meters: DistanceMeters(latitude, longitude, x.Latitude, x.Longitude)))
                .Where(x => x.Meters <= MaxDistanceMeters)
                .OrderBy(x => x.Meters)
                .FirstOrDefault();

            if (closest.Address is null)
            {
                return [];
            }

            return nearby.Where(x => x.Latitude == closest.Address.Latitude && x.Longitude == closest.Address.Longitude);
        }

        /// <summary>
        /// Equirectangular approximation, accurate to well under a meter at these distances.
        /// </summary>
        public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var x = (lon2 - lon1) * Math.PI / 180 * Math.Cos((lat1 + lat2) / 2 * Math.PI / 180);
            var y = (lat2 - lat1) * Math.PI / 180;
            return Math.Sqrt(x * x + y * y) * 6_371_000;
        }
    }
}
