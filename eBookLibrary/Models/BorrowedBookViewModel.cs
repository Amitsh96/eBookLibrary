namespace eBookLibrary.Models
{
    public class BorrowedBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime DueDate { get; set; }  // The return date of the book

        public int DaysLeft { get; set; }  // Days left to return the book
        public string CoverImage { get; set; }

        public ICollection<Review> Reviews { get; set; } // Reviews collection
    }
}
