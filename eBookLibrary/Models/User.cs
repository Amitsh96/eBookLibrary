namespace eBookLibrary.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public ICollection<BorrowedBook> BorrowedBooks { get; set; }
        public ICollection<PurchasedBook> PurchasedBooks { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }

}
