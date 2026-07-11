using Microsoft.AspNetCore.Identity;

namespace LibraryApp.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles = { "Admin", "Employee", "Customer" };

    public static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }
}