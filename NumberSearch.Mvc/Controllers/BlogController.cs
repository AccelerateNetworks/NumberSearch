using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Npgsql;

using System.Linq;
using System.Threading.Tasks;

using WriteAs.NET;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class BlogController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly string _postgresql;

        public BlogController(IConfiguration config)
        {
            configuration = config;
            _postgresql = configuration.GetConnectionString("PostgresqlProd");
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync(string query)
        {
            using var connection = new NpgsqlConnection(_postgresql);

            // Grab the posts from the database.
            var posts = await connection
                .QueryAsync<WriteAs.NET.Client.Models.Post>("SELECT \"Id\", \"Slug\", \"Appearance\", \"Language\", \"Rtl\", \"CreateDate\", \"LastUpdatedDate\", \"Title\", \"Body\", \"Views\" " +
                "FROM public.\"WriteAsPosts\"")
                .ConfigureAwait(false);

            if (posts is null || !posts.Any())
            {
                // If there are no post repopulate the database from the API.
                var client = new WriteAsClient("https://write.as/");
                var allPosts = await client.GetAllPosts("acceleratenetworks");

                // Clear out the table.
                _ = await connection.ExecuteAsync("TRUNCATE \"WriteAsPosts\"");

                // Ingest the current data.
                foreach (var post in allPosts)
                {
                    var result = await connection
                .ExecuteAsync("INSERT INTO public.\"WriteAsPosts\"( \"Id\", \"Slug\", \"Appearance\", \"Language\", \"Rtl\", \"CreateDate\", \"LastUpdatedDate\", \"Title\", \"Body\", \"Views\") " +
                "VALUES (@Id, @Slug, @Appearance, @Language, @Rtl, @CreateDate, @LastUpdatedDate, @Title, @Body, @Views)",
                new { post.Id, post.Slug, post.Appearance, post.Language, post.Rtl, post.CreateDate, post.LastUpdatedDate, post.Title, post.Body, post.Views })
                .ConfigureAwait(false);
                }

                posts = await connection
                    .QueryAsync<WriteAs.NET.Client.Models.Post>("SELECT \"Id\", \"Slug\", \"Appearance\", \"Language\", \"Rtl\", \"CreateDate\", \"LastUpdatedDate\", \"Title\", \"Body\", \"Views\" " +
                    "FROM public.\"WriteAsPosts\"")
                    .ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(posts);
            }
            else
            {
                var post = posts.Where(x => x.Title.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();
                return post is not null ? View(post) : View(posts);
            }
        }
    }
}
