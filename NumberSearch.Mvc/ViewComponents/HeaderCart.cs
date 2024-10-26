using Microsoft.AspNetCore.Mvc;

using NumberSearch.Mvc.Models;

namespace NumberSearch.Mvc.ViewComponents
{
    public class HeaderCart : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View(cart.ProductOrders);
        }
    }
}