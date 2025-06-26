using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Bibliotecário")]
    public class MembersController : Controller
    {
        private readonly LibraryContext _context;

        public MembersController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Members
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var members = from m in _context.Members
                         .Include(m => m.Borrowings)
                          select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                members = members.Where(m => m.Name.Contains(searchString)
                                          || m.Email.Contains(searchString)
                                          || m.CardNumber.Contains(searchString));
            }

            return View(await members.ToListAsync());
        }

        // GET: Members/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.Categories)
                .Include(m => m.BookReviews)
                    .ThenInclude(br => br.Book)
                        .ThenInclude(b => b.Categories)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        // Additional CRUD methods
    }
}
