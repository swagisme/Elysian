using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 🛒 Get current cart (by logged-in user OR guest)
    private Cart GetCart()
    {
        string userId = _userManager.GetUserId(User);

        if (!string.IsNullOrEmpty(userId)) // ✅ Logged-in User
        {
            var cart = _context.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, Items = new List<CartItem>() };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }
            return cart;
        }

        // ✅ Guest User → assign a "GuestCart"
        var guestCart = _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(c => c.UserId == "Guest");

        if (guestCart == null)
        {
            guestCart = new Cart { UserId = "Guest", Items = new List<CartItem>() };
            _context.Carts.Add(guestCart);
            _context.SaveChanges();
        }

        return guestCart;
    }

    // 🛍 Show Cart Page
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    // ➕ Add to Cart
    public IActionResult AddToCart(int productId)
    {
        var product = _context.Products.Find(productId);
        if (product == null) return NotFound();

        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null)
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = 1, CartId = cart.Id });
        else
            item.Quantity++;

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    // Update Quantity
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int itemId, int change)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                (i.Cart.UserId == userId || i.Cart.UserId == "Guest")
            );

        if (item != null)
        {
            item.Quantity += change;

            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // ❌ Remove item
    [HttpPost]
    public IActionResult RemoveFromCart(int itemId)
    {
        var item = _context.CartItems.Find(itemId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    // 🚀 Redirect to Checkout (in OrderController)
    public IActionResult ProceedToCheckout()
    {
        return RedirectToAction("Checkout", "Order");
    }
}