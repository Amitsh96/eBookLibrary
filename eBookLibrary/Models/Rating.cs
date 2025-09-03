namespace eBookLibrary.Models
{
    public class Rating
    {
        public int Id { get; set; } // Primary Key
        public int BookId { get; set; } // Foreign Key to Book
        public int UserId { get; set; } // Foreign Key to User
        public int RatingValue { get; set; } // Rating (e.g., 1 to 5)
        public string Feedback { get; set; } // Optional feedback
        public DateTime CreatedAt { get; set; } // Timestamp for rating/feedback

        // Navigation Properties
        public Book Book { get; set; }
        public User User { get; set; }
    }
}
