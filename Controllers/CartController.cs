
// FILE: Controllers/CartController.cs
// MỤC ĐÍCH: Quản lý giỏ hàng của khách hàng đã đăng nhập.
//           Hỗ trợ: xem giỏ, thêm/cập nhật/xóa từng sản phẩm, xóa toàn bộ.
//
// BẢO MẬT:
//   [LoginRequired] ở cấp class → mọi action đều yêu cầu đăng nhập.
//   UserId luôn lấy từ Session (server-side), không nhận từ form/URL
//   → khách không thể giả mạo UserId để xem/sửa giỏ của người khác.


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;

/// <summary>
/// Controller xử lý toàn bộ thao tác trên giỏ hàng.
/// Chỉ phục vụ khách đã đăng nhập (bảo vệ bởi [LoginRequired]).
/// </summary>
[LoginRequired]
public class CartController(MilkStore4Context db) : Controller
{
    // Lấy UserId từ Session — server tự quản lý, client không can thiệp được.
    // Dùng ! (null-forgiving) vì [LoginRequired] đảm bảo Session luôn có giá trị.
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // --------------------------------------------------------
    // GET /Cart
    // Hiển thị toàn bộ sản phẩm trong giỏ hàng của user hiện tại.
    // Include(Product) để lấy tên, giá, ảnh sản phẩm trong một query duy nhất.
    // --------------------------------------------------------
    /// <summary>Trang giỏ hàng — liệt kê sản phẩm và tổng tiền.</summary>
    public async Task<IActionResult> Index()
    {
        var items = await db.CartItems
            .Include(c => c.Product)          // JOIN Products để lấy tên, giá
            .Where(c => c.UserId == UserId)   // Chỉ lấy giỏ của user đang đăng nhập
            .ToListAsync();

        // Tính tổng tiền: Σ(giá sản phẩm × số lượng)
        // Dùng ?? 0m để tránh lỗi nếu Product bị null (trường hợp sản phẩm đã bị xóa)
        decimal total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        ViewBag.Total = total;

        return View(items);
    }

    // --------------------------------------------------------
    // POST /Cart/Add
    // Thêm sản phẩm vào giỏ.
    //
    // LOGIC UPSERT (thêm mới hoặc cộng thêm số lượng):
    //   - Nếu sản phẩm chưa có trong giỏ → tạo dòng CartItem mới.
    //   - Nếu đã có → cộng thêm số lượng vào dòng hiện tại.
    //   → Tránh trường hợp cùng 1 sản phẩm có 2 dòng riêng biệt.
    // --------------------------------------------------------
    /// <summary>Thêm sản phẩm vào giỏ. Nếu đã có thì cộng thêm số lượng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]  // Chống CSRF: form phải có token hợp lệ
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        // Tìm xem sản phẩm này đã có trong giỏ của user chưa
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == UserId && c.ProductId == productId);

        if (item == null)
        {
            // Chưa có → tạo mới
            db.CartItems.Add(new CartItem
            {
                UserId = UserId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            // Đã có → cộng thêm (không tạo dòng trùng)
            item.Quantity += quantity;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm vào giỏ hàng!";
        return RedirectToAction("Index");
    }

    // --------------------------------------------------------
    // POST /Cart/Update
    // Cập nhật số lượng của một dòng trong giỏ.
    //
    // LOGIC ĐẶC BIỆT: nếu quantity <= 0 → xóa luôn dòng đó.
    // Bảo mật: lọc thêm c.UserId == UserId để user không thể
    // sửa CartItem của người khác dù biết cartItemId.
    // --------------------------------------------------------
    /// <summary>Cập nhật số lượng. Nếu quantity ≤ 0 thì xóa dòng khỏi giỏ.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        // Điều kiện kép: đúng Id VÀ đúng UserId → chống giả mạo cartItemId
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == UserId);

        if (item != null)
        {
            if (quantity <= 0)
                db.CartItems.Remove(item);  // Xóa nếu số lượng không hợp lệ
            else
                item.Quantity = quantity;   // Cập nhật số lượng mới

            await db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // --------------------------------------------------------
    // POST /Cart/Remove
    // Xóa một dòng sản phẩm khỏi giỏ hàng.
    // Bảo mật tương tự Update: phải khớp cả cartItemId lẫn UserId.
    // --------------------------------------------------------
    /// <summary>Xóa một sản phẩm khỏi giỏ hàng.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == UserId);

        if (item != null)
        {
            db.CartItems.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // --------------------------------------------------------
    // POST /Cart/Clear
    // Xóa toàn bộ giỏ hàng của user hiện tại.
    // Dùng RemoveRange để xóa tất cả trong một câu DELETE duy nhất.
    // --------------------------------------------------------
    /// <summary>Xóa sạch toàn bộ giỏ hàng của user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        // Lấy tất cả CartItem của user (chưa ToList → vẫn là IQueryable)
        var items = db.CartItems.Where(c => c.UserId == UserId);

        // RemoveRange: EF Core tạo 1 câu DELETE ... WHERE UserId = @uid (hiệu quả hơn vòng lặp)
        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
