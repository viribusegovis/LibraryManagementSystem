using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador principal para gestão de livros da biblioteca
     * Implementa operações CRUD completas com diferentes interfaces para administradores e membros.
     * Gere relacionamentos muitos-para-muitos com categorias e controla acesso baseado em roles.
     */
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Lista administrativa de livros com filtros e ordenação
         * 
         * @param searchString Termo de pesquisa para título, autor ou ISBN
         * @param categoryId ID da categoria para filtrar livros
         * @param availability Estado de disponibilidade (available, borrowed, unavailable)
         * @param sortOrder Critério de ordenação (title, author, etc.)
         * @return Vista administrativa com lista filtrada de livros
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Index(string searchString, Guid? categoryId, string availability, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "Author" ? "author_desc" : "Author";

            /* Consulta base com relacionamentos necessários */
            var books = from b in _context.Books
                        .Include(b => b.Categories)
                        .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                            .ThenInclude(br => br.Member)
                        select b;

            /* Aplicar filtros de pesquisa */
            if (!String.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString)
                                      || b.Author.Contains(searchString)
                                      || b.ISBN.Contains(searchString));
            }

            /* Filtro por categoria */
            if (categoryId.HasValue)
            {
                books = books.Where(b => b.Categories.Any(c => c.CategoryId == categoryId));
            }

            /* Filtro por estado de disponibilidade */
            if (!String.IsNullOrEmpty(availability))
            {
                switch (availability.ToLower())
                {
                    case "available":
                        books = books.Where(b => b.Available && !b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "borrowed":
                        books = books.Where(b => b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                    case "unavailable":
                        books = books.Where(b => !b.Available && !b.Borrowings.Any(br => br.Status == "Emprestado"));
                        break;
                }
            }

            /* Aplicar ordenação */
            switch (sortOrder)
            {
                case "title_desc":
                    books = books.OrderByDescending(b => b.Title);
                    break;
                case "Author":
                    books = books.OrderBy(b => b.Author);
                    break;
                case "author_desc":
                    books = books.OrderByDescending(b => b.Author);
                    break;
                default:
                    books = books.OrderBy(b => b.Title);
                    break;
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedAvailability = availability;

            return View(await books.ToListAsync());
        }

        /**
         * Detalhes administrativos de um livro específico
         * 
         * @param id Identificador único do livro (GUID)
         * @return Vista com detalhes completos do livro para administradores
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar livro com todos os relacionamentos para vista administrativa */
            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        /**
         * Vista pública de livro para membros
         * 
         * @param id Identificador único do livro (GUID)
         * @return Vista pública com detalhes do livro e opções de avaliação
         */
        [AllowAnonymous]
        public async Task<IActionResult> View(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            /* Verificar estado de empréstimo e avaliação para membros autenticados */
            if (User.Identity.IsAuthenticated && User.IsInRole("Membro"))
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

                if (member != null)
                {
                    ViewBag.CurrentMemberId = member.MemberId.ToString();
                    ViewBag.HasBorrowedBook = await _context.Borrowings
                        .AnyAsync(b => b.BookId == id && b.MemberId == member.MemberId);
                    ViewBag.HasReviewed = await _context.BookReviews
                        .AnyAsync(r => r.BookId == id && r.MemberId == member.MemberId);
                }
                else
                {
                    ViewBag.CurrentMemberId = "";
                    ViewBag.HasBorrowedBook = false;
                    ViewBag.HasReviewed = false;
                }
            }
            else
            {
                ViewBag.CurrentMemberId = "";
                ViewBag.HasBorrowedBook = false;
                ViewBag.HasReviewed = false;
            }

            return View("MemberDetails", book);
        }

        /**
         * Apresenta formulário para criar novo livro
         * @return Vista de criação com lista de categorias disponíveis
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            return View();
        }

        /**
         * Processa criação de novo livro
         * 
         * @param book Dados do livro a criar
         * @param selectedCategories Array de IDs das categorias selecionadas
         * @return Redirecionamento para lista ou vista de criação em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Create([Bind("Title,Author,ISBN,YearPublished,Available")] Book book, Guid[] selectedCategories)
        {
            if (ModelState.IsValid)
            {
                /* Validar unicidade do ISBN */
                var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == book.ISBN);
                if (existingBook != null)
                {
                    ModelState.AddModelError("ISBN", "Já existe um livro com este ISBN.");
                    ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                    return View(book);
                }

                book.BookId = Guid.NewGuid();
                book.CreatedDate = DateTime.Now;

                /* Associar categorias selecionadas - relacionamento muitos-para-muitos */
                if (selectedCategories != null && selectedCategories.Length > 0)
                {
                    var categories = await _context.Categories
                        .Where(c => selectedCategories.Contains(c.CategoryId))
                        .ToListAsync();
                    book.Categories = categories;
                }

                _context.Add(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Livro criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
            return View(book);
        }

        /**
         * Apresenta formulário para editar livro existente
         * 
         * @param id Identificador único do livro a editar (GUID)
         * @return Vista de edição com dados do livro e categorias
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings.Where(br => br.Status == "Emprestado"))
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            /* Verificar se livro está emprestado - aviso para administrador */
            var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
            if (activeBorrowing != null)
            {
                ViewBag.WarningMessage = $"Atenção: Este livro está atualmente emprestado a {activeBorrowing.Member?.Name ?? "um membro"}.";
            }

            ViewBag.Categories = new MultiSelectList(
                await _context.Categories.ToListAsync(),
                "CategoryId",
                "Name",
                book.Categories.Select(c => c.CategoryId));

            return View(book);
        }

        /**
         * Processa edição de livro existente
         * 
         * @param id Identificador único do livro
         * @param book Dados atualizados do livro
         * @param selectedCategories Array de IDs das categorias selecionadas
         * @return Redirecionamento para lista ou vista de edição em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Edit(Guid id, [Bind("BookId,Title,Author,ISBN,YearPublished,Available,CreatedDate")] Book book, Guid[] selectedCategories)
        {
            if (id != book.BookId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    /* Verificar duplicação de ISBN */
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == book.ISBN && b.BookId != book.BookId);

                    if (existingBook != null)
                    {
                        ModelState.AddModelError("ISBN", "Já existe outro livro com este ISBN.");
                        ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
                        return View(book);
                    }

                    /* Carregar livro existente com categorias para atualização */
                    var existingBookWithCategories = await _context.Books
                        .Include(b => b.Categories)
                        .FirstOrDefaultAsync(b => b.BookId == id);

                    if (existingBookWithCategories != null)
                    {
                        /* Atualizar propriedades básicas */
                        existingBookWithCategories.Title = book.Title;
                        existingBookWithCategories.Author = book.Author;
                        existingBookWithCategories.ISBN = book.ISBN;
                        existingBookWithCategories.YearPublished = book.YearPublished;
                        existingBookWithCategories.Available = book.Available;

                        /* Atualizar relacionamento muitos-para-muitos com categorias */
                        existingBookWithCategories.Categories.Clear();
                        if (selectedCategories != null && selectedCategories.Length > 0)
                        {
                            var categories = await _context.Categories
                                .Where(c => selectedCategories.Contains(c.CategoryId))
                                .ToListAsync();

                            foreach (var category in categories)
                            {
                                existingBookWithCategories.Categories.Add(category);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Livro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new MultiSelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", selectedCategories);
            return View(book);
        }

        /**
         * Apresenta confirmação para eliminar livro
         * 
         * @param id Identificador único do livro a eliminar (GUID)
         * @return Vista de confirmação com detalhes do livro
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .Include(b => b.BookReviews)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        /**
         * Processa eliminação de livro com validações de integridade
         * 
         * @param id Identificador único do livro a eliminar
         * @return Redirecionamento para lista com mensagem de sucesso ou erro
         */
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var book = await _context.Books
                .Include(b => b.Borrowings)
                .Include(b => b.BookReviews)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book != null)
            {
                /* Verificar se livro tem empréstimos ativos */
                var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
                if (activeBorrowing != null)
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar o livro '{book.Title}'. Está atualmente emprestado.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                /* Remover dependências em cascata */
                if (book.BookReviews.Any())
                {
                    _context.BookReviews.RemoveRange(book.BookReviews);
                }

                if (book.Borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(book.Borrowings);
                }

                /* Limpar relacionamentos muitos-para-muitos */
                book.Categories.Clear();

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Livro '{book.Title}' e todas as suas dependências foram eliminados com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        /**
         * Apresenta histórico de empréstimos de um livro
         * 
         * @param id Identificador único do livro (GUID)
         * @return Vista com histórico completo de empréstimos
         */
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> BorrowingHistory(Guid? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
                .Include(b => b.Categories)
                .Include(b => b.Borrowings)
                    .ThenInclude(br => br.Member)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        /**
         * Alterna estado de disponibilidade de um livro
         * 
         * @param id Identificador único do livro (GUID)
         * @return Redirecionamento para lista com mensagem de confirmação
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Bibliotecário")]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "ID do livro inválido.";
                return RedirectToAction(nameof(Index));
            }

            var book = await _context.Books
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                TempData["ErrorMessage"] = "Livro não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            /* Verificar se livro está emprestado antes de alterar disponibilidade */
            var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
            if (activeBorrowing != null)
            {
                TempData["ErrorMessage"] = "Não é possível alterar a disponibilidade. O livro está atualmente emprestado.";
                return RedirectToAction(nameof(Index));
            }

            book.Available = !book.Available;
            await _context.SaveChangesAsync();

            var statusMessage = book.Available ? "disponível" : "indisponível";
            TempData["SuccessMessage"] = $"Livro '{book.Title}' marcado como {statusMessage} com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        /**
         * Verifica se um livro existe na base de dados
         * @param id Identificador único do livro
         * @return True se o livro existir, false caso contrário
         */
        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
