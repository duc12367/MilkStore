// FILE: Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using MilkStore.Hubs;
using MilkStore.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 10;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MilkStore4Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MilkStore")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddTransient<MilkStore.Services.EmailService>();

// Google + Facebook OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddGoogle("Google", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
    options.CallbackPath = "/signin-google";
})
.AddFacebook("Facebook", options =>
{
    options.AppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID") ?? "";
    options.AppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET") ?? "";
    options.CallbackPath = "/signin-facebook";
    options.Scope.Add("email");
    options.Fields.Add("email");
    options.Fields.Add("name");
});

var app = builder.Build();

app.UseForwardedHeaders();

// Ép scheme thành https (Render chạy sau proxy)
app.Use(async (context, next) =>
{
    context.Request.Scheme = "https";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.ContentType != null && response.ContentType.Contains("application/json"))
        return;
    response.Redirect($"/error/{response.StatusCode}");
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ChatHub>("/chatHub");
app.Urls.Add("http://0.0.0.0:8080");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MilkStore4Context>();

    // ── BƯỚC 1: Tạo bảng ChatMessages ──────────────────────────────────────
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

    // ── BƯỚC 2: Tạo các bảng core ──────────────────────────────────────────
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
    CREATE TABLE IF NOT EXISTS ""Brands"" (""Id"" SERIAL PRIMARY KEY, ""Name"" VARCHAR(100) NOT NULL);
    CREATE TABLE IF NOT EXISTS ""Categories"" (""Id"" SERIAL PRIMARY KEY, ""Name"" VARCHAR(100) NOT NULL);
    CREATE TABLE IF NOT EXISTS ""Products"" (
        ""Id"" SERIAL PRIMARY KEY, ""ProductName"" VARCHAR(200) NOT NULL,
        ""Description"" TEXT, ""Price"" DECIMAL(18,2) NOT NULL,
        ""StockQuantity"" INT NOT NULL DEFAULT 0, ""ImageUrl"" VARCHAR(500),
        ""ExpiryDate"" DATE NOT NULL DEFAULT CURRENT_DATE,
        ""CategoryId"" INT NOT NULL, ""BrandId"" INT NOT NULL,
        CONSTRAINT ""FK_Products_Categories"" FOREIGN KEY(""CategoryId"") REFERENCES ""Categories""(""Id""),
        CONSTRAINT ""FK_Products_Brands"" FOREIGN KEY(""BrandId"") REFERENCES ""Brands""(""Id"")
    );
    CREATE TABLE IF NOT EXISTS ""Orders"" (
        ""Id"" SERIAL PRIMARY KEY, ""UserId"" INT NOT NULL,
        ""OrderDate"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        ""TotalAmount"" DECIMAL(18,2) NOT NULL,
        ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Pending',
        ""PaymentMethod"" VARCHAR(50) NOT NULL DEFAULT 'COD',
        ""ShippingAddress"" VARCHAR(500), ""Note"" VARCHAR(500),
        CONSTRAINT ""FK_Orders_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id"")
    );
    CREATE TABLE IF NOT EXISTS ""OrderItems"" (
        ""Id"" SERIAL PRIMARY KEY, ""OrderId"" INT NOT NULL, ""ProductId"" INT NOT NULL,
        ""Quantity"" INT NOT NULL DEFAULT 1, ""PriceAtTime"" DECIMAL(18,2) NOT NULL,
        CONSTRAINT ""FK_OrderItems_Orders"" FOREIGN KEY(""OrderId"") REFERENCES ""Orders""(""Id""),
        CONSTRAINT ""FK_OrderItems_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id"")
    );
    CREATE TABLE IF NOT EXISTS ""CartItems"" (
        ""Id"" SERIAL PRIMARY KEY, ""UserId"" INT NOT NULL, ""ProductId"" INT NOT NULL,
        ""Quantity"" INT NOT NULL DEFAULT 1,
        CONSTRAINT ""FK_CartItems_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id""),
        CONSTRAINT ""FK_CartItems_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id"")
    );
    CREATE TABLE IF NOT EXISTS ""Reviews"" (
        ""Id"" SERIAL PRIMARY KEY, ""ProductId"" INT NOT NULL, ""UserId"" INT NOT NULL,
        ""Rating"" INT NOT NULL DEFAULT 5, ""Comment"" TEXT,
        ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        ""IsAdminReply"" BOOLEAN NOT NULL DEFAULT FALSE, ""ParentReviewId"" INT,
        CONSTRAINT ""FK_Reviews_Products"" FOREIGN KEY(""ProductId"") REFERENCES ""Products""(""Id""),
        CONSTRAINT ""FK_Reviews_Users"" FOREIGN KEY(""UserId"") REFERENCES ""Users""(""Id"")
    );
");

    // ── BƯỚC 3: Thêm các cột mới (ADD COLUMN IF NOT EXISTS = an toàn) ──────
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""Orders""
            ADD COLUMN IF NOT EXISTS ""Phone"" VARCHAR(20)  DEFAULT NULL,
            ADD COLUMN IF NOT EXISTS ""Email"" VARCHAR(150) DEFAULT NULL;

        ALTER TABLE ""Users""
            ADD COLUMN IF NOT EXISTS ""BankAccountNumber"" VARCHAR(30)  DEFAULT NULL,
            ADD COLUMN IF NOT EXISTS ""BankName""          VARCHAR(100) DEFAULT NULL,
            ADD COLUMN IF NOT EXISTS ""OtpCode""           VARCHAR(10)  DEFAULT NULL,
            ADD COLUMN IF NOT EXISTS ""OtpIssuedAt""       TIMESTAMPTZ  DEFAULT NULL;
    ");

    // ── BƯỚC 4: Tạo bảng Coupons + seed dữ liệu test ───────────────────────
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Coupons"" (
            ""Id""            SERIAL PRIMARY KEY,
            ""Code""          VARCHAR(50)   NOT NULL UNIQUE,
            ""DiscountType""  VARCHAR(20)   NOT NULL DEFAULT 'Percent',
            ""DiscountValue"" DECIMAL(18,2) NOT NULL,
            ""ExpiryDate""    TIMESTAMPTZ   NOT NULL
        );
        INSERT INTO ""Coupons"" (""Code"",""DiscountType"",""DiscountValue"",""ExpiryDate"") VALUES
            ('SALE10',   'Percent', 10,    '2027-12-31'),
            ('OLD2024',  'Percent', 15,    '2024-01-01'),
            ('SUMMER20', 'Percent', 20,    '2027-12-31'),
            ('FIXED50K', 'Fixed',   50000, '2027-12-31')
        ON CONFLICT DO NOTHING;
    ");
}

app.Run();