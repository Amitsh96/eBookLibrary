namespace eBookLibrary.Models
{
    public class Notification
    {
        public int Id { get; set; } // Primary Key
        public int UserId { get; set; } // Foreign Key to User
        public string Message { get; set; } // Notification Message
        public DateTime CreatedAt { get; set; } // Timestamp of Notification

        public int BookId { get; set; }

        // Navigation Property
        public User User { get; set; }
    }
}
