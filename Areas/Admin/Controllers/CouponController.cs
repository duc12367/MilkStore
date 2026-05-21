// FILE: Areas/Admin/Controllers/CouponController.cs
// [FIX KM03, KM21] Tạo mới — Admin CRUD mã giảm giá
// Fixes: KM02 (kiểm tra trùng Code), KM03 (thiếu UI), KM04 (giá trị âm),
//        KM05 (>100%), KM08 (xóa mã đang dùng), KM19 (ký tự đặc biệt),
//        KM21 (không có trang admin), KM23 (thiếu StartDate), KM24 (thiếu MaxUsage)

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class CouponController(MilkStore4Context db) : Controller
{
    // ── GET /Admin/Coupon ──────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? filter)
    {
        var now = DateTime.UtcNow;
        var query = db.Coupons.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Code.Contains(search.ToUpper()));

        // [FIX KM10] Lọc theo trạng thái
        query = filter switch
        {
            "active" => query.Where(c => c.StartDate <= now && c.ExpiryDate >= now),
            "expired" => query.Where(c => c.ExpiryDate < now),
            "pending" => query.Where(c => c.StartDate > now),
            _ => query
        };

        var coupons = await query.OrderByDescending(c => c.Id).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Filter = filter;
        ViewBag.Now = now;
        return View(coupons);
    }

    // ── GET /Admin/Coupon/Create ───────────────────────────────────
    public IActionResult Create()
    {
        var model = new Coupon
        {
            StartDate = DateTime.UtcNow.Date,
            ExpiryDate = DateTime.UtcNow.Date.AddMonths(1)
        };
        return View("Form", model);
    }

    // ── POST /Admin/Coupon/Create ──────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon model)
    {
        model.Code = model.Code?.Trim().ToUpper() ?? "";

        // [FIX KM02] Kiểm tra trùng Code
        if (await db.Coupons.AnyAsync(c => c.Code == model.Code))
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");

        // [FIX KM04, KM05] Validate DiscountValue theo DiscountType
        if (model.DiscountValue <= 0)
            ModelState.AddModelError("DiscountValue", "Giá trị giảm phải lớn hơn 0.");
        else if (model.DiscountType == "Percent" && model.DiscountValue > 100)
            ModelState.AddModelError("DiscountValue", "Giảm theo % không được vượt quá 100%.");

        // [FIX KM14] Validate StartDate < ExpiryDate
        if (model.ExpiryDate <= model.StartDate)
            ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày bắt đầu.");

        if (!ModelState.IsValid)
            return View("Form", model);

        model.UsageCount = 0;
        db.Coupons.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo mã giảm giá {model.Code}.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/Coupon/Edit/5 ───────────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();
        return View("Form", coupon);
    }

    // ── POST /Admin/Coupon/Edit/5 ──────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon model)
    {
        if (id != model.Id) return BadRequest();

        model.Code = model.Code?.Trim().ToUpper() ?? "";

        // [FIX KM02] Kiểm tra trùng Code, bỏ qua chính nó
        if (await db.Coupons.AnyAsync(c => c.Code == model.Code && c.Id != id))
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");

        if (model.DiscountValue <= 0)
            ModelState.AddModelError("DiscountValue", "Giá trị giảm phải lớn hơn 0.");
        else if (model.DiscountType == "Percent" && model.DiscountValue > 100)
            ModelState.AddModelError("DiscountValue", "Giảm theo % không được vượt quá 100%.");

        if (model.ExpiryDate <= model.StartDate)
            ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày bắt đầu.");

        if (!ModelState.IsValid)
            return View("Form", model);

        db.Coupons.Update(model);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật mã {model.Code}.";
        return RedirectToAction(nameof(Index));
    }

    // ── POST /Admin/Coupon/Delete/5 ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await db.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();

        // [FIX KM08] Không cho xóa mã đang được dùng trong đơn hàng
        bool isUsed = await db.Orders.AnyAsync(o => o.CouponCode == coupon.Code
                                                 && o.Status != "Cancelled");
        if (isUsed)
        {
            TempData["Error"] = $"Không thể xóa mã '{coupon.Code}' vì đang được sử dụng trong đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa mã {coupon.Code}.";
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/Coupon/ValidateCode?code=SALE10 (AJAX) ─────────
    // [FIX KM02] Kiểm tra trùng mã real-time khi admin nhập
    [HttpGet]
    public async Task<IActionResult> ValidateCode(string code, int? excludeId)
    {
        code = code?.Trim().ToUpper() ?? "";
        var exists = await db.Coupons.AnyAsync(c => c.Code == code && c.Id != (excludeId ?? 0));
        return Json(new { exists });
    }
}