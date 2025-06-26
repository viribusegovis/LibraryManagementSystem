// ViewModels/MemberDashboardViewModel.cs
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class MemberDashboardViewModel
    {
        public Member? Member { get; set; }
        public List<BookViewModel> Books { get; set; } = new List<BookViewModel>();
        public string CurrentTab { get; set; } = "available";
    }

    public class BookViewModel
    {
        public Book Book { get; set; } = null!;
        public Borrowing? Borrowing { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public List<BookReview> Comments { get; set; } = new List<BookReview>();
    }
}
