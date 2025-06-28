using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;

        public HomeController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string tab = "available")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            if (User.IsInRole("Bibliotecário"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Perfil de membro não encontrado.";
                return RedirectToAction("AccessDenied", "Account");
            }

            // FIXED: Load borrowings with ALL book reviews (not just from current member)
            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews) // This loads ALL reviews for each book
                        .ThenInclude(br => br.Member) // Include member info for each review
                .Include(b => b.Member)
                .Where(b => b.MemberId == member.MemberId) // Only current member's borrowings
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            // Assign the loaded borrowings to the member
            member.Borrowings = borrowings;

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentTab = tab,
                Books = await GetAvailableBooks()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetTabContent(string tab)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Utilizador não encontrado" });
            }

            // Step 1: Get member
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return Json(new { success = false, message = "Membro não encontrado" });
            }

            // Step 2: Get filtered borrowings with complete book data
            var borrowingsQuery = _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews) // Include ALL reviews
                        .ThenInclude(br => br.Member)
                .Include(b => b.Member)
                .Where(b => b.MemberId == member.MemberId);

            // Apply tab-specific filtering
            switch (tab)
            {
                case "history":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Devolvido");
                    break;
                case "borrowed":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Emprestado");
                    break;
                case "overdue":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now);
                    break;
            }

            var borrowings = await borrowingsQuery
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            // Assign filtered borrowings to member
            member.Borrowings = borrowings;

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentTab = tab
            };

            switch (tab)
            {
                case "borrowed":
                    return PartialView("_BorrowedTab", viewModel);
                case "overdue":
                    return PartialView("_OverdueTab", viewModel);
                case "history":
                    return PartialView("_HistoryTab", viewModel);
                default:
                    return PartialView("_BorrowedTab", viewModel);
            }
        }
        

        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> Catalog(string search)
        {
            var booksQuery = _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews) // Add this line
                    .ThenInclude(br => br.Member) // Add this line
                .Include(b => b.Borrowings) // Add this line
                .Where(b => b.Available);

            if (!string.IsNullOrEmpty(search))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(search) ||
                    b.Author.Contains(search) ||
                    b.Categories.Any(c => c.Name.Contains(search)));
            }

            var books = await booksQuery.ToListAsync();

            var bookViewModels = books.Select(b => new BookViewModel
            {
                Book = b,
                LikesCount = b.BookReviews?.Count(r => r.IsLike) ?? 0,
                DislikesCount = b.BookReviews?.Count(r => !r.IsLike) ?? 0,
                Comments = b.BookReviews?.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList() ?? new List<BookReview>()
            }).ToList();

            ViewData["CurrentFilter"] = search;
            return View(bookViewModels);
        }


        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> MyHistory()
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Index");
            }

            var history = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return View(history);
        }

        public IActionResult About()
        {
            return View();
        }

        private async Task<List<BookViewModel>> GetAvailableBooks()
        {
            var books = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .Where(b => b.Available)
                .ToListAsync();

            return books.Select(b => new BookViewModel
            {
                Book = b,
                LikesCount = b.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.BookReviews.Count(r => !r.IsLike),
                Comments = b.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        private async Task<List<BookViewModel>> GetBorrowedBooks(Guid? memberId)
        {
            if (!memberId.HasValue) return new List<BookViewModel>();

            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews)
                        .ThenInclude(br => br.Member)
                .Where(b => b.MemberId == memberId && b.Status == "Emprestado")
                .ToListAsync();

            return borrowings.Select(b => new BookViewModel
            {
                Book = b.Book,
                Borrowing = b,
                LikesCount = b.Book.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.Book.BookReviews.Count(r => !r.IsLike),
                Comments = b.Book.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        private async Task<List<BookViewModel>> GetOverdueBooks(Guid? memberId)
        {
            if (!memberId.HasValue) return new List<BookViewModel>();

            var overdueBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews)
                        .ThenInclude(br => br.Member)
                .Where(b => b.MemberId == memberId &&
                           b.Status == "Emprestado" &&
                           b.DueDate < DateTime.Now)
                .ToListAsync();

            return overdueBorrowings.Select(b => new BookViewModel
            {
                Book = b.Book,
                Borrowing = b,
                LikesCount = b.Book.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.Book.BookReviews.Count(r => !r.IsLike),
                Comments = b.Book.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateBook(Guid bookId, bool isLike, string comment = "")
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return Json(new { success = false, message = "Membro não encontrado." });
            }

            var hasBorrowedBook = await _context.Borrowings
                .AnyAsync(b => b.MemberId == member.MemberId && b.BookId == bookId);

            if (!hasBorrowedBook)
            {
                return Json(new
                {
                    success = false,
                    message = "Só pode avaliar livros que já requisitou ou devolveu."
                });
            }

            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(br => br.BookId == bookId && br.MemberId == member.MemberId);

            if (existingReview != null)
            {
                existingReview.IsLike = isLike;
                existingReview.Comment = comment;
                existingReview.ReviewDate = DateTime.Now;
            }
            else
            {
                var review = new BookReview
                {
                    BookId = bookId,
                    MemberId = member.MemberId,
                    IsLike = isLike,
                    Comment = comment,
                    ReviewDate = DateTime.Now
                };
                _context.BookReviews.Add(review);
            }

            await _context.SaveChangesAsync();

            var likesCount = await _context.BookReviews.CountAsync(r => r.BookId == bookId && r.IsLike);
            var dislikesCount = await _context.BookReviews.CountAsync(r => r.BookId == bookId && !r.IsLike);

            return Json(new
            {
                success = true,
                message = "Avaliação registada com sucesso!",
                likesCount = likesCount,
                dislikesCount = dislikesCount
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
