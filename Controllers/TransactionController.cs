using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;

namespace PadiPay.Controllers;

[Authorize]
public class TransactionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TransactionController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string sellerEmail, decimal amount)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var seller = await _userManager.FindByEmailAsync(sellerEmail);
        if (seller == null)
        {
            ModelState.AddModelError(string.Empty, "Seller not found.");
            return View();
        }

        if (seller.Id == buyerId)
        {
            ModelState.AddModelError(string.Empty, "You can't create a transaction with yourself.");
            return View();
        }

        var buyerWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == buyerId);

        if (buyerWallet == null || buyerWallet.Balance < amount)
        {
            ModelState.AddModelError(string.Empty, "Insufficient wallet balance.");
            return View();
        }

        buyerWallet.Balance -= amount;

        var transaction = new Transaction
        {
            BuyerId = buyerId,
            SellerId = seller.Id,
            Amount = amount,
            Status = TransactionStatus.Funded
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var transactions = await _context.Transactions
            .Where(t => t.BuyerId == userId || t.SellerId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(transactions);
    }

    [HttpPost]
    public async Task<IActionResult> MarkDelivered(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        if (transaction.SellerId != userId)
        {
            return Forbid();
        }

        if (transaction.Status != TransactionStatus.Funded)
        {
            ModelState.AddModelError(string.Empty, "Transaction cannot be marked delivered from its current status.");
            return RedirectToAction("Index");
        }

        transaction.Status = TransactionStatus.Delivered;
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Release(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        if (transaction.BuyerId != userId)
        {
            return Forbid();
        }

        if (transaction.Status != TransactionStatus.Delivered)
        {
            ModelState.AddModelError(string.Empty, "Transaction must be delivered before funds can be released.");
            return RedirectToAction("Index");
        }

        var sellerWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == transaction.SellerId);

        if (sellerWallet == null)
        {
            return NotFound();
        }

        sellerWallet.Balance += transaction.Amount;
        transaction.Status = TransactionStatus.Released;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}