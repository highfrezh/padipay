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
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private const long MaxDeliverableFileSize = 20 * 1024 * 1024; // 20MB

    public OrderController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int gigId)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return Unauthorized();
        }

        var gig = await _context.Gigs
            .Include(g => g.Category)
            .Include(g => g.Seller)
            .FirstOrDefaultAsync(g => g.Id == gigId);

        if (gig == null || !gig.IsActive)
        {
            TempData["ErrorMessage"] = "The requested gig is no longer available.";
            return RedirectToAction("Index", "Gig");
        }

        if (gig.SellerId == buyerId)
        {
            TempData["ErrorMessage"] = "You cannot order your own gig.";
            return RedirectToAction("Details", "Gig", new { id = gig.Id });
        }

        var buyerWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == buyerId);

        var viewModel = new CheckoutViewModel
        {
            GigId = gig.Id,
            Gig = gig,
            WalletBalance = buyerWallet?.Balance ?? 0m,
            Requirements = string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return Unauthorized();
        }

        var gig = await _context.Gigs
            .Include(g => g.Category)
            .Include(g => g.Seller)
            .FirstOrDefaultAsync(g => g.Id == model.GigId);

        if (gig == null || !gig.IsActive)
        {
            TempData["ErrorMessage"] = "The selected gig is no longer available.";
            return RedirectToAction("Index", "Gig");
        }

        if (gig.SellerId == buyerId)
        {
            TempData["ErrorMessage"] = "You cannot order your own gig.";
            return RedirectToAction("Details", "Gig", new { id = gig.Id });
        }

        var buyerWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == buyerId);

        if (buyerWallet == null || buyerWallet.Balance < gig.Price)
        {
            ModelState.AddModelError(string.Empty, "Insufficient wallet balance to fund escrow for this order.");
            model.Gig = gig;
            model.WalletBalance = buyerWallet?.Balance ?? 0m;
            return View("Checkout", model);
        }

        if (!ModelState.IsValid)
        {
            model.Gig = gig;
            model.WalletBalance = buyerWallet?.Balance ?? 0m;
            return View("Checkout", model);
        }

        // Execute atomic escrow lock transaction
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Debit buyer wallet
            buyerWallet.Balance -= gig.Price;

            // 2. Create Transaction audit record
            var transaction = new Transaction
            {
                BuyerId = buyerId,
                SellerId = gig.SellerId,
                Amount = gig.Price,
                Status = TransactionStatus.Funded,
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 3. Create Order
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var order = new Order
            {
                OrderNumber = orderNumber,
                GigId = gig.Id,
                BuyerId = buyerId,
                SellerId = gig.SellerId,
                Amount = gig.Price,
                DeliveryDays = gig.DeliveryDays,
                AllowedRevisions = gig.RevisionCount,
                RevisionsUsed = 0,
                Status = OrderStatus.InProgress,
                BuyerRequirements = model.Requirements.Trim(),
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(gig.DeliveryDays),
                TransactionId = transaction.Id
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            TempData["SuccessMessage"] = $"Order #{order.OrderNumber} placed! ₦{order.Amount:N0} has been secured in PadiPay escrow.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "An error occurred while funding escrow. Please try again.");
            model.Gig = gig;
            model.WalletBalance = buyerWallet.Balance;
            return View("Checkout", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "buying")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        ViewBag.ActiveTab = tab.ToLowerInvariant();

        var buyingOrders = await _context.Orders
            .Include(o => o.Gig)
            .Include(o => o.Seller)
            .Include(o => o.Deliveries)
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var sellingOrders = await _context.Orders
            .Include(o => o.Gig)
            .Include(o => o.Buyer)
            .Include(o => o.Deliveries)
            .Where(o => o.SellerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.BuyingOrders = buyingOrders;
        ViewBag.SellingOrders = sellingOrders;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Gig)
                .ThenInclude(g => g!.Category)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Transaction)
            .Include(o => o.Review)
            .Include(o => o.Deliveries.OrderByDescending(d => d.DeliveredAt))
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        var isBuyer = order.BuyerId == userId;
        var isSeller = order.SellerId == userId;
        var isAdmin = User.IsInRole("Admin");

        if (!isBuyer && !isSeller && !isAdmin)
        {
            return Forbid();
        }

        ViewBag.IsBuyer = isBuyer;
        ViewBag.IsSeller = isSeller;
        ViewBag.IsAdmin = isAdmin;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(DeliverOrderViewModel model)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Deliveries)
            .FirstOrDefaultAsync(o => o.Id == model.OrderId);

        if (order == null)
        {
            return NotFound();
        }

        if (order.SellerId != sellerId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.InProgress && order.Status != OrderStatus.InRevision)
        {
            TempData["ErrorMessage"] = "Order is not currently in a deliverable status.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        string? deliverableFileUrl = null;
        if (model.DeliverableFile != null && model.DeliverableFile.Length > 0)
        {
            if (model.DeliverableFile.Length > MaxDeliverableFileSize)
            {
                TempData["ErrorMessage"] = "Attachment file size cannot exceed 20 MB.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            deliverableFileUrl = await SaveDeliverableFileAsync(model.DeliverableFile);
        }

        var delivery = new OrderDelivery
        {
            OrderId = order.Id,
            SellerId = sellerId,
            Message = model.Message.Trim(),
            LivePreviewUrl = string.IsNullOrWhiteSpace(model.LivePreviewUrl) ? null : model.LivePreviewUrl.Trim(),
            GithubRepoUrl = string.IsNullOrWhiteSpace(model.GithubRepoUrl) ? null : model.GithubRepoUrl.Trim(),
            DeliverableFileUrl = deliverableFileUrl,
            DeliveredAt = DateTime.UtcNow
        };

        _context.OrderDeliveries.Add(delivery);

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Work delivered successfully! The buyer has been notified to review and release payment.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptDelivery(int id)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Transaction)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.BuyerId != buyerId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Delivered)
        {
            TempData["ErrorMessage"] = "Order must be in Delivered status to accept.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        var sellerWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == order.SellerId);

        if (sellerWallet == null)
        {
            sellerWallet = new Wallet
            {
                UserId = order.SellerId,
                Balance = 0m
            };
            _context.Wallets.Add(sellerWallet);
            await _context.SaveChangesAsync();
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Credit seller wallet with escrow amount
            sellerWallet.Balance += order.Amount;

            // 2. Update transaction status
            if (order.Transaction != null)
            {
                order.Transaction.Status = TransactionStatus.Released;
            }

            // 3. Mark order completed
            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            TempData["SuccessMessage"] = $"Delivery accepted! ₦{order.Amount:N0} has been released from escrow to {order.Seller?.FullName ?? "the seller"}.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            TempData["ErrorMessage"] = "An error occurred while releasing escrow funds. Please try again.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRevision(RequestRevisionViewModel model)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Deliveries.OrderByDescending(d => d.DeliveredAt))
            .FirstOrDefaultAsync(o => o.Id == model.OrderId);

        if (order == null)
        {
            return NotFound();
        }

        if (order.BuyerId != buyerId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Delivered)
        {
            TempData["ErrorMessage"] = "Order is not currently in a deliverable state for revision.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        if (order.RevisionsUsed >= order.AllowedRevisions)
        {
            TempData["ErrorMessage"] = $"You have reached the maximum allowed revisions ({order.AllowedRevisions}) for this order.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        order.RevisionsUsed++;
        order.Status = OrderStatus.InRevision;

        var latestDelivery = order.Deliveries.FirstOrDefault();
        if (latestDelivery != null)
        {
            latestDelivery.RevisionNotes = model.RevisionNotes.Trim();
            latestDelivery.RevisionRequestedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Revision requested! The seller has been notified with your feedback.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    private async Task<string> SaveDeliverableFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "deliveries");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return $"/uploads/deliveries/{uniqueFileName}";
    }
}
