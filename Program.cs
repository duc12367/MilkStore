using Microsoft.EntityFrameworkCore;
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
    options.IdleTimeout = TimeSpan.FromHours(2);   // tăng lên 2 giờ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;        // cần cho MoMo redirect
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();

// Areas route phải khai báo TRƯỚC default
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Urls.Add("http://0.0.0.0:8080");
app.Run();