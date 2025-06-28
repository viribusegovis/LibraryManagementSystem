using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador para gestão de empréstimos de livros
     * Implementa operações CRUD para empréstimos com validações de regras de negócio.
     * Controla disponibilidade de livros e estado dos membros.
     */
    [Authorize(Roles = "Bibliotecário")]
    public class BorrowingsController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public BorrowingsController(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Lista empréstimos com filtros por estado
         * 
         * @param status Filtro de estado (all, active, overdue, returned)
         * @return Vista com lista filtrada de empréstimos
         */
        public async Task<IActionResult> Index(string status = "all")
        {
            ViewData["CurrentStatus"] = status;

            /* Consulta base com relacionamentos necessários */
            var borrowings = _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(b => b.Categories) // Relacionamento muitos-para-muitos
                .Include(b => b.Member)
                .AsQueryable();

            /* Aplicar filtros baseados no estado */
            switch (status)
            {
                case "active":
                    borrowings = borrowings.Where(b => b.Status == "Emprestado");
                    break;
                case "overdue":
                    borrowings = borrowings.Where(b => b.Status == "Emprestado" && b.DueDate < DateTime.Now);
                    break;
                case "returned":
                    borrowings = borrowings.Where(b => b.Status == "Devolvido");
                    break;
            }

            return View(await borrowings.OrderByDescending(b => b.BorrowDate).ToListAsync());
        }

        /**
         * Detalhes de um empréstimo específico
         * 
         * @param id Identificador único do empréstimo (GUID)
         * @return Vista com detalhes completos do empréstimo
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(b => b.Categories)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }

        /**
         * Apresenta formulário para criar novo empréstimo
         * 
         * @param bookId ID do livro pré-selecionado (opcional)
         * @param memberId ID do membro pré-selecionado (opcional)
         * @return Vista de criação com listas de livros e membros disponíveis
         */
        public async Task<IActionResult> Create(Guid? bookId, Guid? memberId)
        {
            /* Carregar livros disponíveis para empréstimo */
            var availableBooks = await _context.Books
                .Include(b => b.Categories)
                .Where(b => b.Available)
                .OrderBy(b => b.Title)
                .ToListAsync();

            /* Carregar membros ativos */
            var activeMembers = await _context.Members
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            /* Criar listas para dropdowns */
            ViewBag.Books = new SelectList(availableBooks, "BookId", "Title", bookId);
            ViewBag.Members = new SelectList(activeMembers, "MemberId", "Name", memberId);

            /* Carregar detalhes se livro ou membro pré-selecionado */
            if (bookId.HasValue)
            {
                var selectedBook = availableBooks.FirstOrDefault(b => b.BookId == bookId);
                ViewBag.SelectedBook = selectedBook;
            }

            if (memberId.HasValue)
            {
                var selectedMember = activeMembers.FirstOrDefault(m => m.MemberId == memberId);
                ViewBag.SelectedMember = selectedMember;
            }

            /* Criar empréstimo com data limite padrão (14 dias) */
            var borrowing = new Borrowing
            {
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                BookId = bookId ?? Guid.Empty,
                MemberId = memberId ?? Guid.Empty
            };

            return View(borrowing);
        }

        /**
         * Processa criação de novo empréstimo com validações completas
         * 
         * @param borrowing Dados do empréstimo a criar
         * @return Redirecionamento para lista ou vista de criação em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,MemberId,DueDate")] Borrowing borrowing)
        {
            /* Remover validações de navegação que são preenchidas automaticamente */
            ModelState.Remove("Book");
            ModelState.Remove("Member");

            if (ModelState.IsValid)
            {
                /* Validar disponibilidade do livro */
                var book = await _context.Books.FindAsync(borrowing.BookId);
                if (book == null || !book.Available)
                {
                    ModelState.AddModelError("BookId", "Este livro não está disponível para empréstimo.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                /* Validar estado do membro */
                var member = await _context.Members.FindAsync(borrowing.MemberId);
                if (member == null || !member.IsActive)
                {
                    ModelState.AddModelError("MemberId", "Membro não encontrado ou inativo.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                /* Verificar se membro tem livros em atraso */
                var overdueBooks = await _context.Borrowings
                    .Where(b => b.MemberId == borrowing.MemberId &&
                               b.Status == "Emprestado" &&
                               b.DueDate < DateTime.Now)
                    .CountAsync();

                if (overdueBooks > 0)
                {
                    ModelState.AddModelError("MemberId", $"Este membro tem {overdueBooks} livro(s) em atraso. Não é possível fazer novos empréstimos.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                /* Validar data limite */
                if (borrowing.DueDate <= DateTime.Now)
                {
                    ModelState.AddModelError("DueDate", "A data limite deve ser posterior à data atual.");
                    await LoadCreateViewData(borrowing);
                    return View(borrowing);
                }

                /* Criar registo de empréstimo */
                borrowing.BorrowingId = Guid.NewGuid();
                borrowing.BorrowDate = DateTime.Now;
                borrowing.Status = "Emprestado";

                /* Marcar livro como indisponível (lógica de cópia única) */
                book.Available = false;

                _context.Add(borrowing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Empréstimo criado com sucesso! Livro '{book.Title}' emprestado a {member.Name}.";
                return RedirectToAction("Index");
            }

            await LoadCreateViewData(borrowing);
            return View(borrowing);
        }

        /**
         * Carrega dados necessários para a vista de criação
         * @param borrowing Empréstimo com dados pré-selecionados
         */
        private async Task LoadCreateViewData(Borrowing borrowing)
        {
            var availableBooks = await _context.Books
                .Include(b => b.Categories)
                .Where(b => b.Available)
                .OrderBy(b => b.Title)
                .ToListAsync();

            var activeMembers = await _context.Members
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Books = new SelectList(availableBooks, "BookId", "Title", borrowing.BookId);
            ViewBag.Members = new SelectList(activeMembers, "MemberId", "Name", borrowing.MemberId);
        }

        /**
         * Processa devolução de livro emprestado
         * 
         * @param id Identificador único do empréstimo a devolver
         * @return Redirecionamento para lista com mensagem de confirmação
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(Guid id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            if (borrowing.Status != "Emprestado")
            {
                TempData["ErrorMessage"] = "Este empréstimo já foi devolvido.";
                return RedirectToAction(nameof(Index));
            }

            /* Marcar como devolvido */
            borrowing.ReturnDate = DateTime.Now;
            borrowing.Status = "Devolvido";

            /* Tornar livro disponível novamente (lógica de cópia única) */
            borrowing.Book.Available = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Livro devolvido com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}
