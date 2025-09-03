using eBookLibrary.Models;

public class Review
{
    public int ReviewId { get; set; }
    public int BookId { get; set; }
    public int UserId { get; set; }
    public string? Content { get; set; }  // Mark as nullable
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual Book? Book { get; set; }  // Mark as nullable
    public virtual User? User { get; set; }  // Mark as nullable
}