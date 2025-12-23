using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elysian.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 🔹 Dashboard
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(stats);
        }

        // 🔹 Products List
        [HttpGet("Products")]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        // 🔹 Create Product (GET)
        [HttpGet("Products/Create")]
        public IActionResult CreateProduct()
        {
            return View();
        }

        // 🔹 Create Product (POST)
        [HttpPost("Products/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                // Handle image upload
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    await SaveProductImage(product, product.ImageFile);
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("Products");
            }

            return View(product);
        }

        // 🔹 Edit Product (GET)
        [HttpGet("Products/Edit/{id}")]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // 🔹 Edit Product (POST)
        [HttpPost("Products/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // Update properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Category = product.Category;
                existingProduct.Material = product.Material;
                existingProduct.Tags = product.Tags;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsNewArrival = product.IsNewArrival;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Handle image upload
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    await SaveProductImage(existingProduct, product.ImageFile);
                }

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("Products");
            }

            return View(product);
        }

        // 🔹 Delete Product
        [HttpPost("Products/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("Products");
        }

        // 🔹 Orders List
        [HttpGet("Orders")]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet("Orders/Details/{id}")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.History)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // 🔹 Update Order Status - FIXED VERSION
        [HttpPost("Orders/UpdateStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status, string notes = "")
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Update order status
            order.Status = status;

            // ❌ REMOVE THIS - Order model doesn't have UpdatedAt (it's NotMapped)
            // order.UpdatedAt = DateTime.UtcNow;

            // Create OrderHistory entry if OrderHistories table exists
            try
            {
                if (_context.OrderHistories != null)
                {
                    var history = new OrderHistory
                    {
                        OrderId = id,
                        Status = status,
                        Notes = notes,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = User.Identity?.Name ?? "Admin"
                    };
                    _context.OrderHistories.Add(history);
                }
            }
            catch
            {
                // If OrderHistories table doesn't exist yet, just skip
            }

            _context.Update(order);
            await _context.SaveChangesAsync();

            // ✅ Use TrackingNumber instead of OrderNumber (which doesn't exist)
            TempData["SuccessMessage"] = $"Order #{order.TrackingNumber} status updated to {status}";
            return RedirectToAction("OrderDetails", new { id });
        }


        [HttpGet("Customers")]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            return View(customers);
        }


        // 🔹 Delete Customer
        [HttpPost("Customers/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Check if user has orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Cannot delete customer with existing orders.";
                return RedirectToAction("Customers");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Customer deleted successfully!";
            return RedirectToAction("Customers");
        }

        // Private helper method for image upload
        private async Task SaveProductImage(Product product, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                product.ImageUrl = $"/uploads/products/{uniqueFileName}";
            }
        }



        [HttpPost("Products/BulkUpdate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdate(List<Product> products, List<IFormFile> ProductImages)
        {
            for (int i = 0; i < products.Count; i++)
            {
                var dbProduct = await _context.Products.FindAsync(products[i].Id);
                if (dbProduct == null) continue;

                dbProduct.Name = products[i].Name;
                dbProduct.Description = products[i].Description;
                dbProduct.Price = products[i].Price;
                dbProduct.Category = products[i].Category;
                dbProduct.Material = products[i].Material;
                dbProduct.Tags = products[i].Tags;
                dbProduct.IsFeatured = products[i].IsFeatured;
                dbProduct.IsNewArrival = products[i].IsNewArrival;
                dbProduct.UpdatedAt = DateTime.UtcNow;

                // Image update
                if (ProductImages != null && ProductImages.Count > i && ProductImages[i] != null)
                {
                    await SaveProductImage(dbProduct, ProductImages[i]);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Products updated successfully!";
            return RedirectToAction("Products");
        }

    }
}