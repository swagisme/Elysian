using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class ShopController : Controller
{
    private readonly ApplicationDbContext _context;

    public ShopController(ApplicationDbContext context)
    {
        _context = context;
    }

    // All Products Page
    public IActionResult Index(string search, string category, int? minPrice, int? maxPrice)
    {
        var products = _context.Products.AsQueryable();

        // 🔎 Search by name
        if (!string.IsNullOrEmpty(search))
        {
            products = products.Where(p => p.Name.Contains(search));
        }

        // 🏷️ Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            products = products.Where(p => p.Category == category);
        }

        // 💰 Price filter
        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }

        // 📊 ViewBag for Categories with count
        ViewBag.Categories = _context.Products
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToList();

        // 📊 ViewBag for Min & Max Price (for slider)
        ViewBag.MinPrice = _context.Products.Any() ? _context.Products.Min(p => p.Price) : 0;
        ViewBag.MaxPrice = _context.Products.Any() ? _context.Products.Max(p => p.Price) : 0;

        return View(products.ToList());
    }

    // Product Details Page
    public IActionResult Details(int id)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }
}
