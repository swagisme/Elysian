namespace Elysian.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        // Navigation property
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
