using InventoryApp.Data;
using InventoryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// DbContext register করো
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// DbContext register করো
// DbContext register করো
var builderStr = new NpgsqlConnectionStringBuilder();

// রেন্ডার (ডাবল আন্ডারস্কোর) এবং লোকাল (কোলন) দুই ফরম্যাটই যেন সাপোর্ট করে তার নিখুঁত ব্যবস্থা
builderStr.Host = builder.Configuration["Supabase:Host"] ?? builder.Configuration["Supabase__Host"];
builderStr.Database = builder.Configuration["Supabase:Database"] ?? builder.Configuration["Supabase__Database"];
builderStr.Username = builder.Configuration["Supabase:Username"] ?? builder.Configuration["Supabase__Username"];
builderStr.Password = builder.Configuration["Supabase:Password"] ?? builder.Configuration["Supabase__Password"];

// পোর্টের খালি স্ট্রিং জনিত ক্র্যাশ দূর করার জন্য নিরাপদ পার্সিং
var portStr = builder.Configuration["Supabase:Port"] ?? builder.Configuration["Supabase__Port"];
if (string.IsNullOrWhiteSpace(portStr))
{
    portStr = "5432"; // ডিফল্ট পোর্ট
}
builderStr.Port = int.Parse(portStr);

// রেন্ডার পোর্ট ও ডিবি কন্টেক্সট রেজিস্ট্রেসশন
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builderStr.ConnectionString));

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

// প্রক্সি হেডার হ্যান্ডেল করার জন্য নিখুঁত কনফিগারেশন
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeaderOptions.KnownNetworks.Clear(); // রেন্ডার প্রক্সিকে ট্রাস্ট করার জন্য এটি আবশ্যক
forwardedHeaderOptions.KnownProxies.Clear();  // এটিও ক্লিয়ার করতে হবে
app.UseForwardedHeaders(forwardedHeaderOptions);

// বাকি কোড (যেমন: Admin role এবং প্রথম user setup) নিচে যেভাবে আছে থাকবে...
// Admin role এবং first user setup
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Admin role না থাকলে তৈরি করা
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