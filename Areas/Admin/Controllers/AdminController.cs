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
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET /Admin/Admin/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var allUsers = await _userManager.Users
            .Include(u => u.Wallet)
            .ToListAsync();

        var allOrders = await _context.Orders
            .Include(o => o.Gig)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Transaction)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var allDisputes = await _context.OrderDisputes
            .Include(d => d.Order)
            .Include(d => d.Initiator)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var allGigs = await _context.Gigs.ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            // Users
            TotalUsers = allUsers.Count,
            TotalSellers = allUsers.Count(u => u.IsSeller),
            TotalBuyers = allUsers.Count(u => !u.IsSeller),

            // Gigs
            TotalGigs = allGigs.Count,
            ActiveGigs = allGigs.Count(g => g.IsActive),
            PausedGigs = allGigs.Count(g => !g.IsActive),

            // Orders
            TotalOrders = allOrders.Count,
            CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed),
            InProgressOrders = allOrders.Count(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.InRevision || o.Status == OrderStatus.Delivered),
            DisputedOrders = allOrders.Count(o => o.Status == OrderStatus.Disputed),

            // Financials
            TotalEscrowVolume = allOrders.Sum(o => o.Amount),
            TotalReleasedFunds = allOrders
                .Where(o => o.Transaction?.Status == TransactionStatus.Released)
                .Sum(o => o.Amount),
            TotalRefundedFunds = allOrders
                .Where(o => o.Transaction?.Status == TransactionStatus.Refunded)
                .Sum(o => o.Amount),

            // Disputes
            OpenDisputes = allDisputes.Count(d => d.Status == DisputeStatus.Open),
            UnderReviewDisputes = allDisputes.Count(d => d.Status == DisputeStatus.UnderReview),
            ResolvedDisputes = allDisputes.Count(d => d.Status == DisputeStatus.ResolvedRefundBuyer || d.Status == DisputeStatus.ResolvedPayoutSeller),

            // Recent Activity
            RecentOrders = allOrders.Take(8).Select(o => new RecentOrderSummary
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                BuyerName = o.Buyer?.FullName ?? "Unknown",
                SellerName = o.Seller?.FullName ?? "Unknown",
                GigTitle = o.Gig?.Title ?? "Unknown Gig",
                Amount = o.Amount,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToList(),

            RecentDisputes = allDisputes.Take(5).Select(d => new RecentDisputeSummary
            {
                Id = d.Id,
                OrderId = d.OrderId,
                OrderNumber = d.Order?.OrderNumber ?? "N/A",
                InitiatorName = d.Initiator?.FullName ?? "Unknown",
                Reason = d.Reason,
                OrderAmount = d.Order?.Amount ?? 0m,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt
            }).ToList()
        };

        return View(vm);
    }

    // GET /Admin/Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .Include(u => u.Wallet)
            .Include(u => u.Gigs)
            .OrderByDescending(u => u.JoinedAt)
            .ToListAsync();

        var orders = await _context.Orders.ToListAsync();
        var disputes = await _context.OrderDisputes.ToListAsync();

        var userList = new List<AdminUserItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new AdminUserItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                SecurityStamp = user.SecurityStamp,
                Location = user.Location,
                ProfilePictureUrl = user.ProfilePictureUrl,
                JoinedAt = user.JoinedAt,
                IsSeller = user.IsSeller,
                WalletBalance = user.Wallet?.Balance ?? 0m,
                Roles = roles,
                AdminNotes = user.AdminNotes,
                GigCount = user.Gigs?.Count ?? 0,
                OrdersAsBuyerCount = orders.Count(o => o.BuyerId == user.Id),
                OrdersAsSellerCount = orders.Count(o => o.SellerId == user.Id),
                DisputesInvolvedCount = disputes.Count(d => d.InitiatorId == user.Id || (d.Order != null && (d.Order.BuyerId == user.Id || d.Order.SellerId == user.Id)))
            });
        }

        return View(userList);
    }

    // POST /Admin/Admin/ToggleLockout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockout(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var currentAdminId = _userManager.GetUserId(User);
        if (currentAdminId == userId)
        {
            TempData["ErrorMessage"] = "Security violation: You cannot lock out your own active administrative account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            // Unlock
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["SuccessMessage"] = $"Account for {user.FullName} ({user.Email}) has been successfully UNLOCKED.";
        }
        else
        {
            // Lockout/Suspend
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["SuccessMessage"] = $"Account for {user.FullName} ({user.Email}) has been SUSPENDED / LOCKED.";
        }

        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/ToggleAdminRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminRole(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var currentAdminId = _userManager.GetUserId(User);
        if (currentAdminId == userId)
        {
            TempData["ErrorMessage"] = "Security restriction: You cannot revoke your own administrator role.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["SuccessMessage"] = $"Admin privileges revoked for {user.FullName}.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["SuccessMessage"] = $"{user.FullName} has been promoted to Administrator.";
        }

        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/ToggleEmailConfirmed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmailConfirmed(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.EmailConfirmed = !user.EmailConfirmed;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = $"Email confirmation for {user.FullName} marked as {(user.EmailConfirmed ? "Verified" : "Unverified")}.";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/ToggleSellerStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSellerStatus(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.IsSeller = !user.IsSeller;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = $"User {user.FullName} type switched to {(user.IsSeller ? "Seller" : "Buyer")}.";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/AdjustWallet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustWallet(WalletAdjustmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid wallet adjustment request parameters.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null) return NotFound();

        if (user.Wallet == null)
        {
            user.Wallet = new Wallet { UserId = user.Id, Balance = 0m };
            _context.Wallets.Add(user.Wallet);
        }

        var adminEmail = User.Identity?.Name ?? "Admin";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC");

        if (request.AdjustmentType == "Debit")
        {
            if (user.Wallet.Balance < request.Amount)
            {
                TempData["ErrorMessage"] = $"Debit failed: User only has ₦{user.Wallet.Balance:N2} in wallet, which is less than ₦{request.Amount:N2}.";
                return RedirectToAction(nameof(Users));
            }

            user.Wallet.Balance -= request.Amount;
            var note = $"[{timestamp}] ⚠️ Wallet DEBIT of ₦{request.Amount:N2} executed by {adminEmail}. Reason: {request.Reason}. New Balance: ₦{user.Wallet.Balance:N2}";
            user.AdminNotes = string.IsNullOrWhiteSpace(user.AdminNotes) ? note : user.AdminNotes + "\n" + note;
            TempData["SuccessMessage"] = $"Successfully debited ₦{request.Amount:N2} from {user.FullName}'s wallet.";
        }
        else
        {
            user.Wallet.Balance += request.Amount;
            var note = $"[{timestamp}] 💰 Wallet CREDIT of ₦{request.Amount:N2} executed by {adminEmail}. Reason: {request.Reason}. New Balance: ₦{user.Wallet.Balance:N2}";
            user.AdminNotes = string.IsNullOrWhiteSpace(user.AdminNotes) ? note : user.AdminNotes + "\n" + note;
            TempData["SuccessMessage"] = $"Successfully credited ₦{request.Amount:N2} to {user.FullName}'s wallet.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/SaveAdminNotes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdminNotes(SaveAdminNotesRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId)) return BadRequest();
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound();

        user.AdminNotes = request.AdminNotes;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = $"Confidential notes updated for {user.FullName}.";
        return RedirectToAction(nameof(Users));
    }

    // POST /Admin/Admin/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Failed to reset password: {errors}";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            
            var adminEmail = User.Identity?.Name ?? "Admin";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC");
            var note = $"[{timestamp}] 🔑 Password was reset by Admin {adminEmail}.";
            user.AdminNotes = string.IsNullOrWhiteSpace(user.AdminNotes) ? note : user.AdminNotes + "\n" + note;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Password for {user.FullName} ({user.Email}) has been successfully updated.";
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"Error resetting password: {errors}";
        }

        return RedirectToAction(nameof(Users));
    }



    // GET /Admin/Admin/Gigs
    public async Task<IActionResult> Gigs()
    {
        var gigs = await _context.Gigs
            .Include(g => g.Seller)
            .Include(g => g.Category)
            .Include(g => g.Reviews)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return View(gigs);
    }

    // POST /Admin/Admin/ToggleGig
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleGig(int id)
    {
        var gig = await _context.Gigs.FindAsync(id);
        if (gig == null) return NotFound();

        gig.IsActive = !gig.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Gig \"{gig.Title.Substring(0, Math.Min(40, gig.Title.Length))}...\" has been {(gig.IsActive ? "activated" : "paused")}.";
        return RedirectToAction(nameof(Gigs));
    }
}
