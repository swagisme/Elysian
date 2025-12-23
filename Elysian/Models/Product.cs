using Elysian.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace Elysian.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public string Category { get; set; }

        public string Material { get; set; }

        public string Tags { get; set; }

        public string ImageUrl { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsNewArrival { get; set; }

        public double Rating { get; set; } = 4.0;

        // These are now in database (from migration)
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        // Not mapped properties
        [NotMapped]
        public IFormFile ImageFile { get; set; }

        [NotMapped]
        public bool RemoveImage { get; set; }
    }
}