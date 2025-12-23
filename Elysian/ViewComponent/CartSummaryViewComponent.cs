using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Elysian.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartSummaryViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                Cart cart = null;
                var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    // For authenticated users
                    cart = await _context.Carts
                        .Include(c => c.Items)
                        .ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                }
                else
                {
                    // For guest users
                    cart = await _context.Carts
                        .Include(c => c.Items)
                        .ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(c => c.UserId == "Guest");
                }

                return View(cart ?? new Cart { Items = new System.Collections.Generic.List<CartItem>() });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                // Log error or handle missing columns
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");

                // Return empty cart on error
                return View(new Cart { Items = new System.Collections.Generic.List<CartItem>() });
            }
        }
    }
}