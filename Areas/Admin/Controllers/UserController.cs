using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class UserController(MilkStore4Context db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.RoleId)
            .ToListAsync();
        return View(users);
    }
}
