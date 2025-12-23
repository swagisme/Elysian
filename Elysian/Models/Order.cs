using Elysian.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;



namespace Elysian.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Database columns
        public string TrackingNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string? UserId { get; set; } // Make nullable
        public virtual ApplicationUser? User { get; set; } // Make nullable
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        // These are now in database (from migration), remove [NotMapped]
        public string OrderNumber { get; set; }
        public string PaymentMethod { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; }
        public virtual ICollection<OrderHistory> History { get; set; }
    }
    public class OrderHistory
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}