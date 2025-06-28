using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Hubs
{
    /**
     * Hub SignalR para gestão de avaliações de livros em tempo real
     * Permite comunicação bidirecional entre clientes para atualizações instantâneas
     * Implementa autenticação obrigatória e integração com base de dados
     */
    [Authorize]
    public class BookReviewHub : Hub
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o hub com contexto da base de dados
         * @param context Contexto Entity Framework para operações de dados
         */
        public BookReviewHub(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Adiciona utilizador a grupo específico de um livro
         * Permite receber atualizações em tempo real sobre avaliações do livro
         * @param bookId Identificador único do livro
         */
        public async Task JoinBookGroup(string bookId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Book_{bookId}");
            Console.WriteLine($"User {Context.UserIdentifier} joined group Book_{bookId}");

            /* Notificar grupo sobre novo visualizador */
            await Clients.Group($"Book_{bookId}").SendAsync("ViewerJoined", new
            {
                userId = Context.UserIdentifier,
                connectionId = Context.ConnectionId
            });
        }

        /**
         * Remove utilizador do grupo específico de um livro
         * @param bookId Identificador único do livro
         */
        public async Task LeaveBookGroup(string bookId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Book_{bookId}");
            Console.WriteLine($"User {Context.UserIdentifier} left group Book_{bookId}");

            /* Notificar grupo sobre saída do visualizador */
            await Clients.Group($"Book_{bookId}").SendAsync("ViewerLeft", new
            {
                userId = Context.UserIdentifier,
                connectionId = Context.ConnectionId
            });
        }

        /**
         * Avaliação rápida de livro (like/dislike) com validação de empréstimo
         * Verifica se o membro emprestou o livro antes de permitir avaliação
         * @param bookId Identificador do livro a avaliar
         * @param memberId Identificador do membro que avalia
         * @param isLike True para like, false para dislike
         */
        public async Task QuickRate(string bookId, string memberId, bool isLike)
        {
            try
            {
                var bookGuid = Guid.Parse(bookId);
                var memberGuid = Guid.Parse(memberId);

                /* Validar se o membro emprestou o livro */
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

                /* Verificar se já existe avaliação */
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

                /* Criar nova avaliação */
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

                /* Obter dados atualizados e transmitir para grupo */
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

                    /* Transmitir estatísticas atualizadas para todos os clientes do grupo */
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

        /**
         * Adiciona comentário com avaliação opcional
         * Permite criar ou atualizar avaliação existente com comentário
         * @param bookId Identificador do livro
         * @param memberId Identificador do membro
         * @param comment Texto do comentário
         * @param isLike Avaliação positiva/negativa
         * @param rating Classificação opcional (1-5 estrelas)
         */
        public async Task AddQuickComment(string bookId, string memberId, string comment, bool isLike, int? rating = null)
        {
            try
            {
                var bookGuid = Guid.Parse(bookId);
                var memberGuid = Guid.Parse(memberId);

                var member = await _context.Members.FindAsync(memberGuid);
                if (member == null) return;

                /* Validar histórico de empréstimos */
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

                /* Criar nova avaliação ou atualizar existente */
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

                /* Transmitir nova avaliação para grupo */
                await Clients.Group($"Book_{bookId}").SendAsync("NewReview", new
                {
                    reviewId = existingReview.BookReviewId.ToString(),
                    memberName = member.Name,
                    isLike = existingReview.IsLike,
                    comment = existingReview.Comment,
                    rating = existingReview.Rating,
                    reviewDate = existingReview.ReviewDate.ToString("dd/MM/yyyy HH:mm")
                });

                /* Atualizar estatísticas do livro */
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

        /**
         * Evento de conexão de cliente
         * Registra conexão para debugging e monitorização
         */
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"SignalR: User {Context.UserIdentifier} connected to BookReviewHub");
            await base.OnConnectedAsync();
        }

        /**
         * Evento de desconexão de cliente
         * Registra desconexão e possíveis erros
         * @param exception Exceção que causou a desconexão (se aplicável)
         */
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
