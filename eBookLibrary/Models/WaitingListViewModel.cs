namespace eBookLibrary.Models
{

    public class WaitingListViewModel
    {
        public int BookId { get; set; }  // Add this line
        public string UserEmail { get; set; }
        public int Position { get; set; }
        public bool IsNotified { get; set; }
    }
}