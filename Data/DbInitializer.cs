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

        // 4. Seed Demo Freelancers and Buyer if needed
        const string seller1Email = "chinedu@padipay.ng";
        var seller1 = await userManager.FindByEmailAsync(seller1Email);
        if (seller1 == null)
        {
            seller1 = new ApplicationUser
            {
                UserName = seller1Email,
                Email = seller1Email,
                EmailConfirmed = true,
                FullName = "Chinedu Okafor",
                Headline = "Senior .NET & Azure Cloud Architect",
                Bio = "Over 7 years building scalable enterprise microservices, ASP.NET Core APIs, and cloud-native solutions for fintech and e-commerce across Africa.",
                Skills = "C#, ASP.NET Core, .NET 10, Azure, Docker, SQL Server, Redis, Clean Architecture",
                Location = "Lekki, Lagos",
                IsSeller = true
            };
            var res = await userManager.CreateAsync(seller1, "Talent@123456");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(seller1, "User");
                context.Wallets.Add(new Wallet { UserId = seller1.Id, Balance = 250000.00m });
                await context.SaveChangesAsync();
            }
        }

        const string seller2Email = "amina@padipay.ng";
        var seller2 = await userManager.FindByEmailAsync(seller2Email);
        if (seller2 == null)
        {
            seller2 = new ApplicationUser
            {
                UserName = seller2Email,
                Email = seller2Email,
                EmailConfirmed = true,
                FullName = "Amina Bello",
                Headline = "Lead UI/UX & Design Systems Designer",
                Bio = "Passionate product designer transforming complex fintech and SaaS workflows into sleek, intuitive interfaces with Figma, interactive prototypes, and atomic design systems.",
                Skills = "Figma, UI/UX Design, Wireframing, Mobile App UX, Design Systems, User Research",
                Location = "Abuja, Nigeria",
                IsSeller = true
            };
            var res = await userManager.CreateAsync(seller2, "Talent@123456");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(seller2, "User");
                context.Wallets.Add(new Wallet { UserId = seller2.Id, Balance = 180000.00m });
                await context.SaveChangesAsync();
            }
        }

        const string buyerEmail = "emeka@padipay.ng";
        var buyer = await userManager.FindByEmailAsync(buyerEmail);
        if (buyer == null)
        {
            buyer = new ApplicationUser
            {
                UserName = buyerEmail,
                Email = buyerEmail,
                EmailConfirmed = true,
                FullName = "Emeka Johnson",
                Headline = "Founder & CTO at PayBridge Africa",
                Bio = "Building B2B payments infrastructure. Regularly hiring top-tier Nigerian engineers and designers through PadiPay escrow.",
                Skills = "Product Management, Fintech, System Design",
                Location = "Victoria Island, Lagos",
                IsSeller = false
            };
            var res = await userManager.CreateAsync(buyer, "Buyer@123456");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(buyer, "User");
                context.Wallets.Add(new Wallet { UserId = buyer.Id, Balance = 500000.00m });
                await context.SaveChangesAsync();
            }
        }

        // 5. Seed Demo Gigs
        if (!await context.Gigs.AnyAsync())
        {
            var webDevCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "web-development");
            var uiuxCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "ui-ux-design");

            if (seller1 != null && webDevCat != null)
            {
                var gig1 = new Gig
                {
                    Title = "I will build enterprise ASP.NET Core Web APIs and Microservices with Clean Architecture",
                    Slug = "build-enterprise-aspnet-core-web-apis",
                    Description = "Get production-ready, clean, maintainable backend architecture built with ASP.NET Core (.NET 10), EF Core, SQL Server, and Docker.\n\nIncluded features:\n- Domain-Driven Design & CQRS\n- JWT Authentication & Role-Based Authorization\n- Automated Unit & Integration Tests\n- Comprehensive Swagger / OpenAPI documentation\n- CI/CD ready Dockerfile",
                    CategoryId = webDevCat.Id,
                    SellerId = seller1.Id,
                    Price = 120000.00m,
                    DeliveryDays = 5,
                    RevisionCount = 3,
                    Requirements = "1. Technical specification document or feature list\n2. Database requirements (if any)\n3. Target hosting environment",
                    Tags = "aspnet, csharp, dotnet, api, sql, backend, clean-architecture",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                };
                context.Gigs.Add(gig1);
                await context.SaveChangesAsync();

                // Seed completed order and review
                if (buyer != null)
                {
                    var tx = new Transaction
                    {
                        BuyerId = buyer.Id,
                        SellerId = seller1.Id,
                        Amount = gig1.Price,
                        Status = TransactionStatus.Released,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    };
                    context.Transactions.Add(tx);
                    await context.SaveChangesAsync();

                    var order = new Order
                    {
                        OrderNumber = "ORD-20260901-7F2A19",
                        GigId = gig1.Id,
                        BuyerId = buyer.Id,
                        SellerId = seller1.Id,
                        Amount = gig1.Price,
                        DeliveryDays = gig1.DeliveryDays,
                        AllowedRevisions = gig1.RevisionCount,
                        RevisionsUsed = 0,
                        Status = OrderStatus.Completed,
                        BuyerRequirements = "Need a high-performance payment gateway webhook consumer service with idempotent request handling.",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        DueDate = DateTime.UtcNow.AddDays(-5),
                        DeliveredAt = DateTime.UtcNow.AddDays(-6),
                        CompletedAt = DateTime.UtcNow.AddDays(-6),
                        TransactionId = tx.Id
                    };
                    context.Orders.Add(order);
                    await context.SaveChangesAsync();

                    var review = new Review
                    {
                        OrderId = order.Id,
                        GigId = gig1.Id,
                        BuyerId = buyer.Id,
                        SellerId = seller1.Id,
                        Rating = 5,
                        Comment = "Chinedu is an extraordinary backend engineer! The .NET 10 microservice was delivered 2 days ahead of schedule, with immaculate clean architecture, zero lint warnings, and 95% test coverage. Escrow release was smooth!",
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    };
                    context.Reviews.Add(review);
                    await context.SaveChangesAsync();
                }
            }

            if (seller2 != null && uiuxCat != null)
            {
                var gig2 = new Gig
                {
                    Title = "I will design modern Fintech & SaaS Mobile App UI/UX in Figma with Interactive Prototypes",
                    Slug = "design-modern-fintech-saas-mobile-app-uiux",
                    Description = "Elevate your Nigerian tech startup with world-class user interfaces designed specifically for mobile responsiveness and high conversion.\n\nIncluded deliverables:\n- Pixel-perfect Figma design source file\n- Complete Typography & Color design token system\n- Clickable mobile prototype\n- Developer handoff assets with exported SVG icons",
                    CategoryId = uiuxCat.Id,
                    SellerId = seller2.Id,
                    Price = 85000.00m,
                    DeliveryDays = 4,
                    RevisionCount = 3,
                    Requirements = "1. App idea overview & user target persona\n2. Brand colors and logo (if available)\n3. Competitor or reference apps you admire",
                    Tags = "figma, ui-ux, mobile-design, fintech, prototype, design-system",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                };
                context.Gigs.Add(gig2);
                await context.SaveChangesAsync();

                if (buyer != null)
                {
                    var tx = new Transaction
                    {
                        BuyerId = buyer.Id,
                        SellerId = seller2.Id,
                        Amount = gig2.Price,
                        Status = TransactionStatus.Released,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    };
                    context.Transactions.Add(tx);
                    await context.SaveChangesAsync();

                    var order = new Order
                    {
                        OrderNumber = "ORD-20260905-9C4B33",
                        GigId = gig2.Id,
                        BuyerId = buyer.Id,
                        SellerId = seller2.Id,
                        Amount = gig2.Price,
                        DeliveryDays = gig2.DeliveryDays,
                        AllowedRevisions = gig2.RevisionCount,
                        RevisionsUsed = 1,
                        Status = OrderStatus.Completed,
                        BuyerRequirements = "Need a 5-screen onboarding and KYC verification flow for our new payments app.",
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                        DueDate = DateTime.UtcNow.AddDays(-4),
                        DeliveredAt = DateTime.UtcNow.AddDays(-4),
                        CompletedAt = DateTime.UtcNow.AddDays(-3),
                        TransactionId = tx.Id
                    };
                    context.Orders.Add(order);
                    await context.SaveChangesAsync();

                    var review = new Review
                    {
                        OrderId = order.Id,
                        GigId = gig2.Id,
                        BuyerId = buyer.Id,
                        SellerId = seller2.Id,
                        Rating = 5,
                        Comment = "Amina's Figma designs are world-class! The dark mode palettes and micro-interactions exceeded our expectations. Our dev team implemented the screens in record time.",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    };
                    context.Reviews.Add(review);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
