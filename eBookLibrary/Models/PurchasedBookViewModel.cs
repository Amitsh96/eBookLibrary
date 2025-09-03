namespace eBookLibrary.Models
{
    public class PurchasedBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime PurchaseDate { get; set; }

        public string CoverImage { get; set; }
        public ICollection<Review> Reviews { get; set; }  // Add reviews here
    }
}
