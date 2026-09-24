using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public string BuildingKey { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; }

        private const string Columns = "\"ServiceAddressId\", \"Provider\", \"Product\", \"Status\", \"HouseNumber\", \"StreetKey\", \"StreetAddress\", \"City\", \"State\", \"Postal\", \"Latitude\", \"Longitude\", \"MaxSpeed\", \"BuildingKey\", \"SourceFile\", \"DateIngested\"";

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
        /// The leading building number of a house number, without leading zeros, ex. "0512" -> "512", "512 1/2" -> "512", "1250A" -> "1250".
        /// Only used when the exact house number doesn't match, because it can't tell 512 from 512 1/2.
        /// </summary>
        public static string ToHouseKey(string houseNumber)
        {
            var digits = Regex.Match(houseNumber.Trim(), @"^\d+").Value.TrimStart('0');
            return digits.Length > 0 ? digits : string.Empty;
        }

        /// <summary>
        /// The download speed in Mbps from a building list speed like "1.0G/1.0G" or "300.0M/300.0M", or 0 when it can't be read.
        /// </summary>
        public static int ParseMbps(string maxSpeed)
        {
            var match = Regex.Match(maxSpeed, @"^\s*(\d+(?:\.\d+)?)\s*([GM])", RegexOptions.IgnoreCase);
            if (!match.Success || !decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return 0;
            }
            return (int)(char.ToUpperInvariant(match.Groups[2].Value[0]) is 'G' ? value * 1000 : value);
        }

        /// <summary>
        /// How a lookup matched the listed addresses. Only an Exact match is precise enough to sell at the listed price.
        /// </summary>
        public enum MatchType { None, Exact, HouseNumber, Nearby }

        public readonly record struct LookupResult(MatchType Match, ServiceAddress[] Addresses);

        public static async Task<ServiceAddress?> GetByIdAsync(long serviceAddressId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<ServiceAddress>($"SELECT {Columns} FROM public.\"ServiceAddresses\" WHERE \"ServiceAddressId\" = @serviceAddressId",
                new { serviceAddressId })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Find the listed addresses matching a street address. Failing an exact house number, try the building number without suffixes or fractions,
        /// and failing that the nearest listed building within MaxDistanceMeters of the point.
        /// </summary>
        public static async Task<LookupResult> LookupAsync(string houseNumber, string streetName, string postal, double latitude, double longitude, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var streetKey = ToStreetKey(streetName);
            if (!string.IsNullOrWhiteSpace(houseNumber) && !string.IsNullOrWhiteSpace(streetKey) && !string.IsNullOrWhiteSpace(postal))
            {
                var street = (await connection
                    .QueryAsync<ServiceAddress>($"SELECT {Columns} FROM public.\"ServiceAddresses\" " +
                    "WHERE \"Postal\" = @postal AND \"StreetKey\" = @streetKey",
                    new { postal = postal.Trim(), streetKey })
                    .ConfigureAwait(false)).ToArray();

                var exact = street.Where(x => string.Equals(x.HouseNumber.Trim(), houseNumber.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
                if (exact.Length > 0)
                {
                    return new(MatchType.Exact, exact);
                }

                var houseKey = ToHouseKey(houseNumber);
                var sameBuilding = houseKey.Length > 0 ? street.Where(x => ToHouseKey(x.HouseNumber) == houseKey).ToArray() : [];
                if (sameBuilding.Length > 0)
                {
                    return new(MatchType.HouseNumber, sameBuilding);
                }
            }

            if (latitude is 0 && longitude is 0)
            {
                return new(MatchType.None, []);
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
                return new(MatchType.None, []);
            }

            return new(MatchType.Nearby, [.. nearby.Where(x => x.Latitude == closest.Address.Latitude && x.Longitude == closest.Address.Longitude)]);
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
