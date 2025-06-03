﻿using NumberSearch.DataAccess;
using NumberSearch.DataAccess.LCGuide;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Ingest
{
    public class Services
    {
        /// <summary>
        /// Assign a NumberType to a number based on the number of repeating digits in the number.
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static ref readonly PhoneNumber[] AssignNumberTypes(in PhoneNumber[] numbers)
        {
            // NumberTypes
            string Executive = "Executive";
            string Premium = "Premium";
            string Standard = "Standard";
            string Tollfree = "Tollfree";

            // Assign a Type based on number of repeating digits.
            foreach (var number in numbers)
            {
                // https://stackoverflow.com/questions/39472429/count-all-character-occurrences-in-a-string-c-sharp
                int count = number.DialedNumber.AsValueEnumerable().GroupBy(static c => c).Select(c => c.Count()).Max();

                number.NumberType = count switch
                {
                    1 or 2 or 3 => Standard,
                    4 or 5 => Premium,
                    6 or 7 or 8 or 9 or 10 => Executive,
                    _ => Standard,
                };

                // Overwrite the number type with Tollfree, as that's the primary type.
                var checkTollfree = PhoneNumbersNA.AreaCode.TollFreeFlatLookup[number.NPA];

                if (checkTollfree)
                {
                    number.NumberType = Tollfree;
                    number.City = "Tollfree";
                    number.State = string.Empty;
                }
            }

            return ref numbers;
        }

        /// <summary>
        /// Split the list of accounts in insert into smaller lists so that they can be submitted in bulk to the database in reasonably sized chunks.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations"></param>
        /// <param name="nSize"> The maximum number of items in the smaller lists. </param>
        /// <returns></returns>
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 100)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        /// <summary>
        /// Submit the ingested Phone numbers to the database in bulk to minimize the number of commands that have to be sent.
        /// </summary>
        /// <param name="numbers"> A list of phone numbers. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> SubmitPhoneNumbersAsync(PhoneNumber[] numbers, ReadOnlyMemory<char> connectionString)
        {
            IngestStatistics stats = new();

            var inserts = new Dictionary<string, PhoneNumber>();
            var updates = new Dictionary<string, PhoneNumber>();

            if (numbers.Length > 0)
            {
                var existingNumbers = await PhoneNumber.GetAllNumbersAsync(connectionString.ToString());
                var dict = existingNumbers.AsValueEnumerable().ToDictionary(x => x, x => x);
                // Submit the batch to the remote database.
                foreach (var number in numbers)
                {
                    // Check if it already exists.
                    var inDb = dict?.TryGetValue(number.DialedNumber, out var _) ?? false;

                    if (inDb)
                    {
                        var check = updates.TryAdd(number.DialedNumber, number);

                        if (check)
                        {
                            stats.NumbersRetrived++;
                            stats.UpdatedExisting++;
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.FailedToIngest++;
                        }

                    }
                    else
                    {
                        // If it doesn't exist then add it.
                        var check = inserts.TryAdd(number.DialedNumber, number);

                        // When the API returns duplicate numbers.
                        if (check)
                        {
                            stats.NumbersRetrived++;
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.FailedToIngest++;
                        }
                    }
                }
                Log.Information("Found {Count} new Phone Numbers to Insert.", inserts?.Count);
                Log.Information("Found {Count} existing Phone Numbers to Update.", updates?.Count);
            }

            var count = 0;

            if (updates is not null && updates.Count != 0)
            {
                // Execute these API requests in parallel.
                await Parallel.ForEachAsync([..updates.Values], async (update, token) =>
                {
                    if (count % 100 == 0 && count != 0)
                    {
                        Log.Information("Updated {Count} of {UpdatesCount} Phone Numbers.", count, updates?.Count);
                    }
                    try
                    {
                        var result = await update.PutAsync(connectionString.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex.Message);
                        Log.Fatal("{@Update}", update);
                    }
                    count++;
                });

                Log.Information("Updated {Count} Phone Numbers", updates?.Count);
            }

            var listInserts = inserts?.Values.AsValueEnumerable().ToList();

            var groups = SplitList(listInserts ?? []);

            foreach (var group in groups.AsValueEnumerable().ToArray())
            {
                try
                {
                    var check = await PhoneNumber.BulkPostAsync(group, connectionString.ToString());

                    if (check) { stats!.IngestedNew += group.Count; };

                    Log.Information("{IngestedNew} of {Count} submitted to the database.", stats?.IngestedNew, listInserts?.Count);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to submit a batch of PhoneNumbers to the database. Exception: {Message}", ex.Message);
                    count = 0;
                    foreach (var number in group)
                    {
                        Log.Error("{Count}. {DialedNumber}, {IngestedFrom}", count, number?.DialedNumber, number?.IngestedFrom);
                        count++;
                    }
                }
            }

            return stats ?? new();
        }

        /// <summary>
        /// Assigns the correct city and state information to a phone number.
        /// </summary>
        /// <param name="numbers"> A list of phone numbers. </param>
        /// <returns> A list of phone numbers. </returns>
        public static async Task<PhoneNumber[]> AssignRatecenterAndRegionAsync(PhoneNumber[] numbers)
        {
            Log.Information("Ingesting the Ratecenters and Regions on {Length} phone numbers.", numbers.Length);

            // Cache the lookups because API requests are expensive and phone numbers tend to be ingested in groups.
            var npaNxxLookup = new Dictionary<string, RateCenterLookup>();

            foreach (var number in numbers)
            {
                var checkTollfree = PhoneNumbersNA.AreaCode.TollFreeFlatLookup[number.NPA];

                if (checkTollfree)
                {
                    // Skip tollfree numbers
                    continue;
                }

                var checkMatch = npaNxxLookup.TryGetValue($"{number.NPA}{number.NXX}", out var match);

                if (checkMatch)
                {
                    number.City = match.RateCenter.ToString();
                    number.State = match.Region.ToString();
                }
                else
                {
                    try
                    {
                        match = await RateCenterLookup.GetAsync(number.NPA, number.NXX);

                        if (!string.IsNullOrWhiteSpace(match.RateCenter.ToString()))
                        {
                            npaNxxLookup.Add($"{number.NPA}{number.NXX}", match);

                            number.City = match.RateCenter.ToString();
                            number.State = match.Region.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to ingesting the Ratecenter and Region on {DialedNumber}", number.DialedNumber);
                        Log.Error(ex.Message);
                        Log.Error(ex.StackTrace ?? "No stack trace found.");
                    }
                }
            }

            Log.Information("Ingesting the Ratecenters and Regions on {Length} phone numbers.", numbers.Length);

            return numbers;
        }
    }
}