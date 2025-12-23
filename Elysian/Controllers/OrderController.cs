using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 🛒 Get current cart (supports both logged-in and guest users)
    private Cart GetCart()
    {
        string userId = _userManager.GetUserId(User);

        if (!string.IsNullOrEmpty(userId)) // ✅ Logged-in User
        {
            var cart = _context.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefault(c => c.UserId == userId);

            return cart ?? new Cart { UserId = userId, Items = new List<CartItem>() };
        }

        // ✅ Guest User
        var guestCart = _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(c => c.UserId == "Guest");

        return guestCart ?? new Cart { UserId = "Guest", Items = new List<CartItem>() };
    }

    // GET: Checkout Page
    [HttpGet]
    public IActionResult Checkout()
    {
        var cart = GetCart();

        if (cart == null || !cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var model = new CheckoutViewModel
        {
            Cart = cart
        };

        // Pre-populate for logged-in users
        if (User.Identity.IsAuthenticated)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user != null)
            {
                model.Email = user.Email ?? "";
            }
        }

        // ✅ EXPLICITLY specify the view location
        return View("~/Views/Cart/Checkout.cshtml", model);
    }

    // POST: Place Order
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        Console.WriteLine("=== CHECKOUT POST STARTED ===");

        var cart = GetCart();

        if (cart == null || !cart.Items.Any())
        {
            Console.WriteLine("Cart is empty or null");
            TempData["Error"] = "Your cart is empty";
            return RedirectToAction("Index", "Cart");
        }

        // Debug incoming form data
        Console.WriteLine("=== INCOMING FORM DATA ===");
        Console.WriteLine($"FullName: '{model.FullName ?? "NULL"}'");
        Console.WriteLine($"Email: '{model.Email ?? "NULL"}'");
        Console.WriteLine($"Phone: '{model.Phone ?? "NULL"}'");
        Console.WriteLine($"Address: '{model.Address ?? "NULL"}'");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("=== MODEL STATE ERRORS ===");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Error: {error.ErrorMessage}");
            }
            model.Cart = cart;
            // ✅ EXPLICITLY specify the view location
            return View("~/Views/Cart/Checkout.cshtml", model);
        }

        try
        {
            // ✅ Get user info (supports both logged-in and guest users)
            var user = User.Identity.IsAuthenticated ? _userManager.GetUserAsync(User).Result : null;
            var userId = user?.Id ?? "Guest";

            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine($"Is Guest: {userId == "Guest"}");

            // ✅ Create order with ALL form data
            var order = new Order
            {
                UserId = userId,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                TrackingNumber = "ORD" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = cart.Items.Sum(i => i.Product.Price * i.Quantity),
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };

            Console.WriteLine("=== ORDER BEING SAVED ===");
            Console.WriteLine($"FullName: '{order.FullName}'");
            Console.WriteLine($"Email: '{order.Email}'");
            Console.WriteLine($"Phone: '{order.Phone}'");
            Console.WriteLine($"Address: '{order.Address}'");
            Console.WriteLine($"TotalAmount: {order.TotalAmount}");
            Console.WriteLine($"Items Count: {order.Items.Count}");

            // ✅ Save order and clear cart
            _context.Orders.Add(order);

            // Remove cart items (not the entire cart) so guest cart can be reused
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();
            Console.WriteLine("✅ ORDER SAVED SUCCESSFULLY");

            return RedirectToAction("OrderConfirmed", new { trackingNumber = order.TrackingNumber });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            ModelState.AddModelError("", "Something went wrong while placing your order.");
            model.Cart = cart;
          
            return View("~/Views/Cart/Checkout.cshtml", model);
        }
    }

    // Order Confirmed Page
    public IActionResult OrderConfirmed(string trackingNumber)
    {
        var order = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(o => o.TrackingNumber == trackingNumber);

        if (order == null)
        {
            return NotFound();
        }

        return View("~/Views/Cart/OrderConfirmed.cshtml", order);
    }



  

    // Order History (for logged-in users only)
    public async Task<IActionResult> OrderHistory()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId) || userId == "Guest")
        {
            TempData["Error"] = "Please log in to view your order history";
            return RedirectToAction("Login", "Account");
        }

        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View("~/Views/Cart/OrderHistory.cshtml", orders);
    }

    // Order Details (for individual order view)
    public async Task<IActionResult> OrderDetails(int id)
    {
        var userId = _userManager.GetUserId(User);

        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View("~/Views/Cart/OrderDetails.cshtml", order);
    }

}