// Controllers/BooksController.cs - Complete with member view and admin functionality
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Books - Admin Index (Librarians only)
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Index(string searchString, Guid? categoryId, string availability, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "Author" ? "author_desc" : "Author";

            var books = from b in _context.Books
                        .Include(b => b.Categories)
                        .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                            .ThenInclude(br => br.Member)
                        select b;

            // Search filter
            if (!String.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString)
                                      || b.Author.Contains(searchString)
                                      || b.ISBN.Contains(searchString));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                books = books.Where(b => b.Categories.Any(c => c.CategoryId == categoryId));
            }

            // State/Availability filter
            if (!String.IsNullOrEmpty(availability))
            {
                switch (availability.ToLower())
                {
                    case "available":
                        books = books.Where(b => b.Available && !b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "borrowed":
                        books = books.Where(b => b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "unavailable":
                        books = books.Where(b => !b.Available && !b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                }
            }

            // Sorting
            switch (sortOrder)
            {
                case "title_desc":
                    books = books.OrderByDescending(b => b.Title);
                    break;
                case "Author":
                    books = books.OrderBy(b => b.Author);
                    break;
                case "author_desc":
                    books = books.OrderByDescending(b => b.Author);
                    break;
                default:
                    books = books.OrderBy(b => b.Title);
                    break;
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedAvailability = availability;

            return View(await books.ToListAsync());
        }

        // GET: Books/Details/5 - Admin Details (Librarians only)
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // GET: Books/View/5 - Member View (Public access)
        // In BooksController.cs - Update View action
        [AllowAnonymous]
        public async Task<IActionResult> View(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            // Check borrowing and review status for authenticated members
            if (User.Identity.IsAuthenticated && User.IsInRole("Membro"))
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

                if (member != null)
                {
                    ViewBag.CurrentMemberId = member.MemberId.ToString(); // Add this line
                    ViewBag.HasBorrowedBook = await _context.Borrowings
                        .AnyAsync(b => b.BookId == id && b.MemberId == member.MemberId);

                    ViewBag.HasReviewed = await _context.BookReviews
                        .AnyAsync(r => r.BookId == id && r.MemberId == member.MemberId);
                }
                else
                {
                    ViewBag.CurrentMemberId = "";
                    ViewBag.HasBorrowedBook = false;
                    ViewBag.HasReviewed = false;
                }
            }
            else
            {
                ViewBag.CurrentMemberId = "";
                ViewBag.HasBorrowedBook = false;
                ViewBag.HasReviewed = false;
            }

            return View("MemberDetails", book);
        }


        // GET: Books/Create
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Create([Bind("Title,Author,ISBN,YearPublished,Available")] Book book, Guid[] selectedCategories)
        {
            if (ModelState.IsValid)
            {
                var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == book.ISBN);
                if (existingBook != null)
                {
                    ModelState.AddModelError("ISBN", "Já existe um livro com este ISBN.");
                    ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                    return View(book);
                }

                book.BookId = Guid.NewGuid();
                book.CreatedDate = DateTime.Now;

                if (selectedCategories != null && selectedCategories.Length > 0)
                {
                    var categories = await _context.Categories
                        .Where(c => selectedCategories.Contains(c.CategoryId))
                        .ToListAsync();
                    book.Categories = categories;
                }

                _context.Add(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Livro criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            // Check if book is currently borrowed
            var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
            if (activeBorrowing != null)
            {
                ViewBag.WarningMessage = $"Atenção: Este livro está atualmente emprestado a {activeBorrowing.Member?.Name ?? "um membro"}.";
            }

            ViewBag.Categories = new MultiSelectList(
                await _context.Categories.ToListAsync(),
                "CategoryId",
                "Name",
                book.Categories.Select(c => c.CategoryId));

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Edit(Guid id, [Bind("BookId,Title,Author,ISBN,YearPublished,Available,CreatedDate")] Book book, Guid[] selectedCategories)
        {
            if (id != book.BookId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate ISBN
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == book.ISBN && b.BookId != book.BookId);

                    if (existingBook != null)
                    {
                        ModelState.AddModelError("ISBN", "Já existe outro livro com este ISBN.");
                        ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                        return View(book);
                    }

                    // Get existing book with categories
                    var existingBookWithCategories = await _context.Books
                        .Include(b => b.Categories)
                        .FirstOrDefaultAsync(b => b.BookId == id);

                    if (existingBookWithCategories != null)
                    {
                        // Update basic properties
                        existingBookWithCategories.Title = book.Title;
                        existingBookWithCategories.Author = book.Author;
                        existingBookWithCategories.ISBN = book.ISBN;
                        existingBookWithCategories.YearPublished = book.YearPublished;
                        existingBookWithCategories.Available = book.Available;

                        // Update categories (many-to-many relationship)
                        existingBookWithCategories.Categories.Clear();
                        if (selectedCategories != null && selectedCategories.Length > 0)
                        {
                            var categories = await _context.Categories
                                .Where(c => selectedCategories.Contains(c.CategoryId))
                                .ToListAsync();

                            foreach (var category in categories)
                            {
                                existingBookWithCategories.Categories.Add(category);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Livro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .Include(b => b.BookReviews)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book != null)
            {
                // Check if book has active borrowings
                var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
                if (activeBorrowing != null)
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar o livro '{book.Title}'. Está atualmente emprestado.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                // Remove all dependencies
                if (book.BookReviews.Any())
                {
                    _context.BookReviews.RemoveRange(book.BookReviews);
                }

                if (book.Borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(book.Borrowings);
                }

                // Clear many-to-many relationships
                book.Categories.Clear();

                // Remove the book
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Livro '{book.Title}' e todas as suas dependências foram eliminados com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/BorrowingHistory/5
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> BorrowingHistory(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        // POST: Books/ToggleAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "ID do livro inválido.";
                return RedirectToAction(nameof(Index));
            }

            var book = await _context.Books
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                TempData["ErrorMessage"] = "Livro não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Check if book is currently borrowed
            var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
            if (activeBorrowing != null)
            {
                TempData["ErrorMessage"] = "Não é possível alterar a disponibilidade. O livro está atualmente emprestado.";
                return RedirectToAction(nameof(Index));
            }

            // Toggle availability
            book.Available = !book.Available;
            await _context.SaveChangesAsync();

            var statusMessage = book.Available ? "disponível" : "indisponível";
            TempData["SuccessMessage"] = $"Livro '{book.Title}' marcado como {statusMessage} com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
