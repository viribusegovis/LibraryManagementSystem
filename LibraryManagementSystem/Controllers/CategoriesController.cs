// Controllers/CategoriesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Bibliotecário")]
    public class CategoriesController : Controller
    {
        private readonly LibraryContext _context;

        public CategoriesController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Books)
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate names
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory != null)
                {
                    ModelState.AddModelError("Name", "Já existe uma categoria com este nome.");
                    return View(category);
                }

                category.CategoryId = Guid.NewGuid();
                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoria criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CategoryId,Name,Description")] Category category)
        {
            if (id != category.CategoryId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate names
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.CategoryId != category.CategoryId);

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Já existe uma categoria com este nome.");

                        // Reload books for the view
                        var categoryWithBooks = await _context.Categories
                            .Include(c => c.Books)
                            .FirstOrDefaultAsync(c => c.CategoryId == id);
                        category.Books = categoryWithBooks?.Books ?? new List<Book>();

                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Categoria atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null)
                return NotFound();

            // Check if category has books with active borrowings
            var activeBorrowings = category.Books.SelectMany(b => b.Borrowings)
                .Count(br => br.Status == "Emprestado");

            ViewBag.HasActiveBorrowings = activeBorrowings > 0;
            ViewBag.ActiveBorrowingsCount = activeBorrowings;

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category != null)
            {
                // Check if category has associated books
                if (category.Books.Any())
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar a categoria '{category.Name}'. Existem {category.Books.Count} livro(s) associado(s).";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Categoria '{category.Name}' eliminada com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(Guid id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
