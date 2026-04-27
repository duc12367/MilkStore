using Microsoft.EntityFrameworkCore;
using MilkStore.Hubs;
using MilkStore.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 10;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MilkStore4Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MilkStore")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

//app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.ContentType != null &&
        response.ContentType.Contains("application/json"))
    {
        return;
    }

    response.Redirect($"/error/{response.StatusCode}");
});
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
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

// Tự tạo bảng nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MilkStore4Context>();
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

    db.Database.ExecuteSqlRaw(@"
    ALTER TABLE ""Reviews"" ADD COLUMN IF NOT EXISTS ""IsAdminReply"" BOOLEAN NOT NULL DEFAULT FALSE;
    ALTER TABLE ""Reviews"" ADD COLUMN IF NOT EXISTS ""ParentReviewId"" INTEGER NULL;
    ");
}
app.Run();