using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.Services;
using System.Security.Claims;

namespace PadiPay.Controllers;

[Authorize]
public class WalletController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public WalletController(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }

    [HttpGet]
    public IActionResult Fund()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Fund(decimal amount)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0m
            };
            _context.Wallets.Add(wallet);
        }

        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Please specify a valid funding amount.";
            return RedirectToAction(nameof(Index));
        }

        wallet.Balance += amount;
        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            userId,
            "Wallet Credited 💳",
            $"Your wallet has been credited with ₦{amount:N0}. Current balance: ₦{wallet.Balance:N0}.",
            "/Wallet",
            NotificationType.WalletFunded);

        TempData["SuccessMessage"] = $"Wallet successfully credited with ₦{amount:N0}!";
        return RedirectToAction("Index");
    }
}