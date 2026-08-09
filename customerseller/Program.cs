using customerseller.Models; // Namespace sahi kar diya aapke project ke mutabik
using Microsoft.EntityFrameworkCore;
using customerseller.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session fix - 30 din tak rahegi
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".customerseller.Session";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserEmail")))
    {
        var emailCookie = context.Request.Cookies["UserEmail"];
        var nameCookie = context.Request.Cookies["UserName"];

        if (!string.IsNullOrEmpty(emailCookie))
        {
            context.Session.SetString("UserEmail", emailCookie);
            context.Session.SetString("UserName", nameCookie ?? "");
            var roleCookie = context.Request.Cookies["UserRole"];
            context.Session.SetString("UserRole", roleCookie ?? "Customer");

            // ShopName bhi restore karo
            var shopCookie = context.Request.Cookies["ShopName"];
            if (!string.IsNullOrEmpty(shopCookie))
                context.Session.SetString("ShopName", shopCookie);
        }
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/chatHub");

app.Run();