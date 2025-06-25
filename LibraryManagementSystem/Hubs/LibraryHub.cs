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
                    IsLiked = isLike,
                    ReviewDate = DateTime.Now
                };
                _context.BookReviews.Add(existingReview);
            }
            else
            {
                if (existingReview.IsLiked == isLike)
                {
                    existingReview.IsLiked = null;
                }
                else
                {
                    existingReview.IsLiked = isLike;
                }
            }

            await _context.SaveChangesAsync();

            var likesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLiked == true);
            var dislikesCount = await _context.BookReviews
                .CountAsync(r => r.BookId == bookGuid && r.IsLiked == false);

            await Clients.Group($"Book_{bookId}").SendAsync("UpdateReactions", new
            {
                BookId = bookId,
                LikesCount = likesCount,
                DislikesCount = dislikesCount,
                UserId = memberId,
                UserReaction = existingReview.IsLiked
            });
        }
    }
}
