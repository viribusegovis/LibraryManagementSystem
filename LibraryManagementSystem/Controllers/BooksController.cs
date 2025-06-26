// Controllers/BooksController.cs - Fixed for Many-to-Many Categories
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

        // GET: Books - FIXED to use Categories properly
        public async Task<IActionResult> Index(string searchString, Guid? categoryId, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "Author" ? "author_desc" : "Author";

            // FIXED: Properly include Member navigation property
            var books = from b in _context.Books
                        .Include(b => b.Categories)
                        .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                            .ThenInclude(br => br.Member) // This loads the Member object
                        select b;

            // Search functionality
            if (!String.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString)
                                      || b.Author.Contains(searchString)
                                      || b.ISBN.Contains(searchString));
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                books = books.Where(b => b.Categories.Any(c => c.CategoryId == categoryId));
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

            // Categories for dropdown filter
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            ViewBag.SelectedCategory = categoryId;

            return View(await books.ToListAsync());
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Categories) // FIXED: Use Categories collection
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create - FIXED for many-to-many
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            return View();
        }

        // POST: Books/Create - FIXED for many-to-many categories
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Author,ISBN,YearPublished,Available")] Book book, Guid[] selectedCategories)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate ISBN
                var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == book.ISBN);
                if (existingBook != null)
                {
                    ModelState.AddModelError("ISBN", "Já existe um livro com este ISBN.");
                    ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                    return View(book);
                }

                book.BookId = Guid.NewGuid();
                book.CreatedDate = DateTime.Now;

                // FIXED: Add selected categories to the book
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

        // GET: Books/Edit/5 - FIXED for many-to-many
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            // Warning if book is currently borrowed
            if (!book.Available)
            {
                ViewBag.WarningMessage = "Atenção: Este livro está atualmente emprestado.";
            }

            ViewBag.Categories = new MultiSelectList(
                await _context.Categories.ToListAsync(),
                "CategoryId",
                "Name",
                book.Categories.Select(c => c.CategoryId));

            return View(book);
        }

        // POST: Books/Edit/5 - FIXED for many-to-many categories
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("BookId,Title,Author,ISBN,YearPublished,Available,CreatedDate")] Book book, Guid[] selectedCategories)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate ISBN (excluding current book)
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == book.ISBN && b.BookId != book.BookId);

                    if (existingBook != null)
                    {
                        ModelState.AddModelError("ISBN", "Já existe outro livro com este ISBN.");
                        ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                        return View(book);
                    }

                    // FIXED: Load existing book with categories
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

                        // FIXED: Update categories (many-to-many)
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
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
            return View(book);
        }

        // GET: Books/Delete/5 - FIXED to include Categories
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Categories) // FIXED: Use Categories collection
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            // Check if book has active borrowings
            var activeBorrowings = book.Borrowings.Count(b => b.Status == "Emprestado");
            ViewBag.HasActiveBorrowings = activeBorrowings > 0;
            ViewBag.ActiveBorrowingsCount = activeBorrowings;

            return View(book);
        }

        // POST: Books/Delete/5 - No changes needed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book != null)
            {
                // Check if book has active borrowings
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

        // GET: Books/BorrowingHistory/5 - FIXED to include Categories
        public async Task<IActionResult> BorrowingHistory(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Categories) // FIXED: Use Categories collection
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/ToggleAvailability/5 - No changes needed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return Json(new { success = false, message = "Livro não encontrado." });
            }

            // Check if book has active borrowings before making it unavailable
            if (book.Available)
            {
                var activeBorrowings = book.Borrowings.Count(b => b.Status == "Emprestado");

                if (activeBorrowings > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Não é possível tornar o livro indisponível. Existe um empréstimo ativo."
                    });
                }

                // Mark as unavailable (manual override)
                book.Available = false;
            }
            else
            {
                // Check if there are any active borrowings that should prevent making it available
                var activeBorrowings = book.Borrowings.Count(b => b.Status == "Emprestado");

                if (activeBorrowings > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Não é possível tornar o livro disponível. Existe um empréstimo ativo que deve ser devolvido primeiro."
                    });
                }

                // Mark as available
                book.Available = true;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                available = book.Available,
                message = book.Available ? "Livro marcado como disponível." : "Livro marcado como indisponível."
            });
        }

        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
