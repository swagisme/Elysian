using Microsoft.AspNetCore.Mvc;
using Elysian.Data;
using Elysian.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Elysian.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Message model)
        {
            if (ModelState.IsValid)
            {
                // 1. Save to Database
                _context.Messages.Add(model);
                await _context.SaveChangesAsync();

                // 2. Send Auto Email to Customer
                await SendThankYouEmail(model.Email, model.Name);

                TempData["Success"] = "Your message has been sent successfully!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        private async Task SendThankYouEmail(string toEmail, string name)
        {
            var fromAddress = new MailAddress("ayankhanx713@gmail.com", "Elysian Support");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "vepiankcbelyxwwm"; 
            string subject = "Thank you for contacting Elysian!";
            string body = $"Dear {name},\n\nThank you for contacting us. We have received your message and will get back to you shortly.\n\nBest Regards,\nElysian Team";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                await smtp.SendMailAsync(message);
            }
        }
    }
}
