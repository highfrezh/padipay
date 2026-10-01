using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.Services;
using PadiPay.ViewModels;

namespace PadiPay.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly INotificationService _notificationService;
    private const long MaxDeliverableFileSize = 20 * 1024 * 1024; // 20MB

    public OrderController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment,
        INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
        _notificationService = notificationService;
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

            // Send notification to seller
            await _notificationService.SendNotificationAsync(
                gig.SellerId,
                "New Order Received! 📦",
                $"You have a new order (#{order.OrderNumber}) for '{gig.Title}'. Escrow amount: ₦{order.Amount:N0}.",
                Url.Action(nameof(Details), "Order", new { id = order.Id }),
                NotificationType.OrderPlaced);

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
            .Include(o => o.Dispute)
            .Include(o => o.Deliveries.OrderByDescending(d => d.DeliveredAt))
            .Include(o => o.Messages.OrderBy(m => m.SentAt))
                .ThenInclude(m => m.Sender)
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
            .Include(o => o.Buyer)
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

        // Notify Buyer
        await _notificationService.SendNotificationAsync(
            order.BuyerId,
            "Deliverables Submitted! 🚀",
            $"The freelancer has delivered work for order #{order.OrderNumber}. Please review and approve payment or request a revision.",
            Url.Action(nameof(Details), "Order", new { id = order.Id }),
            NotificationType.WorkDelivered);

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

            // Notify Seller of payout
            await _notificationService.SendNotificationAsync(
                order.SellerId,
                "Order Completed & Payout Released! 💰",
                $"Buyer accepted delivery for #{order.OrderNumber}! ₦{order.Amount:N0} has been credited to your wallet balance.",
                Url.Action(nameof(Details), "Order", new { id = order.Id }),
                NotificationType.OrderCompleted);

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
            .Include(o => o.Seller)
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

        // Notify Seller of revision
        await _notificationService.SendNotificationAsync(
            order.SellerId,
            "Revision Requested 🔄",
            $"The buyer has requested changes on order #{order.OrderNumber} (Revision {order.RevisionsUsed}/{order.AllowedRevisions}).",
            Url.Action(nameof(Details), "Order", new { id = order.Id }),
            NotificationType.RevisionRequested);

        TempData["SuccessMessage"] = "Revision requested! The seller has been notified with your feedback.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseDispute(RaiseDisputeViewModel model)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Dispute)
            .Include(o => o.Seller)
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
            TempData["ErrorMessage"] = "A dispute can only be raised on an order that has been delivered but not yet accepted.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        if (order.Dispute != null)
        {
            TempData["ErrorMessage"] = "A dispute has already been raised for this order.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all required fields before submitting the dispute.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        var dispute = new OrderDispute
        {
            OrderId = order.Id,
            InitiatorId = buyerId,
            Reason = model.Reason.Trim(),
            Explanation = model.Explanation.Trim(),
            Status = DisputeStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.OrderDisputes.Add(dispute);
        order.Status = OrderStatus.Disputed;
        await _context.SaveChangesAsync();

        // Notify Seller
        await _notificationService.SendNotificationAsync(
            order.SellerId,
            "Dispute Filed ⚖️",
            $"A dispute was filed by the buyer on order #{order.OrderNumber}. Funds remain locked pending admin review.",
            Url.Action(nameof(Details), "Order", new { id = order.Id }),
            NotificationType.DisputeRaised);

        TempData["SuccessMessage"] = "Your dispute has been filed. A PadiPay administrator will review the case and issue a binding decision.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int orderId, string message, IFormFile? attachment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(message) && (attachment == null || attachment.Length == 0))
        {
            TempData["ErrorMessage"] = "Please enter a message or attach a file.";
            return RedirectToAction(nameof(Details), new { id = orderId, tab = "discussion" });
        }

        var order = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound();
        }

        var isBuyer = order.BuyerId == userId;
        var isSeller = order.SellerId == userId;

        if (!isBuyer && !isSeller)
        {
            return Forbid();
        }

        string? attachmentUrl = null;
        string? attachmentFileName = null;

        if (attachment != null && attachment.Length > 0)
        {
            if (attachment.Length > MaxDeliverableFileSize)
            {
                TempData["ErrorMessage"] = "Attachment file size cannot exceed 20 MB.";
                return RedirectToAction(nameof(Details), new { id = orderId, tab = "discussion" });
            }

            attachmentFileName = Path.GetFileName(attachment.FileName);
            attachmentUrl = await SaveOrderMessageFileAsync(attachment);
        }

        var orderMessage = new OrderMessage
        {
            OrderId = order.Id,
            SenderId = userId,
            Message = (message ?? string.Empty).Trim(),
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = attachmentFileName,
            SentAt = DateTime.UtcNow,
            IsReadByCounterparty = false
        };

        _context.OrderMessages.Add(orderMessage);
        await _context.SaveChangesAsync();

        // Notify counterparty
        var recipientId = isBuyer ? order.SellerId : order.BuyerId;
        var senderName = isBuyer ? (order.Buyer?.FullName ?? "Buyer") : (order.Seller?.FullName ?? "Freelancer");

        await _notificationService.SendNotificationAsync(
            recipientId,
            $"New message from {senderName} 💬",
            $"{senderName}: {(orderMessage.Message.Length > 80 ? orderMessage.Message.Substring(0, 80) + "…" : orderMessage.Message)}",
            $"/Order/Details/{order.Id}#discussion",
            NotificationType.MessageReceived);

        return RedirectToAction(nameof(Details), new { id = orderId, tab = "discussion" });
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

    private async Task<string> SaveOrderMessageFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "order-messages");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return $"/uploads/order-messages/{uniqueFileName}";
    }
}

