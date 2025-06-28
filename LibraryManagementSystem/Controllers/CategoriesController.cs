using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador para gestão de categorias de livros
     * Implementa operações CRUD para categorias com validações de integridade referencial.
     * Gere relacionamento muitos-para-muitos com livros.
     */
    [Authorize(Roles = "Bibliotecário")]
    public class CategoriesController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public CategoriesController(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Lista todas as categorias com contagem de livros associados
         * @return Vista com lista de categorias e estatísticas
         */
        public async Task<IActionResult> Index()
        {
            /* Carregar categorias com livros para mostrar estatísticas */
            var categories = await _context.Categories
                .Include(c => c.Books) // Relacionamento muitos-para-muitos
                .ToListAsync();

            return View(categories);
        }

        /**
         * Detalhes de uma categoria específica com livros associados
         * 
         * @param id Identificador único da categoria (GUID)
         * @return Vista com detalhes da categoria e livros
         */
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar categoria com livros e empréstimos para análise completa */
            var category = await _context.Categories
                .Include(c => c.Books) // Relacionamento muitos-para-muitos
                    .ThenInclude(b => b.Borrowings) // Relacionamento muitos-para-um
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        /**
         * Apresenta formulário para criar nova categoria
         * @return Vista de criação vazia
         */
        public IActionResult Create()
        {
            return View();
        }

        /**
         * Processa criação de nova categoria com validação de unicidade
         * 
         * @param category Dados da categoria a criar
         * @return Redirecionamento para lista ou vista de criação em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                /* Validar unicidade do nome da categoria */
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory != null)
                {
                    ModelState.AddModelError("Name", "Já existe uma categoria com este nome.");
                    return View(category);
                }

                category.CategoryId = Guid.NewGuid();
                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoria criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        /**
         * Apresenta formulário para editar categoria existente
         * 
         * @param id Identificador único da categoria a editar (GUID)
         * @return Vista de edição com dados da categoria
         */
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar categoria com livros para mostrar impacto das alterações */
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        /**
         * Processa edição de categoria existente
         * 
         * @param id Identificador único da categoria
         * @param category Dados atualizados da categoria
         * @return Redirecionamento para lista ou vista de edição em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CategoryId,Name,Description")] Category category)
        {
            if (id != category.CategoryId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    /* Validar unicidade do nome (excluindo categoria atual) */
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.CategoryId != category.CategoryId);

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "Já existe uma categoria com este nome.");

                        /* Recarregar livros para a vista em caso de erro */
                        var categoryWithBooks = await _context.Categories
                            .Include(c => c.Books)
                            .FirstOrDefaultAsync(c => c.CategoryId == id);
                        category.Books = categoryWithBooks?.Books ?? new List<Book>();

                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Categoria atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        /**
         * Apresenta confirmação para eliminar categoria
         * 
         * @param id Identificador único da categoria a eliminar (GUID)
         * @return Vista de confirmação com validações de integridade
         */
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar categoria com livros e empréstimos para validar eliminação */
            var category = await _context.Categories
                .Include(c => c.Books)
                    .ThenInclude(b => b.Borrowings)
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null)
                return NotFound();

            /* Verificar se existem empréstimos ativos de livros desta categoria */
            var activeBorrowings = category.Books.SelectMany(b => b.Borrowings)
                .Count(br => br.Status == "Emprestado");

            ViewBag.HasActiveBorrowings = activeBorrowings > 0;
            ViewBag.ActiveBorrowingsCount = activeBorrowings;

            return View(category);
        }

        /**
         * Processa eliminação de categoria com validações de integridade
         * 
         * @param id Identificador único da categoria a eliminar
         * @return Redirecionamento para lista com mensagem de sucesso ou erro
         */
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category != null)
            {
                /* Validar integridade referencial - não eliminar se tem livros associados */
                if (category.Books.Any())
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar a categoria '{category.Name}'. Existem {category.Books.Count} livro(s) associado(s).";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Categoria '{category.Name}' eliminada com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        /**
         * Verifica se uma categoria existe na base de dados
         * @param id Identificador único da categoria
         * @return True se a categoria existir, false caso contrário
         */
        private bool CategoryExists(Guid id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
