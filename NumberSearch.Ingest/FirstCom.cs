﻿using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class FirstCom
    {
        /// <summary>
        /// Ingest phone numbers from the FirstCom API.
        /// </summary>
        /// <param name="username"> The firstCom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <param name="connectionString"> the connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string username, string password, string connectionString)
        {
            var start = DateTime.Now;

            var stats = await Program.SubmitPhoneNumbersAsync(await GetValidNumbersByNPAAsync(username, password), connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "FirstCom";

            return stats;
        }

        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The firstcom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string username, string password)
        {
            var areaCodes = AreaCode.AreaCodes;

            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await NpaNxxFirstPointCom.GetAsync(code.ToString(), string.Empty, string.Empty, username, password));
                    Log.Information($"Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }
    }
}