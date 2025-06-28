using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador principal para interface de membros da biblioteca
     * Implementa dashboard do membro, catálogo de livros e histórico de empréstimos.
     * Gere acesso diferenciado entre bibliotecários e membros.
     */
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public HomeController(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Dashboard principal do membro com abas dinâmicas
         * 
         * @param tab Aba ativa (available, borrowed, overdue, history)
         * @return Vista do dashboard ou redirecionamento baseado no tipo de utilizador
         */
        public async Task<IActionResult> Index(string tab = "available")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            /* Redirecionar bibliotecários para interface administrativa */
            if (User.IsInRole("Bibliotecário"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Perfil de membro não encontrado.";
                return RedirectToAction("AccessDenied", "Account");
            }

            /* Carregar empréstimos com todas as avaliações de livros - relacionamentos muitos-para-um */
            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories) // Relacionamento muitos-para-muitos
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews) // Relacionamento muitos-para-um
                        .ThenInclude(br => br.Member)
                .Include(b => b.Member)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            member.Borrowings = borrowings;

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentTab = tab,
                Books = await GetAvailableBooks()
            };

            return View(viewModel);
        }

        /**
         * Carrega conteúdo de abas via AJAX para interface dinâmica
         * 
         * @param tab Nome da aba a carregar (borrowed, overdue, history)
         * @return Vista parcial com dados filtrados ou erro JSON
         */
        [HttpGet]
        public async Task<IActionResult> GetTabContent(string tab)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Utilizador não encontrado" });
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return Json(new { success = false, message = "Membro não encontrado" });
            }

            /* Consulta base com relacionamentos necessários */
            var borrowingsQuery = _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews)
                        .ThenInclude(br => br.Member)
                .Include(b => b.Member)
                .Where(b => b.MemberId == member.MemberId);

            /* Aplicar filtros específicos por aba */
            switch (tab)
            {
                case "history":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Devolvido");
                    break;
                case "borrowed":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Emprestado");
                    break;
                case "overdue":
                    borrowingsQuery = borrowingsQuery.Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now);
                    break;
            }

            var borrowings = await borrowingsQuery
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            member.Borrowings = borrowings;

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentTab = tab
            };

            /* Retornar vista parcial apropriada */
            switch (tab)
            {
                case "borrowed":
                    return PartialView("_BorrowedTab", viewModel);
                case "overdue":
                    return PartialView("_OverdueTab", viewModel);
                case "history":
                    return PartialView("_HistoryTab", viewModel);
                default:
                    return PartialView("_BorrowedTab", viewModel);
            }
        }

        /**
         * Catálogo público de livros com pesquisa
         * 
         * @param search Termo de pesquisa para título, autor ou categoria
         * @return Vista do catálogo com livros filtrados
         */
        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> Catalog(string search)
        {
            /* Consulta base com relacionamentos para estatísticas */
            var booksQuery = _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .Include(b => b.Borrowings)
                .Where(b => b.Available);

            /* Aplicar filtro de pesquisa se fornecido */
            if (!string.IsNullOrEmpty(search))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(search) ||
                    b.Author.Contains(search) ||
                    b.Categories.Any(c => c.Name.Contains(search)));
            }

            var books = await booksQuery.ToListAsync();

            /* Criar ViewModels com estatísticas calculadas */
            var bookViewModels = books.Select(b => new BookViewModel
            {
                Book = b,
                LikesCount = b.BookReviews?.Count(r => r.IsLike) ?? 0,
                DislikesCount = b.BookReviews?.Count(r => !r.IsLike) ?? 0,
                Comments = b.BookReviews?.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList() ?? new List<BookReview>()
            }).ToList();

            ViewData["CurrentFilter"] = search;
            return View(bookViewModels);
        }

        /**
         * Histórico completo de empréstimos do membro
         * @return Vista com histórico ordenado por data de empréstimo
         */
        [Authorize(Roles = "Membro")]
        public async Task<IActionResult> MyHistory()
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Index");
            }

            /* Carregar histórico completo com relacionamentos */
            var history = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return View(history);
        }

        /**
         * Página sobre o sistema
         * @return Vista estática com informações do sistema
         */
        public IActionResult About()
        {
            return View();
        }

        /**
         * Carrega livros disponíveis para o dashboard
         * @return Lista de ViewModels com livros disponíveis e estatísticas
         */
        private async Task<List<BookViewModel>> GetAvailableBooks()
        {
            var books = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .Where(b => b.Available)
                .ToListAsync();

            return books.Select(b => new BookViewModel
            {
                Book = b,
                LikesCount = b.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.BookReviews.Count(r => !r.IsLike),
                Comments = b.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        /**
         * Carrega livros atualmente emprestados pelo membro
         * 
         * @param memberId ID do membro
         * @return Lista de ViewModels com livros emprestados
         */
        private async Task<List<BookViewModel>> GetBorrowedBooks(Guid? memberId)
        {
            if (!memberId.HasValue) return new List<BookViewModel>();

            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews)
                        .ThenInclude(br => br.Member)
                .Where(b => b.MemberId == memberId && b.Status == "Emprestado")
                .ToListAsync();

            return borrowings.Select(b => new BookViewModel
            {
                Book = b.Book,
                Borrowing = b,
                LikesCount = b.Book.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.Book.BookReviews.Count(r => !r.IsLike),
                Comments = b.Book.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        /**
         * Carrega livros em atraso do membro
         * 
         * @param memberId ID do membro
         * @return Lista de ViewModels com livros em atraso
         */
        private async Task<List<BookViewModel>> GetOverdueBooks(Guid? memberId)
        {
            if (!memberId.HasValue) return new List<BookViewModel>();

            var overdueBorrowings = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book.Categories)
                .Include(b => b.Book)
                    .ThenInclude(book => book.BookReviews)
                        .ThenInclude(br => br.Member)
                .Where(b => b.MemberId == memberId &&
                           b.Status == "Emprestado" &&
                           b.DueDate < DateTime.Now)
                .ToListAsync();

            return overdueBorrowings.Select(b => new BookViewModel
            {
                Book = b.Book,
                Borrowing = b,
                LikesCount = b.Book.BookReviews.Count(r => r.IsLike),
                DislikesCount = b.Book.BookReviews.Count(r => !r.IsLike),
                Comments = b.Book.BookReviews.Where(r => !string.IsNullOrEmpty(r.Comment)).ToList()
            }).ToList();
        }

        /**
         * Processa avaliação de livro via AJAX
         * 
         * @param bookId ID do livro a avaliar
         * @param isLike Se gosta (true) ou não gosta (false)
         * @param comment Comentário opcional
         * @return Resposta JSON com resultado e estatísticas atualizadas
         */
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateBook(Guid bookId, bool isLike, string comment = "")
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return Json(new { success = false, message = "Membro não encontrado." });
            }

            /* Validar se membro já emprestou o livro */
            var hasBorrowedBook = await _context.Borrowings
                .AnyAsync(b => b.MemberId == member.MemberId && b.BookId == bookId);

            if (!hasBorrowedBook)
            {
                return Json(new
                {
                    success = false,
                    message = "Só pode avaliar livros que já requisitou ou devolveu."
                });
            }

            /* Verificar se já existe avaliação - atualizar ou criar nova */
            var existingReview = await _context.BookReviews
                .FirstOrDefaultAsync(br => br.BookId == bookId && br.MemberId == member.MemberId);

            if (existingReview != null)
            {
                existingReview.IsLike = isLike;
                existingReview.Comment = comment;
                existingReview.ReviewDate = DateTime.Now;
            }
            else
            {
                var review = new BookReview
                {
                    BookId = bookId,
                    MemberId = member.MemberId,
                    IsLike = isLike,
                    Comment = comment,
                    ReviewDate = DateTime.Now
                };
                _context.BookReviews.Add(review);
            }

            await _context.SaveChangesAsync();

            /* Calcular estatísticas atualizadas */
            var likesCount = await _context.BookReviews.CountAsync(r => r.BookId == bookId && r.IsLike);
            var dislikesCount = await _context.BookReviews.CountAsync(r => r.BookId == bookId && !r.IsLike);

            return Json(new
            {
                success = true,
                message = "Avaliação registada com sucesso!",
                likesCount = likesCount,
                dislikesCount = dislikesCount
            });
        }

        /**
         * Página de privacidade
         * @return Vista estática com política de privacidade
         */
        public IActionResult Privacy()
        {
            return View();
        }

        /**
         * Página de erro personalizada
         * @return Vista de erro sem cache
         */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
