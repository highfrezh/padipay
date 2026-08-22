using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using System.Security.Claims;

namespace PadiPay.Controllers;

[Authorize]
public class WalletController : Controller
{
    private readonly ApplicationDbContext _context;

    public WalletController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return View(wallet);
    }
}