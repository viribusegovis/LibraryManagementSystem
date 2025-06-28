// Controllers/Api/BooksApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        // GET: api/Books
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            var books = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    YearPublished = b.YearPublished,
                    Available = b.Available,
                    Categories = b.Categories.Select(c => c.Name).ToList(),
                    ReviewCount = b.BookReviews.Count,
                    AverageRating = b.BookReviews.Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating) ?? 0,
                    LikesCount = b.BookReviews.Count(r => r.IsLike),
                    DislikesCount = b.BookReviews.Count(r => !r.IsLike)
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET: api/Books/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<BookDetailDto>> GetBook(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                    .ThenInclude(r => r.Member)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound(new { message = "Livro não encontrado" });
            }

            var bookDto = new BookDetailDto
            {
                BookId = book.BookId,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                YearPublished = book.YearPublished,
                Available = book.Available,
                CreatedDate = book.CreatedDate,
                Categories = book.Categories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description
                }).ToList(),
                Reviews = book.BookReviews.Select(r => new ReviewDto
                {
                    ReviewId = r.BookReviewId,
                    MemberName = r.Member.Name,
                    IsLike = r.IsLike,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                }).ToList(),
                Statistics = new BookStatisticsDto
                {
                    TotalReviews = book.BookReviews.Count,
                    AverageRating = book.BookReviews.Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating) ?? 0,
                    LikesCount = book.BookReviews.Count(r => r.IsLike),
                    DislikesCount = book.BookReviews.Count(r => !r.IsLike),
                    TotalBorrowings = book.Borrowings.Count,
                    CurrentlyBorrowed = book.Borrowings.Any(b => b.Status == "Emprestado")
                }
            };

            return Ok(bookDto);
        }

        // POST: api/Books
        [HttpPost]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<ActionResult<BookDto>> PostBook(CreateBookDto createBookDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check for duplicate ISBN
            if (!string.IsNullOrEmpty(createBookDto.ISBN))
            {
                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => b.ISBN == createBookDto.ISBN);

                if (existingBook != null)
                {
                    return Conflict(new { message = "Já existe um livro com este ISBN" });
                }
            }

            var book = new Book
            {
                BookId = Guid.NewGuid(),
                Title = createBookDto.Title,
                Author = createBookDto.Author,
                ISBN = createBookDto.ISBN,
                YearPublished = createBookDto.YearPublished,
                Available = createBookDto.Available,
                CreatedDate = DateTime.Now
            };

            // Add categories if provided
            if (createBookDto.CategoryIds != null && createBookDto.CategoryIds.Any())
            {
                var categories = await _context.Categories
                    .Where(c => createBookDto.CategoryIds.Contains(c.CategoryId))
                    .ToListAsync();
                book.Categories = categories;
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var bookDto = new BookDto
            {
                BookId = book.BookId,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                YearPublished = book.YearPublished,
                Available = book.Available,
                Categories = book.Categories?.Select(c => c.Name).ToList() ?? new List<string>(),
                ReviewCount = 0,
                AverageRating = 0,
                LikesCount = 0,
                DislikesCount = 0
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, bookDto);
        }

        // PUT: api/Books/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> PutBook(Guid id, UpdateBookDto updateBookDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var book = await _context.Books
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound(new { message = "Livro não encontrado" });
            }

            // Check for duplicate ISBN
            if (!string.IsNullOrEmpty(updateBookDto.ISBN) && updateBookDto.ISBN != book.ISBN)
            {
                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => b.ISBN == updateBookDto.ISBN && b.BookId != id);

                if (existingBook != null)
                {
                    return Conflict(new { message = "Já existe outro livro com este ISBN" });
                }
            }

            // Update book properties
            book.Title = updateBookDto.Title;
            book.Author = updateBookDto.Author;
            book.ISBN = updateBookDto.ISBN;
            book.YearPublished = updateBookDto.YearPublished;
            book.Available = updateBookDto.Available;

            // Update categories
            book.Categories.Clear();
            if (updateBookDto.CategoryIds != null && updateBookDto.CategoryIds.Any())
            {
                var categories = await _context.Categories
                    .Where(c => updateBookDto.CategoryIds.Contains(c.CategoryId))
                    .ToListAsync();
                book.Categories = categories;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Books/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .Include(b => b.BookReviews)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound(new { message = "Livro não encontrado" });
            }

            // Check if book has active borrowings
            var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
            if (activeBorrowing != null)
            {
                return BadRequest(new { message = "Não é possível eliminar o livro. Está atualmente emprestado." });
            }

            // Remove dependencies
            if (book.BookReviews.Any())
            {
                _context.BookReviews.RemoveRange(book.BookReviews);
            }

            if (book.Borrowings.Any())
            {
                _context.Borrowings.RemoveRange(book.Borrowings);
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Livro eliminado com sucesso" });
        }

        // GET: api/Books/search
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks(
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? category,
            [FromQuery] bool? available,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(b => b.Title.Contains(title));
            }

            if (!string.IsNullOrEmpty(author))
            {
                query = query.Where(b => b.Author.Contains(author));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(b => b.Categories.Any(c => c.Name.Contains(category)));
            }

            if (available.HasValue)
            {
                query = query.Where(b => b.Available == available.Value);
            }

            var totalCount = await query.CountAsync();
            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    YearPublished = b.YearPublished,
                    Available = b.Available,
                    Categories = b.Categories.Select(c => c.Name).ToList(),
                    ReviewCount = b.BookReviews.Count,
                    AverageRating = b.BookReviews.Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating) ?? 0,
                    LikesCount = b.BookReviews.Count(r => r.IsLike),
                    DislikesCount = b.BookReviews.Count(r => !r.IsLike)
                })
                .ToListAsync();

            var result = new
            {
                Books = books,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }

        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
