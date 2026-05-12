// FILE: Program.cs
// MỤC ĐÍCH: Điểm khởi động ứng dụng ASP.NET Core MilkStore.
//           Cấu hình toàn bộ service container (DI), middleware pipeline,
//           routing, và khởi tạo database schema khi app start.


using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;   // ← THÊM MỚI: để nhận HTTPS từ reverse proxy Render
using MilkStore.Hubs;
using MilkStore.Models;

var builder = WebApplication.CreateBuilder(args);


// CẤU HÌNH KESTREL (web server tích hợp của ASP.NET Core)
// Giới hạn kết nối đồng thời và kích thước request body
// để tránh bị DDoS hoặc upload file quá lớn.

builder.WebHost.ConfigureKestrel(options =>
{
    // Tối đa 10 kết nối đồng thời (phù hợp môi trường dev/staging nhỏ)
    options.Limits.MaxConcurrentConnections = 10;

    // Giới hạn request body tối đa 10MB (ngăn upload file quá lớn)
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});


// ĐĂNG KÝ SERVICES VÀO DI CONTAINER

// ← THÊM MỚI: Cấu hình ForwardedHeaders để app nhận biết đang chạy sau HTTPS proxy của Render
// Render chạy reverse proxy: client → HTTPS → Render proxy → HTTP nội bộ → app
// Nếu không có cấu hình này, app thấy scheme là "http://" thay vì "https://"
// → Google OAuth sẽ gửi redirect_uri=http://... thay vì https://... → bị lỗi mismatch
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // Tin tưởng tất cả proxy (an toàn vì Render quản lý hạ tầng)
    options.KnownProxies.Clear();
});

// Hỗ trợ MVC Controllers + Razor Views
builder.Services.AddControllersWithViews();

// Kết nối PostgreSQL qua Entity Framework Core
// Connection string lấy từ appsettings.json hoặc biến môi trường
builder.Services.AddDbContext<MilkStore4Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MilkStore")));

// Cấu hình Session — lưu trạng thái người dùng (UserId, giỏ hàng, v.v.)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);    // Session hết hạn sau 2 giờ không hoạt động
    options.Cookie.HttpOnly = true;                  // Cookie không truy cập được từ JavaScript (bảo mật XSS)
    options.Cookie.IsEssential = true;               // Cookie này bắt buộc, không cần consent GDPR
    options.Cookie.SameSite = SameSiteMode.Lax;     // Cho phép gửi cookie khi navigate từ trang khác
});

// Cho phép truy cập HttpContext ở nơi không phải Controller (ví dụ: Filter, Service)
builder.Services.AddHttpContextAccessor();

// Đăng ký IHttpClientFactory — dùng trong ChatController để gọi Groq API
// Factory pattern giúp quản lý connection pooling tốt hơn so với new HttpClient()
builder.Services.AddHttpClient();

// Đăng ký SignalR — hỗ trợ realtime WebSocket cho tính năng chat
builder.Services.AddSignalR();

builder.Services.AddTransient<MilkStore.Services.EmailService>();

// Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies")
.AddGoogle("Google", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
    options.CallbackPath = "/signin-google";
});

var app = builder.Build();

// ← THÊM MỚI: Phải đứng ĐẦU TIÊN trong pipeline, trước tất cả middleware khác
// Đọc header X-Forwarded-Proto từ Render proxy và cập nhật Request.Scheme thành "https"
// Nhờ đó Google OAuth sẽ tạo redirect_uri=https://... thay vì http://...
app.UseForwardedHeaders();

// CẤU HÌNH MIDDLEWARE PIPELINE
// Thứ tự middleware RẤT QUAN TRỌNG — request đi qua theo thứ tự này


if (!app.Environment.IsDevelopment())
{
    // Production: redirect lỗi 500 về trang thân thiện
    app.UseExceptionHandler("/error/500");

    // Bật HSTS (HTTP Strict Transport Security) — buộc dùng HTTPS
    app.UseHsts();
}

// Xử lý HTTP status code lỗi (404, 403, v.v.)
// Logic đặc biệt: nếu response là JSON (API call) thì KHÔNG redirect
// → tránh biến JSON error response thành trang HTML lỗi
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    // Bỏ qua redirect nếu đây là API response trả về JSON
    // (ví dụ: ChatController.Send() trả 400 BadRequest)
    if (response.ContentType != null &&
        response.ContentType.Contains("application/json"))
    {
        return;
    }

    // Redirect về trang lỗi tương ứng (ví dụ: /error/404)
    response.Redirect($"/error/{response.StatusCode}");
});

app.UseStaticFiles();   // Phục vụ file tĩnh trong wwwroot (CSS, JS, ảnh)
app.UseRouting();       // Kích hoạt hệ thống routing
app.UseSession();
app.UseAuthentication();
app.UseAuthorization(); // Kiểm tra quyền truy cập (dùng với [Authorize])
app.MapStaticAssets();  // .NET 10: tối ưu serving static assets với fingerprinting


// ĐỊNH NGHĨA ROUTES


// Route cho Areas (Admin area): /Admin/Dashboard/Index
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// Route mặc định: /Home/Index, /Product/Detail/5, v.v.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map SignalR Hub endpoint — client kết nối WebSocket tới địa chỉ này
// Được dùng trong JavaScript: new signalR.HubConnectionBuilder().withUrl("/chatHub")
app.MapHub<ChatHub>("/chatHub");

// Lắng nghe trên port 8080 (phù hợp deploy Docker/cloud không dùng port 80)
app.Urls.Add("http://0.0.0.0:8080");

// ============================================================
// KHỞI TẠO DATABASE SCHEMA KHI ỨNG DỤNG CHẠY
//
// Thay vì dùng EF Migration, đây dùng raw SQL để tạo bảng
// nếu chưa tồn tại (idempotent — chạy nhiều lần không lỗi).
//
// LÝ DO dùng raw SQL thay Migration:
//   - ChatMessages được thêm sau, không muốn tạo migration mới
//   - Đơn giản hơn cho môi trường deploy tự động
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MilkStore4Context>();

    // Tạo bảng ChatMessages nếu chưa tồn tại
    // Kèm index trên SessionId để query lịch sử chat nhanh hơn
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ChatMessages"" (
            ""Id""        SERIAL PRIMARY KEY,
            ""SessionId"" VARCHAR(100) NOT NULL,
            ""Role""      VARCHAR(20)  NOT NULL,
            ""Content""   TEXT         NOT NULL,
            ""CreatedAt"" TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            ""UserId""    INTEGER      NULL,
            ""IsRead""    BOOLEAN      NOT NULL DEFAULT FALSE
        );
        CREATE INDEX IF NOT EXISTS ""IX_ChatMessages_SessionId"" ON ""ChatMessages""(""SessionId"");
    ");

    // Thêm cột mới vào bảng Reviews nếu chưa có
    // ALTER TABLE ... ADD COLUMN IF NOT EXISTS: an toàn khi chạy nhiều lần
    // IsAdminReply: phân biệt review thường vs reply của admin
    // ParentReviewId: cấu trúc reply lồng nhau (review → admin reply)
    // Tạo toàn bộ bảng nếu chưa có
    db.Database.ExecuteSqlRaw(@"
    CREATE TABLE IF NOT EXISTS ""Roles"" (
        ""Id"" SERIAL PRIMARY KEY,
        ""RoleName"" VARCHAR(50) NOT NULL
    );
    INSERT INTO ""Roles"" (""Id"",""RoleName"") VALUES (1,'Admin'),(2,'Customer') ON CONFLICT DO NOTHING;

    CREATE TABLE IF NOT EXISTS ""Users"" (
        ""Id""       SERIAL PRIMARY KEY,
        ""RoleId""   INT NOT NULL DEFAULT 2,
        ""FullName"" VARCHAR(150) NOT NULL,
        ""Email""    VARCHAR(150) NOT NULL UNIQUE,
        ""Password"" VARCHAR(255) NOT NULL,
        ""Phone""    VARCHAR(20),
        ""Address""  VARCHAR(255),
        ""ResetToken"" VARCHAR(100),
        ""ResetTokenExpiry"" TIMESTAMPTZ,
        CONSTRAINT ""FK_Users_Roles"" FOREIGN KEY(""RoleId"") REFERENCES ""Roles""(""Id"")
    );

    CREATE TABLE IF NOT EXISTS ""Brands"" (
        ""Id"" SERIAL PRIMARY KEY,
        ""Name"" VARCHAR(100) NOT NULL
    );

    CREATE TABLE IF NOT EXISTS ""Categories"" (
        ""Id"" SERIAL PRIMARY KEY,
        ""Name"" VARCHAR(100) NOT NULL
    );

    CREATE TABLE IF NOT EXISTS ""Products"" (
        ""Id""            SERIAL PRIMARY KEY,
        ""ProductName""   VARCHAR(200) NOT NULL,
        ""Description""   TEXT,
        ""Price""         DECIMAL(18,2) NOT NULL,
        ""StockQuantity"" INT NOT NULL DEFAULT 0,
        ""ImageUrl""      VARCHAR(500),
        ""ExpiryDate""    DATE NOT NULL DEFAULT CURRENT_DATE,
        ""CategoryId""    INT NOT NULL,
        ""BrandId""       INT NOT NULL,
        CONSTRAINT ""FK_Products_Categories"" FOREIGN KEY(""CategoryId"") REFERENCES ""Categories""(""Id""),
        CONSTRAINT ""FK_Products_Brands"" FOREIGN KEY(""BrandId"") REFERENCES ""Brands""(""Id"")
    );

    CREATE TABLE IF NOT EXISTS ""Orders"" (
        ""Id""              SERIAL PRIMARY KEY,
        ""UserId""          INT NOT NULL,
        ""OrderDate""       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        ""TotalAmount""     DECIMAL(18,2) NOT NULL,
        ""Status""          VARCHAR(50) NOT NULL DEFAULT 'Pending',
        ""PaymentMethod""   VARCHAR(50) NOT NULL DEFAULT 'COD',
        ""ShippingAddress"" VARCHAR(500),
        ""Note""            VARCHAR(500),
        CONSTRAINT ""FK_Orders_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id"")
    );

    CREATE TABLE IF NOT EXISTS ""OrderItems"" (
        ""Id""          SERIAL PRIMARY KEY,
        ""OrderId""     INT NOT NULL,
        ""ProductId""   INT NOT NULL,
        ""Quantity""    INT NOT NULL DEFAULT 1,
        ""PriceAtTime"" DECIMAL(18,2) NOT NULL,
        CONSTRAINT ""FK_OrderItems_Orders"" FOREIGN KEY(""OrderId"") REFERENCES ""Orders""(""Id""),
        CONSTRAINT ""FK_OrderItems_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id"")
    );

    CREATE TABLE IF NOT EXISTS ""CartItems"" (
        ""Id""        SERIAL PRIMARY KEY,
        ""UserId""    INT NOT NULL,
        ""ProductId"" INT NOT NULL,
        ""Quantity""  INT NOT NULL DEFAULT 1,
        CONSTRAINT ""FK_CartItems_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id""),
        CONSTRAINT ""FK_CartItems_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id"")
    );

    CREATE TABLE IF NOT EXISTS ""Reviews"" (
        ""Id""            SERIAL PRIMARY KEY,
        ""ProductId""     INT NOT NULL,
        ""UserId""        INT NOT NULL,
        ""Rating""        INT NOT NULL DEFAULT 5,
        ""Comment""       TEXT,
        ""CreatedAt""     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        ""IsAdminReply""  BOOLEAN NOT NULL DEFAULT FALSE,
        ""ParentReviewId"" INT,
        CONSTRAINT ""FK_Reviews_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id""),
        CONSTRAINT ""FK_Reviews_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id"")
    );
");
}

app.Run();