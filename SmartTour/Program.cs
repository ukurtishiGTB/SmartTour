using SmartTour.Data;
using SmartTour.Models;
using SmartTour.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ArangoDBHelper>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<PlaceService>();
builder.Services.AddSingleton<TripService>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddScoped<TripPlanningService>();
builder.Services.AddScoped<TripSuggestionService>();

// Add authentication services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthService>();

// Add cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict for better compatibility
        
    });

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    try
    {
        var arangoHelper = scope.ServiceProvider.GetRequiredService<ArangoDBHelper>();
        
        // Test connection first
        if (!await arangoHelper.TestConnectionAsync())
        {
            throw new Exception("Could not connect to ArangoDB. Please check your connection settings in appsettings.json");
        }
        
        await arangoHelper.EnsureCollectionsAsync();
        Console.WriteLine("[Program] Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Program] Error during database initialization: {ex.Message}");
        Console.WriteLine($"[Program] Stack trace: {ex.StackTrace}");
        // In development, you might want to throw the error to prevent the app from starting with an uninitialized database
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Make sure these are in the correct order
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();