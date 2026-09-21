using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;

namespace PadiPay.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var recentGigs = await _context.Gigs
            .Include(g => g.Category)
            .Include(g => g.Seller)
            .Include(g => g.Reviews)
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.RecentGigs = recentGigs;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
