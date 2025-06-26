using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class HomeController : BaseController
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
            var member = await _context.Members
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.Categories)
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.BookReviews)
                            .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

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
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.Categories)
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.BookReviews)
                            .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            List<BookViewModel> books;

            switch (tab)
            {
                case "borrowed":
                    books = await GetBorrowedBooks(member?.MemberId);
                    break;
                case "overdue":
                    books = await GetOverdueBooks(member?.MemberId);
                    break;
                default:
                    books = await GetAvailableBooks();
                    break;
            }

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentTab = tab,
                Books = books
            };

            return PartialView("_TabContent", viewModel);
        }

        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> Catalog(string search = "")
        {
            var booksQuery = _context.Books
                .Include(b => b.Categories) // FIXED: Use Categories collection
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .Where(b => b.Available);

            if (!string.IsNullOrEmpty(search))
            {
                // FIXED: Search in Categories collection
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(search) ||
                    b.Author.Contains(search) ||
                    b.Categories.Any(c => c.Name.Contains(search)));
                ViewData["CurrentFilter"] = search;
            }

            var books = await booksQuery.ToListAsync();

            var bookViewModels = books.Select(b => new BookViewModel
            {
                Book = b,
                LikesCount = b.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.BookReviews.Count(r => !r.IsLike),
                Comments = b.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();

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
