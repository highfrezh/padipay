using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.ViewModels;

namespace PadiPay.Controllers;

public class GigController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public GigController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Gig/
    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, string? sort)
    {
        var query = _context.Gigs
            .Include(g => g.Category)
            .Include(g => g.Seller)
            .Where(g => g.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(g => g.Title.ToLower().Contains(s) ||
                                     g.Description.ToLower().Contains(s) ||
                                     g.Tags.ToLower().Contains(s));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(g => g.CategoryId == categoryId.Value);
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(g => g.Price),
            "price_desc" => query.OrderByDescending(g => g.Price),
            "fastest" => query.OrderBy(g => g.DeliveryDays),
            _ => query.OrderByDescending(g => g.CreatedAt)
        };

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSort = sort;

        var gigs = await query.ToListAsync();
        return View(gigs);
    }

    // GET: /Gig/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var gig = await _context.Gigs
            .Include(g => g.Category)
            .Include(g => g.Seller)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gig == null)
        {
            return NotFound();
        }

        return View(gig);
    }

    // GET: /Gig/MyGigs
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyGigs()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var gigs = await _context.Gigs
            .Include(g => g.Category)
            .Where(g => g.SellerId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return View(gigs);
    }

    // GET: /Gig/Create
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateGigViewModel
        {
            Categories = await GetCategorySelectListAsync()
        };

        return View(model);
    }

    // POST: /Gig/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGigViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }

        string? coverImagePath = null;
        if (model.CoverImage != null && model.CoverImage.Length > 0)
        {
            coverImagePath = await SaveCoverImageAsync(model.CoverImage);
        }

        var gig = new Gig
        {
            Title = model.Title.Trim(),
            Slug = GenerateSlug(model.Title),
            CategoryId = model.CategoryId,
            Description = model.Description.Trim(),
            Price = model.Price,
            DeliveryDays = model.DeliveryDays,
            RevisionCount = model.RevisionCount,
            Requirements = model.Requirements.Trim(),
            Tags = model.Tags?.Trim() ?? string.Empty,
            CoverImageUrl = coverImagePath,
            SellerId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Gigs.Add(gig);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your gig has been published successfully!";
        return RedirectToAction(nameof(MyGigs));
    }

    // GET: /Gig/Edit/5
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var gig = await _context.Gigs.FirstOrDefaultAsync(g => g.Id == id);
        if (gig == null)
        {
            return NotFound();
        }

        // Ownership Authorization Check
        if (gig.SellerId != userId)
        {
            return Forbid();
        }

        var model = new EditGigViewModel
        {
            Id = gig.Id,
            Title = gig.Title,
            CategoryId = gig.CategoryId,
            Description = gig.Description,
            Price = gig.Price,
            DeliveryDays = gig.DeliveryDays,
            RevisionCount = gig.RevisionCount,
            Requirements = gig.Requirements,
            Tags = gig.Tags,
            ExistingCoverImageUrl = gig.CoverImageUrl,
            IsActive = gig.IsActive,
            Categories = await GetCategorySelectListAsync()
        };

        return View(model);
    }

    // POST: /Gig/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditGigViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var gig = await _context.Gigs.FirstOrDefaultAsync(g => g.Id == id);
        if (gig == null)
        {
            return NotFound();
        }

        if (gig.SellerId != userId)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategorySelectListAsync();
            model.ExistingCoverImageUrl = gig.CoverImageUrl;
            return View(model);
        }

        if (model.CoverImage != null && model.CoverImage.Length > 0)
        {
            gig.CoverImageUrl = await SaveCoverImageAsync(model.CoverImage);
        }

        gig.Title = model.Title.Trim();
        gig.Slug = GenerateSlug(model.Title);
        gig.CategoryId = model.CategoryId;
        gig.Description = model.Description.Trim();
        gig.Price = model.Price;
        gig.DeliveryDays = model.DeliveryDays;
        gig.RevisionCount = model.RevisionCount;
        gig.Requirements = model.Requirements.Trim();
        gig.Tags = model.Tags?.Trim() ?? string.Empty;
        gig.IsActive = model.IsActive;
        gig.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gig updated successfully!";
        return RedirectToAction(nameof(MyGigs));
    }

    // POST: /Gig/ToggleStatus/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var gig = await _context.Gigs.FirstOrDefaultAsync(g => g.Id == id);
        if (gig == null)
        {
            return NotFound();
        }

        if (gig.SellerId != userId)
        {
            return Forbid();
        }

        gig.IsActive = !gig.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = gig.IsActive ? "Gig is now Active." : "Gig has been Paused.";
        return RedirectToAction(nameof(MyGigs));
    }

    #region Private Helper Methods

    private async Task<IEnumerable<SelectListItem>> GetCategorySelectListAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }

    private async Task<string> SaveCoverImageAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "gigs");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return $"/uploads/gigs/{uniqueFileName}";
    }

    private static string GenerateSlug(string phrase)
    {
        var str = phrase.ToLowerInvariant();
        // remove invalid chars
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // convert multiple spaces into one space
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // cut and trim
        str = str.Substring(0, str.Length <= 60 ? str.Length : 60).Trim();
        // replace spaces with hyphen
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }

    #endregion
}
