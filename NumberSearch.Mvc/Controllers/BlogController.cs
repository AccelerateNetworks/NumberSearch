using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Threading.Tasks;

using WriteAs.NET;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class BlogController : Controller
    {
        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> IndexAsync(string postName)
        {
            var client = new WriteAsClient("https://write.as/");
            var allPosts = await client.GetAllPosts("acceleratenetworks");
            if (string.IsNullOrWhiteSpace(postName))
            {
                return View(allPosts);
            }
            else
            {
                var post = allPosts.FirstOrDefault(x => x.Title.Contains(postName));
                return post is not null ? View(post) : View(allPosts);
            }
        }
    }
}
