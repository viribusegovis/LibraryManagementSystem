// Hubs/LibraryHub.cs
using Microsoft.AspNetCore.SignalR;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Hubs
{
    public class LibraryHub : Hub
    {
        private readonly LibraryContext _context;

        public LibraryHub(LibraryContext context)
        {
            _context = context;
        }

        public async Task JoinBookGroup(string bookId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Book_{bookId}");
        }

        public async Task LeaveBookGroup(string bookId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Book_{bookId}");
        }

        // FIXED: Updated to use IsLike instead of IsLiked
        public async Task ToggleLike(string bookId, string memberId, bool isLike)
        {
            var bookGuid = Guid.Parse(bookId);
            var memberGuid = Guid.Parse(memberId);

            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(r => r.BookId == bookGuid && r.MemberId == memberGuid);

            if (existingReview == null)
            {
                existingReview = new BookReview
                {
                    BookId = bookGuid,
                    MemberId = memberGuid,
                    IsLike = isLike, // FIXED: Changed from IsLiked to IsLike
                    ReviewDate = DateTime.Now
                };
                _context.BookReviews.Add(existingReview);
            }
            else
            {
                // FIXED: Updated logic to match IsLike property
                existingReview.IsLike = isLike;
                existingReview.ReviewDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // FIXED: Updated queries to use IsLike
            var likesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLike == true);
            var dislikesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLike == false);

            // Send real-time update to all clients in the book group
            await Clients.Group($"Book_{bookId}").SendAsync("UpdateReactions", new
            {
                BookId = bookId,
                LikesCount = likesCount,
                DislikesCount = dislikesCount,
                UserId = memberId,
                UserReaction = existingReview.IsLike // FIXED: Changed from IsLiked to IsLike
            });
        }

        // NEW: Method to add comments with real-time updates
        public async Task AddComment(string bookId, string memberId, string comment, bool isLike)
        {
            var bookGuid = Guid.Parse(bookId);
            var memberGuid = Guid.Parse(memberId);

            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(r => r.BookId == bookGuid && r.MemberId == memberGuid);

            if (existingReview == null)
            {
                existingReview = new BookReview
                {
                    BookId = bookGuid,
                    MemberId = memberGuid,
                    IsLike = isLike,
                    Comment = comment,
                    ReviewDate = DateTime.Now
                };
                _context.BookReviews.Add(existingReview);
            }
            else
            {
                existingReview.IsLike = isLike;
                existingReview.Comment = comment;
                existingReview.ReviewDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Get member name for display
            var member = await _context.Members.FindAsync(memberGuid);

            // Send real-time comment update
            await Clients.Group($"Book_{bookId}").SendAsync("NewComment", new
            {
                BookId = bookId,
                MemberName = member?.Name ?? "Utilizador",
                Comment = comment,
                ReviewDate = DateTime.Now.ToString("dd/MM/yyyy"),
                IsLike = isLike
            });

            // Update reaction counts
            var likesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLike == true);
            var dislikesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLike == false);

            await Clients.Group($"Book_{bookId}").SendAsync("UpdateReactions", new
            {
                BookId = bookId,
                LikesCount = likesCount,
                DislikesCount = dislikesCount,
                UserId = memberId,
                UserReaction = existingReview.IsLike
            });
        }

        // Connection management for Portuguese interface compliance
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
