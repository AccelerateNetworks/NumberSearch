using Dapper;

using Npgsql;

using System;
using System.Threading.Tasks;

#nullable disable

namespace NumberSearch.DataAccess
{
    public partial class Carrier
    {
        public Guid CarrierId { get; set; }
        public string Ocn { get; set; }
        public string Lec { get; set; }
        public string Lectype { get; set; }
        public string Spid { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Ratecenter { get; set; }
        public string Color { get; set; }
        public string LogoLink { get; set; }
        public DateTime LastUpdated { get; set; }

        public static async Task<Carrier> GetByOCNAsync(string ocn, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Carrier>("SELECT \"CarrierId\", \"OCN\", \"LEC\", \"LECType\", \"SPID\", \"Name\", \"Type\", \"Ratecenter\", \"Color\", \"LogoLink\", \"LastUpdated\" FROM public.\"Carriers\" WHERE \"OCN\" = @OCN",
                new { OCN = ocn })
                .ConfigureAwait(false);

            return result;
        }
    }
}
