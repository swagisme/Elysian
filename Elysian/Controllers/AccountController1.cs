using Elysian.Data;
using Elysian.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Elysian.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 ApplicationDbContext context,
                                 IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _environment = environment;
        }

        // ======================
        // 🔹 Index / Dashboard
        // ======================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var recentOrders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;
            return View(user);
        }

        // ======================
        // 🔹 Edit Profile GET
        // ======================
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                ExistingProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        // ======================
        // 🔹 Edit Profile POST
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                user.ProfilePicture = $"/images/profiles/{uniqueFileName}";
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;
                user.EmailConfirmed = false;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ======================
        // 🔹 Change Password
        // ======================
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in changePasswordResult.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ======================
        // 🔹 Register
        // ======================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ======================
        // 🔹 Login GET - disable cache + force logout
        // ======================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                Response.Cookies.Delete(".AspNetCore.Identity.Application");
            }

            return View();
        }

        // ======================
        // 🔹 Login POST
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Guest cart merge
                    var userId = user.Id;
                    var guestCart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.UserId == null);

                    if (guestCart != null)
                    {
                        var userCart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.UserId == userId);
                        if (userCart == null)
                        {
                            userCart = new Cart { UserId = userId, Items = new List<CartItem>() };
                            _context.Carts.Add(userCart);
                        }

                        foreach (var item in guestCart.Items)
                        {
                            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                            if (existing != null)
                                existing.Quantity += item.Quantity;
                            else
                                userCart.Items.Add(new CartItem { ProductId = item.ProductId, Quantity = item.Quantity });
                        }

                        _context.Carts.Remove(guestCart);
                        await _context.SaveChangesAsync();
                    }

                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Index", "Admin");
                    else
                        return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // ======================
        // 🔹 Logout
        // ======================
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Identity.Application"); // force remove identity cookie
            return RedirectToAction("Login");
        }

        // ======================
        // 🔹 Access Denied
        // ======================
        public IActionResult AccessDenied() => View();
    }
}
