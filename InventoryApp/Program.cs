using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext register করো
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity register করো
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Google + Facebook OAuth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
        options.Fields.Add("email");
        options.Fields.Add("name");
    });

// Localization
builder.Services.AddLocalization(options =>
    options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "bn" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});
// MVC register করো
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();
// Admin role এবং প্রথম user setup
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Admin role না থাকলে তৈরি করো
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // প্রথম user কে Admin বানাও
    var firstUser = userManager.Users.FirstOrDefault();
    if (firstUser != null && !await userManager.IsInRoleAsync(firstUser, "Admin"))
    {
        await userManager.AddToRoleAsync(firstUser, "Admin");
    }
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Language middleware
app.Use(async (context, next) =>
{
    var lang = context.Request.Cookies["language"] ?? "en";
    var culture = new System.Globalization.CultureInfo(lang);
    System.Globalization.CultureInfo.CurrentCulture = culture;
    System.Globalization.CultureInfo.CurrentUICulture = culture;
    await next();
});
app.UseRouting();

app.UseAuthentication(); // ← এটা অবশ্যই UseAuthorization এর আগে
app.UseAuthorization();
app.MapHub<InventoryApp.Hubs.CommentHub>("/commentHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();