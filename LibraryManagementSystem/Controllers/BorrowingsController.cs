using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Bibliotecário")]
    public class BorrowingsController : Controller
    {
        private readonly LibraryContext _context;

        public BorrowingsController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Borrowings - FIXED to use Categories collection
        public async Task<IActionResult> Index(string status = "all")
        {
            ViewData["CurrentStatus"] = status;

            var borrowings = _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(b => b.Categories) // FIXED: Use Categories collection instead of Category
                .Include(b => b.Member)
                .AsQueryable();

            switch (status)
            {
                case "active":
                    borrowings = borrowings.Where(b => b.Status == "Emprestado");
                    break;
                case "overdue":
                    borrowings = borrowings.Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now);
                    break;
                case "returned":
                    borrowings = borrowings.Where(b => b.Status == "Devolvido");
                    break;
            }

            return View(await borrowings.OrderByDescending(b => b.BorrowDate).ToListAsync());
        }

        // GET: Borrowings/Details/5 - FIXED to include Categories
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(b => b.Categories)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }


        // GET: Borrowings/Create
        public async Task<IActionResult> Create(Guid? bookId, Guid? memberId)
        {
            // Load available books and active members for dropdowns
            var availableBooks = await _context.Books
                .Include(b => b.Categories)
                .Where(b => b.Available)
                .OrderBy(b => b.Title)
                .ToListAsync();

            var activeMembers = await _context.Members
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            // Create SelectLists for dropdowns
            ViewBag.Books = new SelectList(availableBooks, "BookId", "Title", bookId);
            ViewBag.Members = new SelectList(activeMembers, "MemberId", "Name", memberId);

            // If specific book or member is pre-selected, load their details
            if (bookId.HasValue)
            {
                var selectedBook = availableBooks.FirstOrDefault(b => b.BookId == bookId);
                ViewBag.SelectedBook = selectedBook;
            }

            if (memberId.HasValue)
            {
                var selectedMember = activeMembers.FirstOrDefault(m => m.MemberId == memberId);
                ViewBag.SelectedMember = selectedMember;
            }

            // Create new borrowing with default due date (14 days from now)
            var borrowing = new Borrowing
            {
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                BookId = bookId ?? Guid.Empty,
                MemberId = memberId ?? Guid.Empty
            };

            return View(borrowing);
        }

        // POST: Borrowings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,MemberId,DueDate")] Borrowing borrowing)
        {
            ModelState.Remove("Book");
            ModelState.Remove("Member");

            if (ModelState.IsValid)
            {
                // Validate book availability
                var book = await _context.Books.FindAsync(borrowing.BookId);
                if (book == null || !book.Available)
                {
                    ModelState.AddModelError("BookId", "Este livro não está disponível para empréstimo.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                // Validate member status
                var member = await _context.Members.FindAsync(borrowing.MemberId);
                if (member == null || !member.IsActive)
                {
                    ModelState.AddModelError("MemberId", "Membro não encontrado ou inativo.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                // Check if member has overdue books
                var overdueBooks = await _context.Borrowings
                    .Where(b => b.MemberId == borrowing.MemberId &&
                               b.Status == "Emprestado" &&
                               b.DueDate < DateTime.Now)
                    .CountAsync();

                if (overdueBooks > 0)
                {
                    ModelState.AddModelError("MemberId", $"Este membro tem {overdueBooks} livro(s) em atraso. Não é possível fazer novos empréstimos.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                // Validate due date
                if (borrowing.DueDate <= DateTime.Now)
                {
                    ModelState.AddModelError("DueDate", "A data limite deve ser posterior à data atual.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                // Create the borrowing record
                borrowing.BorrowingId = Guid.NewGuid();
                borrowing.BorrowDate = DateTime.Now;
                borrowing.Status = "Emprestado";

                // Mark book as unavailable (single copy logic)
                book.Available = false;

                _context.Add(borrowing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Empréstimo criado com sucesso! Livro '{book.Title}' emprestado a {member.Name}.";
                return RedirectToAction("Index");
            }

            await LoadCreateViewData(borrowing);
            return View(borrowing);
        }

        private async Task LoadCreateViewData(Borrowing borrowing)
        {
            var availableBooks = await _context.Books
                .Include(b => b.Categories)
                .Where(b => b.Available)
                .OrderBy(b => b.Title)
                .ToListAsync();

            var activeMembers = await _context.Members
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Books = new SelectList(availableBooks, "BookId", "Title", borrowing.BookId);
            ViewBag.Members = new SelectList(activeMembers, "MemberId", "Name", borrowing.MemberId);
        }

        // Return book - makes single copy available again
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(Guid id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            if (borrowing.Status != "Emprestado")
            {
                TempData["ErrorMessage"] = "Este empréstimo já foi devolvido.";
                return RedirectToAction(nameof(Index));
            }

            // Mark as returned
            borrowing.ReturnDate = DateTime.Now;
            borrowing.Status = "Devolvido";

            // FIXED: Make the single copy available again
            borrowing.Book.Available = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Livro devolvido com sucesso!";
            return RedirectToAction(nameof(Index));
        }

    }
}
