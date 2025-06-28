using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Hubs;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class BookReviewsController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IHubContext<BookReviewHub> _hubContext;

        public BookReviewsController(LibraryContext context, IHubContext<BookReviewHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // POST: BookReviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> Create(Guid BookId, bool IsLike, string? Comment, int? Rating)
        {
            if (BookId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Livro não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Membro não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            var hasBorrowedBook = await _context.Borrowings
                .AnyAsync(b => b.BookId == BookId && b.MemberId == member.MemberId);

            if (!hasBorrowedBook)
            {
                TempData["ErrorMessage"] = "Só pode avaliar livros que já emprestou.";
                return RedirectToAction("View", "Books", new { id = BookId });
            }

            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(r => r.BookId == BookId && r.MemberId == member.MemberId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Já avaliou este livro anteriormente.";
                return RedirectToAction("View", "Books", new { id = BookId });
            }

            var review = new BookReview
            {
                BookReviewId = Guid.NewGuid(),
                BookId = BookId,
                MemberId = member.MemberId,
                IsLike = IsLike,
                Comment = Comment?.Trim(),
                Rating = Rating,
                ReviewDate = DateTime.Now
            };

            _context.BookReviews.Add(review);
            await _context.SaveChangesAsync();

            // Broadcast updates (existing code)
            var book = await _context.Books
                .Include(b => b.BookReviews)
                    .ThenInclude(r => r.Member)
                .FirstOrDefaultAsync(b => b.BookId == BookId);

            if (book != null)
            {
                var likes = book.BookReviews.Count(r => r.IsLike);
                var dislikes = book.BookReviews.Count(r => !r.IsLike);
                var totalReviews = book.BookReviews.Count;
                var averageRating = book.BookReviews.Where(r => r.Rating.HasValue).Average(r => r.Rating) ?? 0;

                try
                {
                    await _hubContext.Clients.Group($"Book_{BookId}").SendAsync("UpdateBookStats", new
                    {
                        bookId = BookId.ToString(),
                        likes = likes,
                        dislikes = dislikes,
                        totalReviews = totalReviews,
                        averageRating = Math.Round(averageRating, 1)
                    });

                    await _hubContext.Clients.Group($"Book_{BookId}").SendAsync("NewReview", new
                    {
                        reviewId = review.BookReviewId.ToString(),
                        memberName = member.Name,
                        isLike = review.IsLike,
                        comment = review.Comment,
                        rating = review.Rating,
                        reviewDate = review.ReviewDate.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR Error: {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = "Avaliação adicionada com sucesso!";
            return RedirectToAction("View", "Books", new { id = BookId });
        }

        // GET: BookReviews/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var review = await _context.BookReviews
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.BookReviewId == id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Avaliação não encontrada.";
                return RedirectToAction("Index", "Home");
            }

            // Check if user can edit this review
            if (User.IsInRole("Membro"))
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

                if (member == null || review.MemberId != member.MemberId)
                {
                    TempData["ErrorMessage"] = "Só pode editar as suas próprias avaliações.";
                    return RedirectToAction("View", "Books", new { id = review.BookId });
                }
            }

            return View(review);
        }

        // POST: BookReviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, bool IsLike, string? Comment, int? Rating)
        {
            var review = await _context.BookReviews
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.BookReviewId == id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Avaliação não encontrada.";
                return RedirectToAction("Index", "Home");
            }

            // Check permissions
            if (User.IsInRole("Membro"))
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

                if (member == null || review.MemberId != member.MemberId)
                {
                    TempData["ErrorMessage"] = "Só pode editar as suas próprias avaliações.";
                    return RedirectToAction("View", "Books", new { id = review.BookId });
                }
            }

            // Update review
            review.IsLike = IsLike;
            review.Comment = Comment?.Trim();
            review.Rating = Rating;
            review.ReviewDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Broadcast updates
            var book = await _context.Books
                .Include(b => b.BookReviews)
                .FirstOrDefaultAsync(b => b.BookId == review.BookId);

            if (book != null)
            {
                var likes = book.BookReviews.Count(r => r.IsLike);
                var dislikes = book.BookReviews.Count(r => !r.IsLike);
                var totalReviews = book.BookReviews.Count;
                var averageRating = book.BookReviews.Where(r => r.Rating.HasValue).Average(r => r.Rating) ?? 0;

                try
                {
                    await _hubContext.Clients.Group($"Book_{review.BookId}").SendAsync("UpdateBookStats", new
                    {
                        bookId = review.BookId.ToString(),
                        likes = likes,
                        dislikes = dislikes,
                        totalReviews = totalReviews,
                        averageRating = Math.Round(averageRating, 1)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR Error: {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = "Avaliação atualizada com sucesso!";
            return RedirectToAction("View", "Books", new { id = review.BookId });
        }

        // POST: BookReviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var review = await _context.BookReviews
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.BookReviewId == id);

            if (review != null)
            {
                var bookId = review.BookId;
                var isAdminAction = User.IsInRole("Bibliotecário");

                // Check permissions for members
                if (User.IsInRole("Membro"))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                    var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

                    if (member == null || review.MemberId != member.MemberId)
                    {
                        TempData["ErrorMessage"] = "Só pode eliminar as suas próprias avaliações.";
                        return RedirectToAction("View", "Books", new { id = bookId });
                    }
                }

                _context.BookReviews.Remove(review);
                await _context.SaveChangesAsync();

                // Broadcast updates
                var book = await _context.Books
                    .Include(b => b.BookReviews)
                    .FirstOrDefaultAsync(b => b.BookId == bookId);

                if (book != null)
                {
                    var likes = book.BookReviews.Count(r => r.IsLike);
                    var dislikes = book.BookReviews.Count(r => !r.IsLike);
                    var totalReviews = book.BookReviews.Count;
                    var averageRating = book.BookReviews.Where(r => r.Rating.HasValue).Average(r => r.Rating) ?? 0;

                    try
                    {
                        await _hubContext.Clients.Group($"Book_{bookId}").SendAsync("UpdateBookStats", new
                        {
                            bookId = bookId.ToString(),
                            likes = likes,
                            dislikes = dislikes,
                            totalReviews = totalReviews,
                            averageRating = Math.Round(averageRating, 1)
                        });

                        await _hubContext.Clients.Group($"Book_{bookId}").SendAsync("ReviewDeleted", new
                        {
                            reviewId = id.ToString()
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SignalR Error: {ex.Message}");
                    }
                }

                TempData["SuccessMessage"] = "Avaliação eliminada com sucesso!";

                // FIXED: Redirect based on user role
                if (isAdminAction)
                {
                    return RedirectToAction("Details", "Books", new { id = bookId }); // Admin goes to admin details
                }
                else
                {
                    return RedirectToAction("View", "Books", new { id = bookId }); // Member goes to public view
                }
            }

            TempData["ErrorMessage"] = "Avaliação não encontrada.";
            return RedirectToAction("Index", "Home");
        }
    }
}
