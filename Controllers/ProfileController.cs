using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.ViewModels;

namespace PadiPay.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeInBytes = 3 * 1024 * 1024; // 3MB

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    [Authorize]
    public IActionResult Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Details), new { id = currentUserId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string? id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var targetUserId = id;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            targetUserId = currentUserId;
        }

        var user = await _context.Users
            .Include(u => u.Gigs.Where(g => g.IsActive))
                .ThenInclude(g => g.Category)
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (user == null)
        {
            return NotFound("User profile not found.");
        }

        var isOwner = !string.IsNullOrEmpty(currentUserId) && currentUserId == user.Id;

        var viewModel = new UserProfileViewModel
        {
            User = user,
            ActiveGigs = user.Gigs.OrderByDescending(g => g.CreatedAt).ToList(),
            IsOwner = isOwner
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditProfileViewModel
        {
            FullName = user.FullName,
            Headline = user.Headline,
            Bio = user.Bio,
            Skills = user.Skills,
            Location = user.Location,
            CurrentProfilePictureUrl = user.ProfilePictureUrl
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user == null)
        {
            return NotFound();
        }

        if (model.ProfilePicture != null)
        {
            var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "Only .jpg, .jpeg, .png, and .webp images are allowed.");
            }
            else if (model.ProfilePicture.Length > MaxFileSizeInBytes)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "Image size cannot exceed 3 MB.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            user.ProfilePictureUrl = await SaveProfilePictureAsync(model.ProfilePicture);
        }

        user.FullName = model.FullName.Trim();
        user.Headline = model.Headline.Trim();
        user.Bio = model.Bio?.Trim() ?? string.Empty;
        user.Skills = model.Skills?.Trim() ?? string.Empty;
        user.Location = model.Location.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        TempData["SuccessMessage"] = "Your profile has been updated successfully!";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    private async Task<string> SaveProfilePictureAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return $"/uploads/profiles/{uniqueFileName}";
    }
}
