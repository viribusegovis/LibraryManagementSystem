using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalCategories { get; set; }
        public int ActiveBorrowings { get; set; }
        public int OverdueBooks { get; set; }
        public int BooksReturnedToday { get; set; }

        public List<Borrowing> RecentBorrowings { get; set; } = new List<Borrowing>();
        public List<Borrowing> OverdueList { get; set; } = new List<Borrowing>();
        public List<PopularBookViewModel> PopularBooks { get; set; } = new List<PopularBookViewModel>();
    }

    public class PopularBookViewModel
    {
        public Book Book { get; set; } = null!;
        public int BorrowCount { get; set; }
    }
}
