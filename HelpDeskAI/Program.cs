using HelpDeskAI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true).AddEnvironmentVariables().Build();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Login"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Home/AccessDenied";
    });
builder.Services.AddScoped<UserDataAccess>(
    provider => new UserDataAccess(config.GetValue<string>("DBConnectionString"), config.GetValue<string>("UserTableName")));
builder.Services.AddScoped<ChatDataAccess>(
    provider => new ChatDataAccess(config.GetValue<string>("DBConnectionString"), config.GetValue<string>("RoomTableName"), config.GetValue<string>("ChatTableName")));
builder.Services.AddScoped<MailService>(
    provider => new MailService(config.GetValue<string>("Smtp-Server"), config.GetValue<int>("Smtp-Port"), config.GetValue<string>("Smtp-Username"), config.GetValue<string>("Smtp-Password")));
builder.Services.AddScoped<ChatHub>(
    provider => new ChatHub(config.GetValue<string>("Gemini-Apikey"), provider.GetRequiredService<ChatDataAccess>()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/chatHub");

app.Run();
