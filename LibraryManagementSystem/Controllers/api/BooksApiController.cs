using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.DTOs;

namespace LibraryManagementSystem.Controllers.Api
{
    /// <summary>
    /// API para gestão de livros do Sistema de Gestão de Biblioteca
    /// Desenvolvimento Web - Licenciatura em Engenharia Informática - IPT
    /// </summary>
    /// <remarks>
    /// Esta API permite realizar operações CRUD completas sobre livros,
    /// incluindo gestão de categorias, avaliações e estatísticas.
    /// Implementa autenticação baseada em roles para operações administrativas.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;

        /// <summary>
        /// Construtor do controlador de livros
        /// </summary>
        /// <param name="context">Contexto da base de dados Entity Framework</param>
        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obter lista de todos os livros disponíveis no sistema
        /// </summary>
        /// <returns>Lista completa de livros com estatísticas de avaliações</returns>
        /// <response code="200">Lista de livros retornada com sucesso</response>
        /// <response code="500">Erro interno do servidor</response>
        /// <remarks>
        /// Este endpoint não requer autenticação e retorna todos os livros
        /// com informações básicas incluindo:
        /// - Dados do livro (título, autor, ISBN, ano)
        /// - Categorias associadas
        /// - Estatísticas de avaliações (gostos, não gostos, classificação média)
        /// - Número total de avaliações
        /// 
        /// Exemplo de uso:
        /// GET /api/books
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<BookDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            try
            {
                var books = await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.BookReviews)
                    .Select(b => new BookDto
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        ISBN = b.ISBN,
                        YearPublished = b.YearPublished,
                        Available = b.Available,
                        Categories = b.Categories.Select(c => c.Name).ToList(),
                        ReviewCount = b.BookReviews.Count,
                        AverageRating = b.BookReviews.Where(r => r.Rating.HasValue)
                            .Average(r => r.Rating) ?? 0,
                        LikesCount = b.BookReviews.Count(r => r.IsLike),
                        DislikesCount = b.BookReviews.Count(r => !r.IsLike)
                    })
                    .ToListAsync();

                return Ok(books);
            }
            catch (Exception ex)
            {
                // Log do erro (implementar logging conforme necessário)
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obter detalhes completos de um livro específico
        /// </summary>
        /// <param name="id">Identificador único do livro (GUID)</param>
        /// <returns>Detalhes completos do livro incluindo avaliações e estatísticas</returns>
        /// <response code="200">Livro encontrado e detalhes retornados</response>
        /// <response code="404">Livro não encontrado</response>
        /// <response code="400">ID inválido fornecido</response>
        /// <remarks>
        /// Este endpoint retorna informações detalhadas sobre um livro específico:
        /// - Informações básicas do livro
        /// - Lista completa de categorias
        /// - Todas as avaliações com detalhes dos membros
        /// - Estatísticas completas (empréstimos, avaliações, etc.)
        /// 
        /// Exemplo de uso:
        /// GET /api/books/123e4567-e89b-12d3-a456-426614174000
        /// </remarks>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BookDetailDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BookDetailDto>> GetBook(Guid id)
        {
            // Validação do ID
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "ID do livro inválido" });
            }

            try
            {
                var book = await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.BookReviews)
                        .ThenInclude(r => r.Member)
                    .Include(b => b.Borrowings)
                        .ThenInclude(br => br.Member)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                var bookDto = new BookDetailDto
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Author = book.Author,
                    ISBN = book.ISBN,
                    YearPublished = book.YearPublished,
                    Available = book.Available,
                    CreatedDate = book.CreatedDate,
                    Categories = book.Categories.Select(c => new CategoryDto
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name,
                        Description = c.Description
                    }).ToList(),
                    Reviews = book.BookReviews.Select(r => new ReviewDto
                    {
                        ReviewId = r.BookReviewId,
                        MemberName = r.Member.Name,
                        IsLike = r.IsLike,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        ReviewDate = r.ReviewDate
                    }).ToList(),
                    Statistics = new BookStatisticsDto
                    {
                        TotalReviews = book.BookReviews.Count,
                        AverageRating = book.BookReviews.Where(r => r.Rating.HasValue)
                            .Average(r => r.Rating) ?? 0,
                        LikesCount = book.BookReviews.Count(r => r.IsLike),
                        DislikesCount = book.BookReviews.Count(r => !r.IsLike),
                        TotalBorrowings = book.Borrowings.Count,
                        CurrentlyBorrowed = book.Borrowings.Any(b => b.Status == "Emprestado")
                    }
                };

                return Ok(bookDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Criar um novo livro no sistema
        /// </summary>
        /// <param name="createBookDto">Dados do livro a ser criado</param>
        /// <returns>Livro criado com informações básicas</returns>
        /// <response code="201">Livro criado com sucesso</response>
        /// <response code="400">Dados inválidos fornecidos</response>
        /// <response code="409">ISBN já existe no sistema</response>
        /// <response code="401">Utilizador não autenticado</response>
        /// <response code="403">Utilizador sem permissões (apenas Bibliotecários)</response>
        /// <remarks>
        /// Este endpoint permite criar novos livros no sistema.
        /// Requer autenticação e role de "Bibliotecário".
        /// 
        /// Validações implementadas:
        /// - Título obrigatório (máx. 200 caracteres)
        /// - Autor obrigatório (máx. 100 caracteres)
        /// - ISBN único no sistema (máx. 13 caracteres)
        /// - Ano de publicação entre 1000 e 2030
        /// 
        /// Exemplo de payload:
        /// {
        ///   "title": "O Senhor dos Anéis",
        ///   "author": "J.R.R. Tolkien",
        ///   "isbn": "9780007525546",
        ///   "yearPublished": 1954,
        ///   "available": true,
        ///   "categoryIds": ["123e4567-e89b-12d3-a456-426614174000"]
        /// }
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Bibliotecário")]
        [ProducesResponseType(typeof(BookDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BookDto>> PostBook(CreateBookDto createBookDto)
        {
            // Validação do modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Verificação de ISBN duplicado
                if (!string.IsNullOrEmpty(createBookDto.ISBN))
                {
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == createBookDto.ISBN);

                    if (existingBook != null)
                    {
                        return Conflict(new { message = "Já existe um livro com este ISBN" });
                    }
                }

                // Criação do novo livro
                var book = new Book
                {
                    BookId = Guid.NewGuid(),
                    Title = createBookDto.Title,
                    Author = createBookDto.Author,
                    ISBN = createBookDto.ISBN,
                    YearPublished = createBookDto.YearPublished,
                    Available = createBookDto.Available,
                    CreatedDate = DateTime.Now
                };

                // Associação de categorias (relacionamento muitos-para-muitos)
                if (createBookDto.CategoryIds != null && createBookDto.CategoryIds.Any())
                {
                    var categories = await _context.Categories
                        .Where(c => createBookDto.CategoryIds.Contains(c.CategoryId))
                        .ToListAsync();
                    book.Categories = categories;
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                // Preparação da resposta
                var bookDto = new BookDto
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Author = book.Author,
                    ISBN = book.ISBN,
                    YearPublished = book.YearPublished,
                    Available = book.Available,
                    Categories = book.Categories?.Select(c => c.Name).ToList() ?? new List<string>(),
                    ReviewCount = 0,
                    AverageRating = 0,
                    LikesCount = 0,
                    DislikesCount = 0
                };

                return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, bookDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Atualizar informações de um livro existente
        /// </summary>
        /// <param name="id">Identificador único do livro a atualizar</param>
        /// <param name="updateBookDto">Novos dados do livro</param>
        /// <returns>Confirmação da atualização</returns>
        /// <response code="204">Livro atualizado com sucesso</response>
        /// <response code="400">Dados inválidos fornecidos</response>
        /// <response code="404">Livro não encontrado</response>
        /// <response code="409">ISBN já existe noutro livro</response>
        /// <response code="401">Utilizador não autenticado</response>
        /// <response code="403">Utilizador sem permissões (apenas Bibliotecários)</response>
        /// <remarks>
        /// Este endpoint permite atualizar todas as informações de um livro.
        /// Requer autenticação e role de "Bibliotecário".
        /// 
        /// Funcionalidades:
        /// - Atualização de dados básicos do livro
        /// - Gestão de categorias (relacionamento muitos-para-muitos)
        /// - Validação de ISBN único
        /// - Controlo de concorrência
        /// 
        /// Exemplo de uso:
        /// PUT /api/books/123e4567-e89b-12d3-a456-426614174000
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "Bibliotecário")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PutBook(Guid id, UpdateBookDto updateBookDto)
        {
            // Validação do modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Busca do livro existente
                var book = await _context.Books
                    .Include(b => b.Categories)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                // Verificação de ISBN duplicado (excluindo o livro atual)
                if (!string.IsNullOrEmpty(updateBookDto.ISBN) && updateBookDto.ISBN != book.ISBN)
                {
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == updateBookDto.ISBN && b.BookId != id);

                    if (existingBook != null)
                    {
                        return Conflict(new { message = "Já existe outro livro com este ISBN" });
                    }
                }

                // Atualização das propriedades do livro
                book.Title = updateBookDto.Title;
                book.Author = updateBookDto.Author;
                book.ISBN = updateBookDto.ISBN;
                book.YearPublished = updateBookDto.YearPublished;
                book.Available = updateBookDto.Available;

                // Atualização das categorias (relacionamento muitos-para-muitos)
                book.Categories.Clear();
                if (updateBookDto.CategoryIds != null && updateBookDto.CategoryIds.Any())
                {
                    var categories = await _context.Categories
                        .Where(c => updateBookDto.CategoryIds.Contains(c.CategoryId))
                        .ToListAsync();
                    book.Categories = categories;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(id))
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }
                return Conflict(new { message = "Conflito de concorrência. O livro foi modificado por outro utilizador." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar um livro do sistema
        /// </summary>
        /// <param name="id">Identificador único do livro a eliminar</param>
        /// <returns>Confirmação da eliminação</returns>
        /// <response code="200">Livro eliminado com sucesso</response>
        /// <response code="400">Livro não pode ser eliminado (está emprestado)</response>
        /// <response code="404">Livro não encontrado</response>
        /// <response code="401">Utilizador não autenticado</response>
        /// <response code="403">Utilizador sem permissões (apenas Bibliotecários)</response>
        /// <remarks>
        /// Este endpoint elimina um livro e todas as suas dependências.
        /// Requer autenticação e role de "Bibliotecário".
        /// 
        /// Regras de negócio:
        /// - Não permite eliminar livros atualmente emprestados
        /// - Remove automaticamente avaliações associadas
        /// - Remove histórico de empréstimos (apenas se devolvidos)
        /// - Operação em cascata para manter integridade referencial
        /// 
        /// Exemplo de uso:
        /// DELETE /api/books/123e4567-e89b-12d3-a456-426614174000
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Bibliotecário")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            try
            {
                var book = await _context.Books
                    .Include(b => b.Borrowings)
                    .Include(b => b.BookReviews)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                // Verificação de empréstimos ativos
                var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
                if (activeBorrowing != null)
                {
                    return BadRequest(new { message = "Não é possível eliminar o livro. Está atualmente emprestado." });
                }

                // Remoção em cascata das dependências
                if (book.BookReviews.Any())
                {
                    _context.BookReviews.RemoveRange(book.BookReviews);
                }

                if (book.Borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(book.Borrowings);
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Livro eliminado com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Pesquisar livros com filtros e paginação
        /// </summary>
        /// <param name="title">Filtro por título (pesquisa parcial)</param>
        /// <param name="author">Filtro por autor (pesquisa parcial)</param>
        /// <param name="category">Filtro por categoria (pesquisa parcial)</param>
        /// <param name="available">Filtro por disponibilidade (true/false)</param>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="pageSize">Itens por página (padrão: 10, máx: 50)</param>
        /// <returns>Lista paginada de livros que correspondem aos critérios</returns>
        /// <response code="200">Pesquisa realizada com sucesso</response>
        /// <response code="400">Parâmetros de pesquisa inválidos</response>
        /// <remarks>
        /// Este endpoint permite pesquisar livros com múltiplos filtros e paginação.
        /// Não requer autenticação.
        /// 
        /// Funcionalidades:
        /// - Pesquisa por título, autor ou categoria (case-insensitive, parcial)
        /// - Filtro por disponibilidade
        /// - Paginação com informações de navegação
        /// - Ordenação por título (padrão)
        /// 
        /// Exemplos de uso:
        /// GET /api/books/search?title=senhor&amp;page=1&amp;pageSize=5
        /// GET /api/books/search?author=tolkien&amp;available=true
        /// GET /api/books/search?category=fantasia&amp;page=2
        /// 
        /// Resposta inclui:
        /// - Lista de livros
        /// - Total de registos
        /// - Informações de paginação
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks(
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? category,
            [FromQuery] bool? available,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validação dos parâmetros de paginação
            if (page < 1)
            {
                return BadRequest(new { message = "O número da página deve ser maior que 0" });
            }

            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new { message = "O tamanho da página deve estar entre 1 e 50" });
            }

            try
            {
                var query = _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.BookReviews)
                    .AsQueryable();

                // Aplicação dos filtros de pesquisa
                if (!string.IsNullOrEmpty(title))
                {
                    query = query.Where(b => b.Title.Contains(title));
                }

                if (!string.IsNullOrEmpty(author))
                {
                    query = query.Where(b => b.Author.Contains(author));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(b => b.Categories.Any(c => c.Name.Contains(category)));
                }

                if (available.HasValue)
                {
                    query = query.Where(b => b.Available == available.Value);
                }

                // Contagem total para paginação
                var totalCount = await query.CountAsync();

                // Aplicação da paginação e projeção
                var books = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new BookDto
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        ISBN = b.ISBN,
                        YearPublished = b.YearPublished,
                        Available = b.Available,
                        Categories = b.Categories.Select(c => c.Name).ToList(),
                        ReviewCount = b.BookReviews.Count,
                        AverageRating = b.BookReviews.Where(r => r.Rating.HasValue)
                            .Average(r => r.Rating) ?? 0,
                        LikesCount = b.BookReviews.Count(r => r.IsLike),
                        DislikesCount = b.BookReviews.Count(r => !r.IsLike)
                    })
                    .ToListAsync();

                // Preparação da resposta com metadados de paginação
                var result = new
                {
                    Books = books,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page < Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Método auxiliar para verificar se um livro existe
        /// </summary>
        /// <param name="id">Identificador do livro</param>
        /// <returns>True se o livro existir, False caso contrário</returns>
        private bool BookExists(Guid id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
