using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LibraryManagementSystem.Controllers.Api
{
    /**
     * API para gestão completa de livros no Sistema de Gestão de Biblioteca
     * Implementa operações CRUD com autenticação JWT e controlo de acesso baseado em roles
     * Suporta relacionamentos muitos-para-um e muitos-para-muitos conforme requisitos obrigatórios
     */
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public BooksController(LibraryContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /**
         * Obter lista de todos os livros disponíveis no sistema
         * 
         * @return Lista completa de livros com estatísticas de avaliações
         * @response 200 Lista de livros retornada com sucesso
         * @response 500 Erro interno do servidor
         */
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<BookDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            try
            {
                /* Consulta com relacionamentos obrigatórios - muitos-para-muitos e muitos-para-um */
                var books = await _context.Books
                    .Include(b => b.Categories) // Relacionamento muitos-para-muitos
                    .Include(b => b.BookReviews) // Relacionamento muitos-para-um
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
                /* Implementa mensagens de erro adequadas em português */
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /**
         * Obter detalhes completos de um livro específico
         * 
         * @param id Identificador único do livro (GUID)
         * @return Detalhes completos do livro incluindo avaliações e estatísticas
         * @response 200 Livro encontrado e detalhes retornados
         * @response 404 Livro não encontrado
         * @response 400 ID inválido fornecido
         */
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BookDetailDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BookDetailDto>> GetBook(Guid id)
        {
            /* Validação adequada dos dados introduzidos */
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "ID do livro inválido" });
            }

            try
            {
                /* Consulta complexa com múltiplos relacionamentos */
                var book = await _context.Books
                    .Include(b => b.Categories) // Relacionamento muitos-para-muitos
                    .Include(b => b.BookReviews) // Relacionamento muitos-para-um
                        .ThenInclude(r => r.Member)
                    .Include(b => b.Borrowings) // Relacionamento muitos-para-um
                        .ThenInclude(br => br.Member)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                /* Construção do DTO com dados completos */
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

        /**
         * Criar um novo livro no sistema
         * 
         * @param createBookDto Dados do livro a ser criado
         * @return Livro criado com informações básicas
         * @response 201 Livro criado com sucesso
         * @response 400 Dados inválidos fornecidos
         * @response 409 ISBN já existe no sistema
         * @response 401 Utilizador não autenticado
         * @response 403 Utilizador sem permissões (apenas Bibliotecários)
         */
        [HttpPost]
        [Authorize(Roles = "Bibliotecário", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(BookDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BookDto>> PostBook(CreateBookDto createBookDto)
        {
            /* Validação adequada dos dados introduzidos pelos utilizadores */
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                /* Validação de unicidade do ISBN */
                if (!string.IsNullOrEmpty(createBookDto.ISBN))
                {
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == createBookDto.ISBN);

                    if (existingBook != null)
                    {
                        return Conflict(new { message = "Já existe um livro com este ISBN" });
                    }
                }

                /* Criação da entidade Book - operação CREATE do CRUD */
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

                /* Gestão do relacionamento muitos-para-muitos com categorias */
                if (createBookDto.CategoryIds != null && createBookDto.CategoryIds.Any())
                {
                    var categories = await _context.Categories
                        .Where(c => createBookDto.CategoryIds.Contains(c.CategoryId))
                        .ToListAsync();
                    book.Categories = categories;
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                /* Preparação da resposta */
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

        /**
         * Atualizar informações de um livro existente
         * 
         * @param id Identificador único do livro a atualizar
         * @param updateBookDto Novos dados do livro
         * @return Confirmação da atualização
         * @response 204 Livro atualizado com sucesso
         * @response 400 Dados inválidos fornecidos
         * @response 404 Livro não encontrado
         * @response 409 ISBN já existe noutro livro
         */
        [HttpPut("{id}")]
        [Authorize(Roles = "Bibliotecário", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> PutBook(Guid id, UpdateBookDto updateBookDto)
        {
            /* Validação dos dados de entrada */
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                /* Busca do livro existente com relacionamentos */
                var book = await _context.Books
                    .Include(b => b.Categories)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                /* Validação de unicidade do ISBN (excluindo o livro atual) */
                if (!string.IsNullOrEmpty(updateBookDto.ISBN) && updateBookDto.ISBN != book.ISBN)
                {
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(b => b.ISBN == updateBookDto.ISBN && b.BookId != id);

                    if (existingBook != null)
                    {
                        return Conflict(new { message = "Já existe outro livro com este ISBN" });
                    }
                }

                /* Atualização das propriedades básicas - operação UPDATE do CRUD */
                book.Title = updateBookDto.Title;
                book.Author = updateBookDto.Author;
                book.ISBN = updateBookDto.ISBN;
                book.YearPublished = updateBookDto.YearPublished;
                book.Available = updateBookDto.Available;

                /* Atualização do relacionamento muitos-para-muitos com categorias */
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
                return Conflict(new { message = "Conflito de concorrência" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /**
         * Eliminar um livro do sistema
         * 
         * @param id Identificador único do livro a eliminar
         * @return Confirmação da eliminação
         * @response 200 Livro eliminado com sucesso
         * @response 400 Livro não pode ser eliminado (está emprestado)
         * @response 404 Livro não encontrado
         */
        [HttpDelete("{id}")]
        [Authorize(Roles = "Bibliotecário", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            try
            {
                /* Busca do livro com todas as dependências */
                var book = await _context.Books
                    .Include(b => b.Borrowings)
                    .Include(b => b.BookReviews)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null)
                {
                    return NotFound(new { message = "Livro não encontrado" });
                }

                /* Validação das operações de remoção/eliminação de informação */
                var activeBorrowing = book.Borrowings.FirstOrDefault(b => b.Status == "Emprestado");
                if (activeBorrowing != null)
                {
                    return BadRequest(new { message = "Não é possível eliminar o livro. Está atualmente emprestado." });
                }

                /* Remoção em cascata das dependências - operação DELETE do CRUD */
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

        /**
 * Pesquisar livros com filtros múltiplos e paginação
 * 
 * @param title Filtro por título (pesquisa parcial)
 * @param author Filtro por autor (pesquisa parcial)
 * @param category Filtro por categoria (pesquisa parcial)
 * @param available Filtro por disponibilidade (true/false)
 * @param page Número da página (padrão: 1)
 * @param pageSize Itens por página (padrão: 10, máx: 50)
 * @return Lista paginada de livros que correspondem aos critérios
 * @response 200 Pesquisa realizada com sucesso
 * @response 400 Parâmetros de pesquisa inválidos
 */
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
            /* Validação rigorosa dos parâmetros de paginação */
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
                /* Construção da consulta base com relacionamentos */
                var query = _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.BookReviews)
                    .AsQueryable();

                /* Aplicação sequencial dos filtros usando LINQ */
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

                /* Contagem total para metadados de paginação */
                var totalCount = await query.CountAsync();

                /* Calculate totalPages in the correct scope */
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                /* Aplicação da paginação e projeção */
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

                /* Preparação da resposta com metadados completos */
                var result = new
                {
                    Books = books,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
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
