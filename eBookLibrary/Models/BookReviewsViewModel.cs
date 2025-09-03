namespace eBookLibrary.Models
{
    public class BookReviewsViewModel
    {
        public Book Book { get; set; }
        public IEnumerable<Review> Reviews { get; set; }
    }
}
