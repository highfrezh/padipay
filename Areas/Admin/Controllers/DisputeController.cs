using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.ViewModels;

namespace PadiPay.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DisputeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DisputeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET /Admin/Dispute/Index
    public async Task<IActionResult> Index(string filter = "open")
    {
        ViewBag.ActiveFilter = filter.ToLowerInvariant();

        var query = _context.OrderDisputes
            .Include(d => d.Order)
                .ThenInclude(o => o!.Gig)
            .Include(d => d.Initiator)
            .Include(d => d.Order)
                .ThenInclude(o => o!.Buyer)
            .Include(d => d.Order)
                .ThenInclude(o => o!.Seller)
            .AsQueryable();

        query = filter switch
        {
            "resolved" => query.Where(d => d.Status == DisputeStatus.ResolvedRefundBuyer || d.Status == DisputeStatus.ResolvedPayoutSeller || d.Status == DisputeStatus.Cancelled),
            "review" => query.Where(d => d.Status == DisputeStatus.UnderReview),
            _ => query.Where(d => d.Status == DisputeStatus.Open)
        };

        var disputes = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        ViewBag.OpenCount = await _context.OrderDisputes.CountAsync(d => d.Status == DisputeStatus.Open);
        ViewBag.ReviewCount = await _context.OrderDisputes.CountAsync(d => d.Status == DisputeStatus.UnderReview);
        ViewBag.ResolvedCount = await _context.OrderDisputes.CountAsync(d => d.Status == DisputeStatus.ResolvedRefundBuyer || d.Status == DisputeStatus.ResolvedPayoutSeller);

        return View(disputes);
    }

    // GET /Admin/Dispute/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var dispute = await _context.OrderDisputes
            .Include(d => d.Order)
                .ThenInclude(o => o!.Gig)
                    .ThenInclude(g => g!.Category)
            .Include(d => d.Order)
                .ThenInclude(o => o!.Deliveries.OrderByDescending(del => del.DeliveredAt))
            .Include(d => d.Order)
                .ThenInclude(o => o!.Transaction)
            .Include(d => d.Initiator)
            .Include(d => d.ResolvedBy)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dispute == null) return NotFound();

        // Mark as UnderReview if still Open
        if (dispute.Status == DisputeStatus.Open)
        {
            dispute.Status = DisputeStatus.UnderReview;
            await _context.SaveChangesAsync();
        }

        var buyer = await _userManager.FindByIdAsync(dispute.Order!.BuyerId);
        var seller = await _userManager.FindByIdAsync(dispute.Order.SellerId);

        var vm = new AdminDisputeDetailViewModel
        {
            Dispute = dispute,
            Order = dispute.Order!,
            Buyer = buyer!,
            Seller = seller!
        };

        return View(vm);
    }

    // POST /Admin/Dispute/Resolve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int disputeId, string decision, string adminNotes)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var dispute = await _context.OrderDisputes
            .Include(d => d.Order)
                .ThenInclude(o => o!.Transaction)
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null || dispute.Order == null)
        {
            TempData["ErrorMessage"] = "Dispute not found.";
            return RedirectToAction(nameof(Index));
        }

        if (dispute.Status == DisputeStatus.ResolvedRefundBuyer || dispute.Status == DisputeStatus.ResolvedPayoutSeller)
        {
            TempData["ErrorMessage"] = "This dispute has already been resolved.";
            return RedirectToAction(nameof(Details), new { id = disputeId });
        }

        var order = dispute.Order;

        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (decision == "RefundBuyer")
            {
                // Credit buyer wallet
                var buyerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == order.BuyerId);
                if (buyerWallet == null)
                {
                    buyerWallet = new Wallet { UserId = order.BuyerId, Balance = 0m };
                    _context.Wallets.Add(buyerWallet);
                }
                buyerWallet.Balance += order.Amount;

                // Update transaction
                if (order.Transaction != null)
                    order.Transaction.Status = TransactionStatus.Refunded;

                // Update order
                order.Status = OrderStatus.Cancelled;

                // Resolve dispute
                dispute.Status = DisputeStatus.ResolvedRefundBuyer;
            }
            else if (decision == "PayoutSeller")
            {
                // Credit seller wallet
                var sellerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == order.SellerId);
                if (sellerWallet == null)
                {
                    sellerWallet = new Wallet { UserId = order.SellerId, Balance = 0m };
                    _context.Wallets.Add(sellerWallet);
                }
                sellerWallet.Balance += order.Amount;

                // Update transaction
                if (order.Transaction != null)
                    order.Transaction.Status = TransactionStatus.Released;

                // Update order
                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTime.UtcNow;

                // Resolve dispute
                dispute.Status = DisputeStatus.ResolvedPayoutSeller;
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid resolution decision.";
                return RedirectToAction(nameof(Details), new { id = disputeId });
            }

            dispute.AdminNotes = adminNotes?.Trim();
            dispute.ResolvedById = adminId;
            dispute.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await dbTx.CommitAsync();

            var outcomeText = decision == "RefundBuyer" ? "Buyer refunded" : "Seller paid out";
            TempData["SuccessMessage"] = $"Dispute resolved: {outcomeText}. ₦{order.Amount:N0} has been transferred accordingly.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await dbTx.RollbackAsync();
            TempData["ErrorMessage"] = "An error occurred while resolving the dispute. Please try again.";
            return RedirectToAction(nameof(Details), new { id = disputeId });
        }
    }
}
