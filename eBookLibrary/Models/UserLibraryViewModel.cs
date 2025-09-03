namespace eBookLibrary.Models
{
    public class UserLibraryViewModel
    {
        public List<BorrowedBookViewModel> BorrowedBooks { get; set; } = new List<BorrowedBookViewModel>();
        public List<PurchasedBookViewModel> PurchasedBooks { get; set; } = new List<PurchasedBookViewModel>();

        public List<WaitingBookViewModel> WaitingList { get; set; }
        public List<string> Notifications { get; set; } = new List<string>();
    }


}
