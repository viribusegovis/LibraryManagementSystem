// Hubs/BookReviewHub.cs - Enhanced with database context
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Hubs
{
    [Authorize]
    public class BookReviewHub : Hub
    {
        private readonly LibraryContext _context;

        public BookReviewHub(LibraryContext context)
        {
            _context = context;
        }

        public async Task JoinBookGroup(string bookId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Book_{bookId}");
            Console.WriteLine($"User {Context.UserIdentifier} joined group Book_{bookId}");

            // Notify group about new viewer
            await Clients.Group($"Book_{bookId}").SendAsync("ViewerJoined", new
            {
                userId = Context.UserIdentifier,
                connectionId = Context.ConnectionId
            });
        }

        public async Task LeaveBookGroup(string bookId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Book_{bookId}");
            Console.WriteLine($"User {Context.UserIdentifier} left group Book_{bookId}");

            // Notify group about viewer leaving
            await Clients.Group($"Book_{bookId}").SendAsync("ViewerLeft", new
            {
                userId = Context.UserIdentifier,
                connectionId = Context.ConnectionId
            });
        }

        // Enhanced method for quick rating from dashboard
        public async Task QuickRate(string bookId, string memberId, bool isLike)
        {
            try
            {
                var bookGuid = Guid.Parse(bookId);
                var memberGuid = Guid.Parse(memberId);

                // Check if member has borrowed this book
                var hasBorrowedBook = await _context.Borrowings
                    .AnyAsync(b => b.BookId == bookGuid && b.MemberId == memberGuid);

                if (!hasBorrowedBook)
                {
                    await Clients.Caller.SendAsync("RatingError", new
                    {
                        message = "Só pode avaliar livros que já emprestou."
                    });
                    return;
                }

                // Check if already reviewed
                var existingReview = await _context.BookReviews
                    .FirstOrDefaultAsync(r => r.BookId == bookGuid && r.MemberId == memberGuid);

                if (existingReview != null)
                {
                    await Clients.Caller.SendAsync("RatingError", new
                    {
                        message = "Já avaliou este livro anteriormente."
                    });
                    return;
                }

                // Create new review
                var review = new BookReview
                {
                    BookReviewId = Guid.NewGuid(),
                    BookId = bookGuid,
                    MemberId = memberGuid,
                    IsLike = isLike,
                    ReviewDate = DateTime.Now
                };

                _context.BookReviews.Add(review);
                await _context.SaveChangesAsync();

                // Get member info and updated stats
                var member = await _context.Members.FindAsync(memberGuid);
                var book = await _context.Books
                    .Include(b => b.BookReviews)
                    .FirstOrDefaultAsync(b => b.BookId == bookGuid);

                if (book != null && member != null)
                {
                    var likes = book.BookReviews.Count(r => r.IsLike);
                    var dislikes = book.BookReviews.Count(r => !r.IsLike);
                    var totalReviews = book.BookReviews.Count;
                    var averageRating = book.BookReviews.Where(r => r.Rating.HasValue).Average(r => r.Rating) ?? 0;

                    // Broadcast to all viewers
                    await Clients.Group($"Book_{bookId}").SendAsync("UpdateBookStats", new
                    {
                        bookId = bookId,
                        likes = likes,
                        dislikes = dislikes,
                        totalReviews = totalReviews,
                        averageRating = Math.Round(averageRating, 1)
                    });

                    await Clients.Group($"Book_{bookId}").SendAsync("NewReview", new
                    {
                        reviewId = review.BookReviewId.ToString(),
                        memberName = member.Name,
                        isLike = review.IsLike,
                        comment = review.Comment,
                        rating = review.Rating,
                        reviewDate = review.ReviewDate.ToString("dd/MM/yyyy HH:mm")
                    });

                    Console.WriteLine($"SignalR: Quick rate broadcasted for book {bookId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR QuickRate Error: {ex.Message}");
                await Clients.Caller.SendAsync("RatingError", new
                {
                    message = "Erro ao processar avaliação. Tente novamente."
                });
            }
        }

        // Method for commenting from dashboard
        public async Task AddQuickComment(string bookId, string memberId, string comment, bool isLike, int? rating = null)
        {
            try
            {
                var bookGuid = Guid.Parse(bookId);
                var memberGuid = Guid.Parse(memberId);

                var member = await _context.Members.FindAsync(memberGuid);
                if (member == null) return;

                // Check borrowing history
                var hasBorrowedBook = await _context.Borrowings
                    .AnyAsync(b => b.BookId == bookGuid && b.MemberId == memberGuid);

                if (!hasBorrowedBook)
                {
                    await Clients.Caller.SendAsync("CommentError", new
                    {
                        message = "Só pode comentar livros que já emprestou."
                    });
                    return;
                }

                var existingReview = await _context.BookReviews
                    .FirstOrDefaultAsync(r => r.BookId == bookGuid && r.MemberId == memberGuid);

                if (existingReview == null)
                {
                    existingReview = new BookReview
                    {
                        BookReviewId = Guid.NewGuid(),
                        BookId = bookGuid,
                        MemberId = memberGuid,
                        IsLike = isLike,
                        Comment = comment,
                        Rating = rating,
                        ReviewDate = DateTime.Now
                    };
                    _context.BookReviews.Add(existingReview);
                }
                else
                {
                    existingReview.Comment = comment;
                    existingReview.IsLike = isLike;
                    if (rating.HasValue) existingReview.Rating = rating;
                    existingReview.ReviewDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Broadcast update
                await Clients.Group($"Book_{bookId}").SendAsync("NewReview", new
                {
                    reviewId = existingReview.BookReviewId.ToString(),
                    memberName = member.Name,
                    isLike = existingReview.IsLike,
                    comment = existingReview.Comment,
                    rating = existingReview.Rating,
                    reviewDate = existingReview.ReviewDate.ToString("dd/MM/yyyy HH:mm")
                });

                // Update stats
                var book = await _context.Books
                    .Include(b => b.BookReviews)
                    .FirstOrDefaultAsync(b => b.BookId == bookGuid);

                if (book != null)
                {
                    var likes = book.BookReviews.Count(r => r.IsLike);
                    var dislikes = book.BookReviews.Count(r => !r.IsLike);
                    var totalReviews = book.BookReviews.Count;
                    var averageRating = book.BookReviews.Where(r => r.Rating.HasValue).Average(r => r.Rating) ?? 0;

                    await Clients.Group($"Book_{bookId}").SendAsync("UpdateBookStats", new
                    {
                        bookId = bookId,
                        likes = likes,
                        dislikes = dislikes,
                        totalReviews = totalReviews,
                        averageRating = Math.Round(averageRating, 1)
                    });
                }

                Console.WriteLine($"SignalR: Comment added for book {bookId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR AddComment Error: {ex.Message}");
                await Clients.Caller.SendAsync("CommentError", new
                {
                    message = "Erro ao adicionar comentário. Tente novamente."
                });
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"SignalR: User {Context.UserIdentifier} connected to BookReviewHub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"SignalR: User {Context.UserIdentifier} disconnected from BookReviewHub");
            if (exception != null)
            {
                Console.WriteLine($"SignalR Disconnect Error: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
