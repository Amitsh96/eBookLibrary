namespace eBookLibrary.Models
{
    public class WaitingBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string CoverImage { get; set; }
        public int EstimatedDays { get; set; }
        public int Position { get; set; }
        public int WaitingCount { get; set; }
    }
}
