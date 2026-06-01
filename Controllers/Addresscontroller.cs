// FILE: Controllers/AddressController.cs
//  Quản lý nhiều địa chỉ giao hàng cho user.
// Tích hợp với Checkout: gợi ý địa chỉ đã lưu qua AJAX.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;

[LoginRequired]
public class AddressController(MilkStore4Context db) : Controller
{
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;
    private const int MaxAddresses = 5;

    // GET /Address — danh sách địa chỉ
    public async Task<IActionResult> Index()
    {
        var addresses = await db.UserAddresses
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
        return View(addresses);
    }

    // POST /Address/Add
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string label, string fullAddress, string? phone, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(fullAddress) || fullAddress.Trim().Length < 10)
        {
            TempData["Error"] = "Địa chỉ quá ngắn (tối thiểu 10 ký tự).";
            return RedirectToAction("Index");
        }

        int count = await db.UserAddresses.CountAsync(a => a.UserId == UserId);
        if (count >= MaxAddresses)
        {
            TempData["Error"] = $"Tối đa {MaxAddresses} địa chỉ. Vui lòng xóa bớt.";
            return RedirectToAction("Index");
        }

        // Nếu set default → bỏ default của các địa chỉ khác
        if (isDefault)
            await db.UserAddresses
                .Where(a => a.UserId == UserId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

        db.UserAddresses.Add(new UserAddress
        {
            UserId = UserId,
            Label = label?.Trim() ?? "Địa chỉ",
            FullAddress = fullAddress.Trim(),
            Phone = phone?.Trim(),
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm địa chỉ mới.";
        return RedirectToAction("Index");
    }

    // POST /Address/SetDefault?id=3
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        // Bỏ default cũ
        await db.UserAddresses
            .Where(a => a.UserId == UserId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

        // Set default mới
        await db.UserAddresses
            .Where(a => a.Id == id && a.UserId == UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, true));

        TempData["Success"] = "Đã cập nhật địa chỉ mặc định.";
        return RedirectToAction("Index");
    }

    // POST /Address/Delete?id=3
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await db.UserAddresses
            .Where(a => a.Id == id && a.UserId == UserId)
            .ExecuteDeleteAsync();

        TempData["Success"] = "Đã xóa địa chỉ.";
        return RedirectToAction("Index");
    }

    // GET /Address/List — AJAX: trả JSON danh sách địa chỉ (dùng trong Checkout)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var addresses = await db.UserAddresses
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Label,
                a.FullAddress,
                a.Phone,
                a.IsDefault
            })
            .ToListAsync();
        return Json(addresses);
    }
}