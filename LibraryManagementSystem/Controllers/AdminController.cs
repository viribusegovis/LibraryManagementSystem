using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Bibliotecário")]
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = new AdminDashboardViewModel
            {
                TotalBooks = await _context.Books.CountAsync(),
                AvailableBooks = await _context.Books.CountAsync(b => b.Available),
                TotalMembers = await _context.Members.CountAsync(),
                ActiveMembers = await _context.Members.CountAsync(m => m.IsActive),
                TotalCategories = await _context.Categories.CountAsync(),
                ActiveBorrowings = await _context.Borrowings
                    .CountAsync(b => b.Status == "Emprestado"),
                OverdueBooks = await _context.Borrowings
                    .CountAsync(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now),
                BooksReturnedToday = await _context.Borrowings
                    .CountAsync(b => b.ReturnDate.HasValue && b.ReturnDate.Value.Date == DateTime.Today),
                RecentBorrowings = await _context.Borrowings
                    .Include(b => b.Book)
                    .Include(b => b.Member)
                    .OrderByDescending(b => b.BorrowDate)
                    .Take(5)
                    .ToListAsync(),
                OverdueList = await _context.Borrowings
                    .Include(b => b.Book)
                    .Include(b => b.Member)
                    .Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now)
                    .OrderBy(b => b.DueDate)
                    .Take(10)
                    .ToListAsync(),
                PopularBooks = await _context.Borrowings
                    .Include(b => b.Book)
                    .GroupBy(b => b.Book)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new PopularBookViewModel
                    {
                        Book = g.Key,
                        BorrowCount = g.Count()
                    })
                    .ToListAsync()
            };

            return View(dashboardData);
        }
    }
}
