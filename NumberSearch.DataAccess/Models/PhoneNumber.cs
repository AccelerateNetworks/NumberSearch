using Dapper;

using Npgsql;

using NumberSearch.DataAccess.Models;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public enum IngestProvider
    {
        BulkVS,
        TeleMessage,
        FirstPointCom,
        Peerless,
        IntegrationTest
    }

    public class PhoneNumber
    {
        public string DialedNumber { get; set; }
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public string NumberType { get; set; }
        public bool Purchased { get; set; }

        public static bool TryParse(string input, out PhoneNumber number)
        {
            // Clean up the query.
            input = input?.Trim().ToLowerInvariant();

            // Parse the query.
            var converted = new List<char>();
            foreach (var letter in input)
            {
                // Allow digits.
                if (char.IsDigit(letter))
                {
                    converted.Add(letter);
                }
                // Convert letters to digits.
                else if (char.IsLetter(letter))
                {
                    converted.Add(LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            var cleanedQuery = new string(converted.ToArray());

            var checkTenDigit = cleanedQuery.Length == 10;

            bool checkNpa = int.TryParse(cleanedQuery.Substring(0, 3), out int npa);
            bool checkNxx = int.TryParse(cleanedQuery.Substring(3, 3), out int nxx);
            bool checkXxxx = int.TryParse(cleanedQuery.Substring(6, 4), out int xxxx);

            var checkValid = AreaCode.ValidPhoneNumber(npa, nxx, xxxx);

            if (checkNpa && checkNxx && checkXxxx && checkTenDigit && checkValid)
            {
                number = new PhoneNumber
                {
                    DialedNumber = cleanedQuery,
                    NPA = npa,
                    NXX = nxx,
                    XXXX = xxxx,
                    DateIngested = DateTime.Now
                };

                return true;
            }
            else
            {
                number = null;
                return false;
            }
        }

        /// <summary>
        /// Get a list of all phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of all phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> GetAllNumbersAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<string>("SELECT \"DialedNumber\" FROM public.\"PhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PhoneNumber> GetAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QuerySingleOrDefaultAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false) ?? new PhoneNumber();

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> SearchAsync(string query, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query", new { query = $"%{query}%" })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query and returns paginated results with a prioritized list of numbers.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> RecommendedPaginatedSearchAsync(string query, int page, string connectionString)
        {
            var offset = (page * 50) - 50;
            var limit = 50;
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query ORDER BY \"NumberType\", \"DialedNumber\" OFFSET @offset LIMIT @limit",
                new { query = $"%{query}%", offset, limit })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query and returns paginated results with a sequential list of numbers.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> SequentialPaginatedSearchAsync(string query, int page, string connectionString)
        {
            var offset = (page * 50) - 50;
            var limit = 50;
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query ORDER BY \"DialedNumber\" OFFSET @offset LIMIT @limit",
                new { query = $"%{query}%", offset, limit })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query and returns paginated results with a list of numbers ordered by location.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> LocationPaginatedSearchAsync(string query, int page, string connectionString)
        {
            var offset = (page * 50) - 50;
            var limit = 50;
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query ORDER BY \"City\", \"NumberType\" OFFSET @offset LIMIT @limit",
                new { query = $"%{query}%", offset, limit })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<PhoneNumber>> LocationByCityPaginatedSearchAsync(string query, string city, int page, string connectionString)
        {
            var offset = (page * 50) - 50;
            var limit = 50;
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query AND \"City\" = @city ORDER BY \"NumberType\" OFFSET @offset LIMIT @limit",
                new { query = $"%{query}%", city, offset, limit })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> NumberOfResultsInQuery(string query, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query",
                new { query = $"%{query}%" })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> NumberOfResultsInQueryWithCity(string query, string city, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query AND \"City\" = @city ",
                new { query = $"%{query}%", city })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<string>> CitiesInQueryAsync(string query, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<string>("SELECT DISTINCT \"City\" FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" LIKE @query ",
                new { query = $"%{query}%" })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> GetCountByProvider(string ingestedFrom, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"IngestedFrom\" = @ingestedFrom",
                new { ingestedFrom })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> GetCountByNumberType(string numberType, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"NumberType\" = @numberType",
                new { numberType })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> GetCountByAreaCode(int NPA, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"NPA\" = @NPA",
                new { NPA })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<int> GetTotal(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) AS Count FROM public.\"PhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Delete only numbers that haven't been reingested recently.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IngestStatistics> DeleteOld(DateTime ingestStart, string connectionString)
        {
            var start = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PhoneNumbers\" " +
                "WHERE \"DateIngested\" < @DateIngested",
                new { DateIngested = ingestStart.AddHours(-12) })
                .ConfigureAwait(false);

            return new IngestStatistics
            {
                Removed = result,
                IngestedFrom = "Cleanup",
                StartDate = start,
                EndDate = DateTime.Now
            };
        }

        /// <summary>
        /// Delete a specific phone number by its dialed number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PhoneNumbers\" " +
                "WHERE \"DialedNumber\" = @DialedNumber",
                new { DialedNumber })
                .ConfigureAwait(false);


            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Delete only numbers that haven't been reingested recently that were ingested from a specific provider.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IngestStatistics> DeleteOldByProvider(DateTime ingestStart, TimeSpan cycleTime, string ingestedFrom, string connectionString)
        {
            var start = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PhoneNumbers\" " +
                "WHERE \"DateIngested\" < @DateIngested AND \"IngestedFrom\" = @IngestedFrom",
                new { DateIngested = ingestStart - cycleTime, IngestedFrom = ingestedFrom })
                .ConfigureAwait(false);

            return new IngestStatistics
            {
                Removed = result,
                IngestedFrom = $"{ingestedFrom} Cleanup",
                StartDate = start,
                EndDate = DateTime.Now
            };
        }

        /// <summary>
        /// Delete only numbers that haven't been reingested recently that were ingested from a specific provider.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IngestStatistics> DeleteOldByProviderAndAreaCode(DateTime ingestStart, TimeSpan cycleTime, int areaCode, string ingestedFrom, string connectionString)
        {
            var start = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PhoneNumbers\" " +
                "WHERE \"NPA\" = @areaCode AND \"DateIngested\" < @DateIngested AND \"IngestedFrom\" = @IngestedFrom",
                new { areaCode, DateIngested = ingestStart - cycleTime, IngestedFrom = ingestedFrom })
                .ConfigureAwait(false);

            return new IngestStatistics
            {
                Removed = result,
                IngestedFrom = $"{ingestedFrom} Cleanup",
                StartDate = start,
                EndDate = DateTime.Now
            };
        }


        /// <summary>
        /// Added new numbers to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            // If anything is null bail out.
            if (NPA < 100 || NXX < 100 || XXXX < 1 || DialedNumber == null || City == null || State == null || IngestedFrom == null)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\") " +
                "VALUES(@DialedNumber, @NPA, @NXX, @XXXX, @City, @State, @IngestedFrom, @DateIngested, @NumberType, @Purchased)",
                new { DialedNumber, NPA, NXX, XXXX, City, State, IngestedFrom, DateIngested, NumberType, Purchased })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Submit ingested phone numbers to the database in bulk.
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<bool> BulkPostAsync(IEnumerable<PhoneNumber> numbers, string connectionString)
        {
            // Make sure there are some numbers incoming.
            if (numbers is null || numbers?.ToArray()?.Length < 0)
            {
                return false;
            }

            var values = new List<string>();

            foreach (var number in numbers?.ToArray())
            {
                // If anything is null bail out.
                if (!(number.NPA < 100 || number.NXX < 100 || number.XXXX < 1 || number.DialedNumber == null || number.City == null || number.State == null || number.IngestedFrom == null || number.NumberType == null))
                {
                    values.Add($"('{number.DialedNumber}', {number.NPA}, {number.NXX}, {number.XXXX.ToString("0000", new CultureInfo("en-US"))}, '{number.City}', '{number.State}', '{number.IngestedFrom}', '{number.DateIngested}', '{number.NumberType}', '{number.Purchased}')");
                }
            }

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\") VALUES {string.Join(", ", values)}";

            var result = await connection.ExecuteAsync(sql).ConfigureAwait(false);

            if (result > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Update a phone number that already exists in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PhoneNumbers\" SET \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"NumberType\" = @NumberType, \"Purchased\" = @Purchased, \"City\" = @City, \"State\" = @State " +
                "WHERE \"DialedNumber\" = @DialedNumber",
                new { IngestedFrom, DateIngested, NumberType, Purchased, City, State, DialedNumber })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a letter in a string to numbers on a dialpad.
        /// </summary>
        /// <param name="letter"> A lowercase char. </param>
        /// <returns> The dialpad equivalent. </returns>
        public static char LetterToKeypadDigit(char letter)
        {
            // Map the chars to their keypad numerical values.
            return letter switch
            {
                '+' => '0',
                // The digit 1 isn't mapped to any chars on a phone keypad.
                'a' or 'b' or 'c' => '2',
                'd' or 'e' or 'f' => '3',
                'g' or 'h' or 'i' => '4',
                'j' or 'k' or 'l' => '5',
                'm' or 'n' or 'o' => '6',
                'p' or 'q' or 'r' or 's' => '7',
                't' or 'u' or 'v' => '8',
                'w' or 'x' or 'y' or 'z' => '9',
                // If the char isn't mapped to anything, respect it's existence by mapping it to a wildcard.
                _ => '*',
            };
        }
    }
}
