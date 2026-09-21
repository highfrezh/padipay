using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.ViewModels;

namespace PadiPay.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Review/Create?orderId=5
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Gig)
            .Include(o => o.Seller)
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        if (order.BuyerId != currentUserId)
        {
            TempData["ErrorMessage"] = "Only the buyer can review this order.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        if (order.Status != OrderStatus.Completed)
        {
            TempData["ErrorMessage"] = "You can only leave a review once the order delivery has been accepted and completed.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        if (order.Review != null)
        {
            TempData["ErrorMessage"] = "You have already reviewed this order.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        var viewModel = new CreateReviewViewModel
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            GigId = order.GigId,
            GigTitle = order.Gig?.Title ?? "Service Delivery",
            GigCoverImageUrl = order.Gig?.CoverImageUrl,
            SellerId = order.SellerId,
            SellerName = order.Seller?.FullName ?? "Freelancer",
            SellerHeadline = order.Seller?.Headline,
            SellerPictureUrl = order.Seller?.ProfilePictureUrl,
            Rating = 5,
            Comment = string.Empty
        };

        return View(viewModel);
    }

    // POST: /Review/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewViewModel model)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Gig)
            .Include(o => o.Seller)
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.Id == model.OrderId);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        if (order.BuyerId != currentUserId)
        {
            TempData["ErrorMessage"] = "Only the buyer can review this order.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        if (order.Status != OrderStatus.Completed)
        {
            TempData["ErrorMessage"] = "You can only leave a review once the order delivery has been accepted and completed.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        if (order.Review != null)
        {
            TempData["ErrorMessage"] = "You have already reviewed this order.";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }

        if (model.Rating < 1 || model.Rating > 5)
        {
            ModelState.AddModelError(nameof(model.Rating), "Please select a rating between 1 and 5 stars.");
        }

        if (!ModelState.IsValid)
        {
            model.OrderNumber = order.OrderNumber;
            model.GigTitle = order.Gig?.Title ?? "Service Delivery";
            model.GigCoverImageUrl = order.Gig?.CoverImageUrl;
            model.SellerName = order.Seller?.FullName ?? "Freelancer";
            model.SellerHeadline = order.Seller?.Headline;
            model.SellerPictureUrl = order.Seller?.ProfilePictureUrl;
            return View(model);
        }

        var review = new Review
        {
            OrderId = order.Id,
            GigId = order.GigId,
            BuyerId = currentUserId,
            SellerId = order.SellerId,
            Rating = model.Rating,
            Comment = model.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Thank you! Your {model.Rating}-star review for {order.Seller?.FullName ?? "the freelancer"} has been published.";
        return RedirectToAction("Details", "Order", new { id = order.Id });
    }
}
