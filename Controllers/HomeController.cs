using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

public class HomeController(MilkStore4Context db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var featured = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.StockQuantity > 0)
            .OrderBy(_ => Guid.NewGuid())
            .Take(8)
            .ToListAsync();

        ViewBag.FeaturedProducts = featured;
      
        return View();
    }

    [Route("error/{code?}")]
    public IActionResult Error(int? code)
    {
        ViewBag.Code = code ?? 500;
        return View();
    }
}
