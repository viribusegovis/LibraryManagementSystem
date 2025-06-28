using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador administrativo para gestão do dashboard da biblioteca
     * Desenvolvimento Web - Licenciatura em Engenharia Informática - IPT
     * 
     * Implementa dashboard com estatísticas do sistema, empréstimos e análise de dados.
     * Acesso restrito a utilizadores com role "Bibliotecário".
     */
    [Authorize(Roles = "Bibliotecário")]
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public AdminController(LibraryContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /**
         * Dashboard principal com estatísticas completas do sistema
         * Implementa operações READ do CRUD obrigatório com consultas LINQ complexas
         * 
         * @return Vista com dados estatísticos da biblioteca
         */
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = new AdminDashboardViewModel
                {
                    /* Contadores básicos do sistema */
                    TotalBooks = await _context.Books.CountAsync(),
                    AvailableBooks = await _context.Books.CountAsync(b => b.Available),
                    TotalMembers = await _context.Members.CountAsync(),
                    ActiveMembers = await _context.Members.CountAsync(m => m.IsActive),
                    TotalCategories = await _context.Categories.CountAsync(),

                    /* Métricas de empréstimos */
                    ActiveBorrowings = await _context.Borrowings
                        .CountAsync(b => b.Status == "Emprestado"),
                    OverdueBooks = await _context.Borrowings
                        .CountAsync(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now),
                    BooksReturnedToday = await _context.Borrowings
                        .CountAsync(b => b.ReturnDate.HasValue && b.ReturnDate.Value.Date == DateTime.Today),

                    /* Listas para análise detalhada - carrega relacionamentos muitos-para-um */
                    RecentBorrowings = await _context.Borrowings
                        .Include(b => b.Book)    // Relacionamento Borrowing -> Book
                        .Include(b => b.Member)  // Relacionamento Borrowing -> Member
                        .OrderByDescending(b => b.BorrowDate)
                        .Take(5)
                        .ToListAsync(),

                    OverdueList = await _context.Borrowings
                        .Include(b => b.Book)
                        .Include(b => b.Member)
                        .Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now)
                        .OrderBy(b => b.DueDate)  // Mais urgentes primeiro
                        .Take(10)
                        .ToListAsync(),

                    /* Análise de popularidade - consulta LINQ com GroupBy */
                    PopularBooks = await _context.Borrowings
                        .Include(b => b.Book)
                        .GroupBy(b => b.Book)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => new PopularBookViewModel
                        {
                            Book = g.Key,
                            BorrowCount = g.Count()
                        })
                        .ToListAsync()
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                /* Tratamento de erros conforme requisitos */
                ViewBag.ErrorMessage = "Erro ao carregar dados do dashboard";
                ViewBag.ErrorDetails = ex.Message;
                return View("Error");
            }
        }
    }
}
