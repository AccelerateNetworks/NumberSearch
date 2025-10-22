using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NumberSearch.DataAccess.Models
{
    public enum IngestProvider
    {
        BulkVS,
        TeliMessage,
        FirstPointCom,
        Peerless,
        Call48,
        IntegrationTest
    }

    public readonly record struct PhoneNumberS(in ReadOnlyMemory<char> DialedNumber, int NPA, int NXX,
        int XXXX, ReadOnlyMemory<char> City, ReadOnlyMemory<char> State,
        ReadOnlyMemory<char> IngestedFrom, DateTime DateIngested, ReadOnlyMemory<char> NumberType,
        bool Purchased);

    public class PhoneNumber
    {
        public string DialedNumber { get; set; } = string.Empty;
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; }
        public string NumberType { get; set; } = string.Empty;
        public bool Purchased { get; set; }

    /// <summary>
    /// Get a list of all phone numbers in the database.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<PhoneNumber>> GetAllAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

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
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<string>("""SELECT "DialedNumber" FROM public."PhoneNumbers" """)
            .ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Get all phone numbers in an area code.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<PhoneNumber>> GetAllByAreaCodeAsync(int NPA, string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" WHERE \"NPA\" = @NPA",
            new { NPA })
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
        await using var connection = new NpgsqlConnection(connectionString);

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
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        var offset = page * 50 - 50;
        var limit = 50;
        // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        var offset = page * 50 - 50;
        var limit = 50;
        // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        var offset = page * 50 - 50;
        var limit = 50;
        // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<PhoneNumber>("SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\" FROM public.\"PhoneNumbers\" " +
            "WHERE \"DialedNumber\" LIKE @query ORDER BY \"City\", \"NumberType\" OFFSET @offset LIMIT @limit",
            new { query = $"%{query}%", offset, limit })
            .ConfigureAwait(false);

        return result;
    }

    public static async Task<IEnumerable<PhoneNumber>> LocationByCityPaginatedSearchAsync(string query, string city, int page, string connectionString)
    {
        var offset = page * 50 - 50;
        var limit = 50;
        // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

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
        query = query.Trim().Replace('*', '_');

        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<string>("SELECT DISTINCT \"City\" FROM public.\"PhoneNumbers\" " +
            "WHERE \"DialedNumber\" LIKE @query ",
            new { query = $"%{query}%" })
            .ConfigureAwait(false);

        return result;
    }

    public static async Task<int> GetCountByProvider(string ingestedFrom, string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"IngestedFrom\" = @ingestedFrom",
            new { ingestedFrom })
            .ConfigureAwait(false);

        return result;
    }

    public class CountByProvider
    {
        public string IngestedFrom { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }

    public static async Task<CountByProvider[]> GetCountAllProvider(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountByProvider>("SELECT \"IngestedFrom\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" GROUP BY \"IngestedFrom\"", new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public class CountNumberType
    {
        public string NumberType { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }

    public static async Task<CountNumberType[]> GetCountAllNumberType(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountNumberType>("SELECT \"NumberType\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" GROUP BY \"NumberType\"",
            new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public class CountNPA
    {
        public string NPA { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
    }


    public static async Task<CountNPA[]> GetCountAllAreaCode(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountNPA>("SELECT \"NPA\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" GROUP BY \"NPA\"",
            new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public static async Task<CountNPA[]> GetCountAllAreaCodeOwnedNumber(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountNPA>("SELECT \"NPA\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"IngestedFrom\" = 'OwnedNumber' GROUP BY \"NPA\"",
            new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public static async Task<CountNPA[]> GetCountAllAreaCodeBulkVS(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountNPA>("SELECT \"NPA\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"IngestedFrom\" = 'BulkVS' GROUP BY \"NPA\"",
            new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public static async Task<CountNPA[]> GetCountAllAreaCodeFPC(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .QueryAsync<CountNPA>("SELECT \"NPA\", COUNT(*) AS Count FROM public.\"PhoneNumbers\" WHERE \"IngestedFrom\" = 'FirstPointCom' GROUP BY \"NPA\"",
            new { })
            .ConfigureAwait(false);

        return [.. result];
    }

    public static async Task<int> GetTotal(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);

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

        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .ExecuteAsync("DELETE FROM public.\"PhoneNumbers\" " +
            "WHERE \"DateIngested\" < @DateIngested",
            new { DateIngested = ingestStart.AddHours(-48) })
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
        await using var connection = new NpgsqlConnection(connectionString);

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

        await using var connection = new NpgsqlConnection(connectionString);

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

        await using var connection = new NpgsqlConnection(connectionString);

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

        await using var connection = new NpgsqlConnection(connectionString);

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

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            if (numbers is not null)
            {
                foreach (var number in numbers.ToArray())
                {
                    // If anything is null, skip it.
                    if (!(number.NPA < 100 || number.NXX < 100 || number.XXXX < 1 || number.DialedNumber == null || number.City == null || number.State == null || number.IngestedFrom == null || number.NumberType == null))
                    {
                        var command = connection.CreateCommand();
                        command.CommandText =
                            $"INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"NumberType\", \"Purchased\") VALUES (@DialedNumber, @NPA, @NXX, @XXXX, @City, @State, @IngestedFrom, @DateIngested, @NumberType, @Purchased);";

                        var parameterDialedNumber = command.CreateParameter();
                        parameterDialedNumber.ParameterName = "@DialedNumber";
                        command.Parameters.Add(parameterDialedNumber);
                        parameterDialedNumber.Value = number.DialedNumber;

                        var parameterNPA = command.CreateParameter();
                        parameterNPA.ParameterName = "@NPA";
                        command.Parameters.Add(parameterNPA);
                        parameterNPA.Value = number.NPA;

                        var parameterNXX = command.CreateParameter();
                        parameterNXX.ParameterName = "@NXX";
                        command.Parameters.Add(parameterNXX);
                        parameterNXX.Value = number.NXX;

                        var parameterXXXX = command.CreateParameter();
                        parameterXXXX.ParameterName = "@XXXX";
                        command.Parameters.Add(parameterXXXX);
                        parameterXXXX.Value = number.XXXX;

                        var parameterCity = command.CreateParameter();
                        parameterCity.ParameterName = "@City";
                        command.Parameters.Add(parameterCity);
                        parameterCity.Value = number.City;

                        var parameterState = command.CreateParameter();
                        parameterState.ParameterName = "@State";
                        command.Parameters.Add(parameterState);
                        parameterState.Value = number.State;

                        var parameterIngestedFrom = command.CreateParameter();
                        parameterIngestedFrom.ParameterName = "@IngestedFrom";
                        command.Parameters.Add(parameterIngestedFrom);
                        parameterIngestedFrom.Value = number.IngestedFrom;

                        var parameterDateIngested = command.CreateParameter();
                        parameterDateIngested.ParameterName = "@DateIngested";
                        command.Parameters.Add(parameterDateIngested);
                        parameterDateIngested.Value = number.DateIngested;

                        var parameterNumberType = command.CreateParameter();
                        parameterNumberType.ParameterName = "@NumberType";
                        command.Parameters.Add(parameterNumberType);
                        parameterNumberType.Value = number.NumberType;

                        var parameterPurchased = command.CreateParameter();
                        parameterPurchased.ParameterName = "@Purchased";
                        command.Parameters.Add(parameterPurchased);
                        parameterPurchased.Value = number.Purchased;

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            await transaction.CommitAsync();
            await connection.CloseAsync();
            return true;
        }
        catch
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
        await using var connection = new NpgsqlConnection(connectionString);

        var result = await connection
            .ExecuteAsync("UPDATE public.\"PhoneNumbers\" SET \"DialedNumber\" = @DialedNumber, \"NPA\" = @NPA, \"NXX\" = @NXX, \"XXXX\" = @XXXX, \"City\" = @City, \"State\" = @State, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"NumberType\" = @NumberType, \"Purchased\" = @Purchased " +
            "WHERE \"DialedNumber\" = @DialedNumber",
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
}
}
