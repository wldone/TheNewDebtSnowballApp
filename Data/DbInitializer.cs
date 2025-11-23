// DbInitializer.cs
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        bool seedDemoUsers = true,
        CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);
        // 0) Flags (optional: drive from appsettings)
        bool seedDemo = true;
        const string adminEmail = "admin@example.com";
        const string demoEmail = "demo@example.com";
        const string defaultPw = "Passw0rd!";

        // 1) Roles
        var roles = new[] { "Admin", "User" };
        foreach (var r in roles)
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole(r));

        // 2) Users (create if missing, then add roles)
        async Task<ApplicationUser> EnsureUserAsync(string email, string role)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u == null)
            {
                u = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };
                var create = await userManager.CreateAsync(u, defaultPw);
                if (!create.Succeeded)
                    throw new Exception("User create failed: " + string.Join("; ", create.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrEmpty(role) && !await userManager.IsInRoleAsync(u, role))
                await userManager.AddToRoleAsync(u, role);

            return u;
        }
        // === Seed Partners (idempotent by Code) ===
        var desiredPartners = new[]
        {
            new Partner { Name = "Demo Partner",     Code = "DEMO", Active = true, SortOrder = 10 },
            new Partner { Name = "Merrick Bank",     Code = "MB",   Active = true, SortOrder = 20 },
            new Partner { Name = "Contoso Finance",  Code = "CON",  Active = true, SortOrder = 30 },
            new Partner { Name = "Acme Credit",      Code = "ACME", Active = true, SortOrder = 40 },
        };

        var existingCodes = await context.Partners
            .AsNoTracking()
            .Select(p => p.Code)
            .ToListAsync(ct);

        var toInsert = desiredPartners.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (toInsert.Count > 0)
        {
            context.Partners.AddRange(toInsert);
            await context.SaveChangesAsync(ct);
        }
        var adminUser = await EnsureUserAsync(adminEmail, "Admin");
        var demoUser = await EnsureUserAsync(demoEmail, "User");

        // Build a lookup after inserts
        var partnersByCode = await context.Partners
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);  // Code -> int

        // === Assign seeded users to Partners (once) ===
        async Task AssignUserToPartnerAsync(string email, string partnerCode)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return;
            if (!partnersByCode.TryGetValue(partnerCode, out int partnerId)) return;

            if (user.PartnerId != partnerId)
            {
                user.PartnerId = partnerId;
                var res = await userManager.UpdateAsync(user);
                if (!res.Succeeded)
                    throw new Exception($"Failed to set PartnerId for {email}: {string.Join("; ", res.Errors.Select(e => e.Description))}");
            }
        }

        // map your seeded users
        await AssignUserToPartnerAsync("admin@example.com", "MB");
        await AssignUserToPartnerAsync("demo@example.com", "DEMO");
        // (optional) your personal dev login → DEMO, etc.
        // await AssignUserToPartnerAsync("you@yourmail.com", "DEMO");


        // 3) DebtTypes (fixed IDs, non-identity)
        // Ensure your model: [DatabaseGenerated(DatabaseGeneratedOption.None)]
        if (!await context.DebtTypes.AnyAsync(ct))
        {
            context.DebtTypes.AddRange(
                new DebtType { Id = 1, Description = "Credit Card", IsActive = true, SortOrder = 0 },
                new DebtType { Id = 2, Description = "Auto Loan", IsActive = true, SortOrder = 0 },
                new DebtType { Id = 3, Description = "Personal Loan", IsActive = true, SortOrder = 0 },
                new DebtType { Id = 4, Description = "Student Loan", IsActive = true, SortOrder = 0 }
            );
            await context.SaveChangesAsync(ct);
        }

        var typeByDesc = await context.DebtTypes.AsNoTracking()
            .ToDictionaryAsync(x => x.Description, x => x.Id, ct);

        // 4) DebtItems (only if none exist for a user)
        async Task SeedDebtsForAsync(ApplicationUser user)
        {
            bool hasDebts = await context.DebtItems.AnyAsync(d => d.UserId == user.Id, ct);
            if (hasDebts) return;

            var now = DateTime.UtcNow;
            var debts = new List<DebtItem>
            {
                new DebtItem {
                    Name = "Visa Credit Card", Balance = 3200m, BegBalance = 3200m,
                    MinimumPayment = 85m, InterestRate = 18.99m,
                    CreationDate = now, LastUpdate = now, UserId = user.Id,
                    DebtTypeId = typeByDesc["Credit Card"]
                },
                new DebtItem {
                    Name = "Auto Loan", Balance = 7800m, BegBalance = 7800m,
                    MinimumPayment = 265m, InterestRate = 6.5m,
                    CreationDate = now, LastUpdate = now, UserId = user.Id,
                    DebtTypeId = typeByDesc["Auto Loan"]
                },
                new DebtItem {
                    Name = "Personal Loan", Balance = 2200m, BegBalance = 2200m,
                    MinimumPayment = 95m, InterestRate = 11.2m,
                    CreationDate = now, LastUpdate = now, UserId = user.Id,
                    DebtTypeId = typeByDesc["Personal Loan"]
                },
                new DebtItem {
                    Name = "Student Loan", Balance = 12500m, BegBalance = 12500m,
                    MinimumPayment = 140m, InterestRate = 4.99m,
                    CreationDate = now, LastUpdate = now, UserId = user.Id,
                    DebtTypeId = typeByDesc["Student Loan"]
                }
            };

            context.DebtItems.AddRange(debts);
            await context.SaveChangesAsync(ct);
        }

        if (seedDemo)
        {
            await SeedDebtsForAsync(adminUser);
            await SeedDebtsForAsync(demoUser);
        }
    }
}
