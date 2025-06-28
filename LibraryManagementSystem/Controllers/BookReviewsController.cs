using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador para gestão de avaliações de livros pelos membros
     * Implementa operações CRUD para avaliações (gostos, classificações, comentários).
     * Controla acesso baseado em roles e valida regras de negócio.
     */
    [Authorize]
    public class BookReviewsController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public BookReviewsController(LibraryContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /**
         * Processa criação de nova avaliação de livro
         * 
         * @param BookId Identificador único do livro a avaliar (GUID)
         * @param IsLike Indica se gosta (true) ou não gosta (false) do livro
         * @param Comment Comentário opcional sobre o livro (máximo 1000 caracteres)
         * @param Rating Classificação de 1 a 5 estrelas (opcional)
         * @return Redirecionamento para página do livro com mensagem de sucesso ou erro
         */
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

            /* Obter membro atual autenticado através do contexto de utilizador */
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Membro não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            /* Validar regra de negócio: só pode avaliar livros que já emprestou */
            var hasBorrowedBook = await _context.Borrowings
                .AnyAsync(b => b.BookId == BookId && b.MemberId == member.MemberId);

            if (!hasBorrowedBook)
            {
                TempData["ErrorMessage"] = "Só pode avaliar livros que já emprestou.";
                return RedirectToAction("View", "Books", new { id = BookId });
            }

            /* Verificar se já avaliou este livro - evita avaliações duplicadas */
            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(r => r.BookId == BookId && r.MemberId == member.MemberId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Já avaliou este livro anteriormente.";
                return RedirectToAction("View", "Books", new { id = BookId });
            }

            /* Criar nova avaliação com dados validados */
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

            TempData["SuccessMessage"] = "Avaliação adicionada com sucesso!";
            return RedirectToAction("View", "Books", new { id = BookId });
        }

        /**
         * Apresenta formulário para editar avaliação existente
         * 
         * @param id Identificador único da avaliação a editar (GUID)
         * @return Vista de edição com dados da avaliação ou redirecionamento em caso de erro
         */
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

            /* Verificar permissões: membros só podem editar as suas próprias avaliações */
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

        /**
         * Processa edição de avaliação existente
         * 
         * @param id Identificador único da avaliação a atualizar (GUID)
         * @param IsLike Nova indicação se gosta (true) ou não gosta (false) do livro
         * @param Comment Novo comentário sobre o livro (máximo 1000 caracteres, opcional)
         * @param Rating Nova classificação de 1 a 5 estrelas (opcional)
         * @return Redirecionamento para página do livro com mensagem de sucesso ou erro
         */
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

            /* Verificar permissões para edição - controlo de acesso */
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

            /* Atualizar dados da avaliação com novos valores */
            review.IsLike = IsLike;
            review.Comment = Comment?.Trim();
            review.Rating = Rating;
            review.ReviewDate = DateTime.Now; // Atualizar timestamp de modificação

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Avaliação atualizada com sucesso!";
            return RedirectToAction("View", "Books", new { id = review.BookId });
        }

        /**
         * Remove avaliação do sistema
         * 
         * @param id Identificador único da avaliação a eliminar (GUID)
         * @return Redirecionamento para página apropriada baseado no tipo de utilizador
         */
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

                /* Verificar permissões: membros só podem eliminar as suas próprias avaliações */
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

                TempData["SuccessMessage"] = "Avaliação eliminada com sucesso!";

                /* Redirecionar baseado no tipo de utilizador - diferentes interfaces */
                if (isAdminAction)
                {
                    return RedirectToAction("Details", "Books", new { id = bookId }); // Admin -> interface administrativa
                }
                else
                {
                    return RedirectToAction("View", "Books", new { id = bookId }); // Membro -> interface pública
                }
            }

            TempData["ErrorMessage"] = "Avaliação não encontrada.";
            return RedirectToAction("Index", "Home");
        }
    }
}
