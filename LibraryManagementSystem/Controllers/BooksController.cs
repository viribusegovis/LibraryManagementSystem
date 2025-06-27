// Controllers/BooksController.cs - Three-state status logic, many-to-many categories, and toggle
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Bibliotecário")]
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Books
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
                        // Available: Available = true AND no active borrowings
                        books = books.Where(b => b.Available && !b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "borrowed":
                        // Borrowed: Has active borrowings
                        books = books.Where(b => b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "unavailable":
                        // Unavailable: Available = false AND no active borrowings
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

        // GET: Books/Details/5
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

        // GET: Books/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            var activeBorrowings = book.Borrowings.Count(b => b.Status == "Emprestado");
            ViewBag.HasActiveBorrowings = activeBorrowings > 0;
            ViewBag.ActiveBorrowingsCount = activeBorrowings;

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book != null)
            {
                var activeBorrowings = book.Borrowings.Count(b => b.Status == "Emprestado");
                if (activeBorrowings > 0)
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar o livro. Existem {activeBorrowings} empréstimos ativos.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Livro eliminado com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/BorrowingHistory/5
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
        public async Task<IActionResult> ToggleAvailability([FromBody] ToggleRequest request)
        {
            try
            {
                var book = await _context.Books
                    .Include(b => b.Borrowings)
                    .FirstOrDefaultAsync(b => b.BookId == request.Id);

                if (book == null)
                {
                    return Json(new { success = false, message = "Livro não encontrado." });
                }

                // Check if book is currently borrowed
                var activeBorrowing = book.Borrowings?.FirstOrDefault(b => b.Status == "Emprestado");
                if (activeBorrowing != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Não é possível alterar a disponibilidade. O livro está atualmente emprestado."
                    });
                }

                // Toggle availability (between disponível and indisponível)
                book.Available = !book.Available;
                await _context.SaveChangesAsync();

                var statusMessage = book.Available ? "disponível" : "indisponível";
                return Json(new
                {
                    success = true,
                    message = $"Livro marcado como {statusMessage} com sucesso.",
                    newStatus = book.Available
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Erro interno do servidor ao alterar disponibilidade."
                });
            }
        }

        public class ToggleRequest
        {
            public Guid Id { get; set; }
        }


    }
}
