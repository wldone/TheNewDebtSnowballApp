using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Models.Payoff;
using DebtSnowballApp.Models.Interfaces;
using DebtSnowballApp.Services;
using DebtSnowballApp.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "DebtSnowballAuth";
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // 👈 needed for Identity UI

builder.Services.AddScoped<SnowballCalculator>();
builder.Services.AddScoped(typeof(IDebtPayoffCalculator<>), typeof(DebtPayoffCalculator<>));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    // Optional rebuild before seeding
    var rebuild = app.Configuration.GetValue<bool>("Dev:RebuildDb");
    // DEV-ONLY rebuild (guard with your config flag)
    if (rebuild)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();

        // 1) increase command timeout for heavy ops
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        // 2) force-drop the database (kills other connections)
        try
        {
            var conn = db.Database.GetDbConnection();
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(conn.ConnectionString);

            var dbName = csb.InitialCatalog;
            csb.InitialCatalog = "master";

            await using var master = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
            await master.OpenAsync();

            var sql = $@"
                IF DB_ID(@db) IS NOT NULL
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END";
            await using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, master))
            {
                cmd.Parameters.AddWithValue("@db", dbName);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            // fine if it didn't exist; log other errors if you like
            Console.WriteLine("Force-drop note: " + ex.Message);
        }

        // 3) recreate schema
        await db.Database.MigrateAsync();
    }


    // Seed once here (dev only)
    // Program.cs (NET 6 minimal hosting)
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var ctx = sp.GetRequiredService<ApplicationDbContext>();
        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.SeedAsync(ctx, um, rm, seedDemoUsers: true);
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseRouting();
app.UseSession();           
app.UseAuthentication();
app.UseAuthorization();

// --- Routes ---
// Areas first, with a sane default controller (Home) and action (Index)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();


app.Run();
