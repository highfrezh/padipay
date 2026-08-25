using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PadiPay.Models;

namespace PadiPay.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply pending migrations automatically
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Default Admin User
        const string adminEmail = "admin@padipay.ng";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "PadiPay Admin",
                Headline = "Chief Platform Administrator",
                Bio = "Overseeing platform trust, disputes, and marketplace security.",
                Skills = "Platform Management, Escrow Mediation",
                Location = "Lagos, Nigeria",
                IsSeller = false
            };

            var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Seed Admin Wallet
                var adminWallet = new Wallet
                {
                    UserId = adminUser.Id,
                    Balance = 1000000.00m // Default seed funding for admin operations
                };
                context.Wallets.Add(adminWallet);
                await context.SaveChangesAsync();
            }
        }

        // 3. Seed Tech Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new()
                {
                    Name = "Web Development",
                    Slug = "web-development",
                    Description = "ASP.NET Core, React, Next.js, Node.js, Python/Django & Full-Stack Solutions",
                    Icon = "bi-code-slash",
                    DisplayOrder = 1
                },
                new()
                {
                    Name = "Mobile App Development",
                    Slug = "mobile-apps",
                    Description = "Cross-platform Flutter & React Native, Native Android (Kotlin) & iOS (Swift)",
                    Icon = "bi-phone",
                    DisplayOrder = 2
                },
                new()
                {
                    Name = "UI/UX & Product Design",
                    Slug = "ui-ux-design",
                    Description = "Figma Prototypes, Design Systems, Mobile App & SaaS Dashboard Design",
                    Icon = "bi-palette",
                    DisplayOrder = 3
                },
                new()
                {
                    Name = "Cloud, DevOps & Infrastructure",
                    Slug = "cloud-devops",
                    Description = "AWS, Azure, Docker, Kubernetes, CI/CD Pipelines & Server Management",
                    Icon = "bi-cloud-check",
                    DisplayOrder = 4
                },
                new()
                {
                    Name = "Data Science, AI & Machine Learning",
                    Slug = "data-ai",
                    Description = "Data Analysis, ML Pipelines, LLM Integrations, SQL Database Optimization",
                    Icon = "bi-cpu",
                    DisplayOrder = 5
                },
                new()
                {
                    Name = "Technical Writing & API Docs",
                    Slug = "technical-writing",
                    Description = "Developer Documentation, API Specs, Engineering Blogs & Technical Guides",
                    Icon = "bi-journal-code",
                    DisplayOrder = 6
                },
                new()
                {
                    Name = "Cybersecurity & Code Audit",
                    Slug = "cybersecurity",
                    Description = "Security Audits, Penetration Testing, OWASP Compliance & Vulnerability Scanning",
                    Icon = "bi-shield-check",
                    DisplayOrder = 7
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }
    }
}
