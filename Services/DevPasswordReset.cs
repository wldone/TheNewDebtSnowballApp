namespace DebtSnowballApp.Services;

using DebtSnowballApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class DevPasswordReset
{
    /// <summary>
    /// Resets a user's password in Development using Identity APIs.
    /// Set either email or userName. Leaves other account bits tidy.
    /// </summary>
    public static async Task ResetAsync(
        IServiceProvider services,
        string? email,
        string? userName,
        string newPassword)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevPasswordReset");
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1) Find the user (no EF expression-tree tricks)
        ApplicationUser? user = null;

        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await userManager.FindByEmailAsync(email);
        }

        if (user == null && !string.IsNullOrWhiteSpace(userName))
        {
            user = await userManager.FindByNameAsync(userName);
        }

        if (user == null)
        {
            logger.LogError("User not found. Email='{Email}', UserName='{UserName}'", email, userName);
            return;
        }

        // 2) Generate a reset token and reset the password
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!reset.Succeeded)
        {
            logger.LogError("Password reset failed: {Errors}",
                string.Join("; ", reset.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return;
        }

        // 3) Optional hygiene
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        // Optionally disable 2FA for local dev:
        // await userManager.SetTwoFactorEnabledAsync(user, false);

        logger.LogInformation("Password reset OK for Email='{Email}', UserName='{UserName}'",
            user.Email, user.UserName);
    }
}
