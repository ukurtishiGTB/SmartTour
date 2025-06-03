using SmartTour.Data;
using SmartTour.Models;
using SmartTour.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ArangoDBHelper>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<PlaceService>();
builder.Services.AddSingleton<TripService>();
builder.Services.AddSingleton<RecommendationService>();


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var arangoHelper = scope.ServiceProvider.GetRequiredService<ArangoDBHelper>();
    await arangoHelper.EnsureCollectionsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();