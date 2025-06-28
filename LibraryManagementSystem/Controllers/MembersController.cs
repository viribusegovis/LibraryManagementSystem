using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    /**
     * Controlador para gestão de membros da biblioteca
     * Implementa operações CRUD para membros com histórico de empréstimos e avaliações.
     * Gere relacionamentos muitos-para-um com empréstimos e avaliações.
     */
    [Authorize(Roles = "Bibliotecário")]
    public class MembersController : Controller
    {
        private readonly LibraryContext _context;

        /**
         * Inicializa o controlador com contexto da base de dados
         * @param context Contexto Entity Framework para acesso aos dados
         */
        public MembersController(LibraryContext context)
        {
            _context = context;
        }

        /**
         * Lista membros com pesquisa por nome, email ou número de cartão
         * 
         * @param searchString Termo de pesquisa para filtrar membros
         * @return Vista com lista filtrada de membros e estatísticas de empréstimos
         */
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            /* Consulta base com relacionamentos para estatísticas */
            var members = from m in _context.Members
                         .Include(m => m.Borrowings) // Relacionamento muitos-para-um
                          select m;

            /* Aplicar filtro de pesquisa em múltiplos campos */
            if (!String.IsNullOrEmpty(searchString))
            {
                members = members.Where(m => m.Name.Contains(searchString)
                                          || m.Email.Contains(searchString)
                                          || m.CardNumber.Contains(searchString));
            }

            return View(await members.ToListAsync());
        }

        /**
         * Detalhes completos de um membro específico
         * 
         * @param id Identificador único do membro (GUID)
         * @return Vista com detalhes do membro, histórico de empréstimos e avaliações
         */
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            /* Carregar membro com todos os relacionamentos para vista completa */
            var member = await _context.Members
                .Include(m => m.Borrowings) // Relacionamento muitos-para-um
                    .ThenInclude(b => b.Book) // Navegação para livros emprestados
                        .ThenInclude(b => b.Categories) // Relacionamento muitos-para-muitos
                .Include(m => m.BookReviews) // Relacionamento muitos-para-um
                    .ThenInclude(br => br.Book) // Navegação para livros avaliados
                        .ThenInclude(b => b.Categories)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        /**
         * Apresenta formulário para criar novo membro
         * @return Vista de criação vazia
         */
        public IActionResult Create()
        {
            return View();
        }

        /**
         * Processa criação de novo membro com validações
         * 
         * @param member Dados do membro a criar
         * @return Redirecionamento para lista ou vista de criação em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,DateOfBirth,CardNumber")] Member member)
        {
            if (ModelState.IsValid)
            {
                /* Validar unicidade do email */
                var existingMemberByEmail = await _context.Members
                    .FirstOrDefaultAsync(m => m.Email.ToLower() == member.Email.ToLower());

                if (existingMemberByEmail != null)
                {
                    ModelState.AddModelError("Email", "Já existe um membro com este email.");
                    return View(member);
                }

                /* Validar unicidade do número de cartão */
                var existingMemberByCard = await _context.Members
                    .FirstOrDefaultAsync(m => m.CardNumber == member.CardNumber);

                if (existingMemberByCard != null)
                {
                    ModelState.AddModelError("CardNumber", "Já existe um membro com este número de cartão.");
                    return View(member);
                }

                member.MemberId = Guid.NewGuid();
                member.MembershipDate = DateTime.Now;
                member.IsActive = true;

                _context.Add(member);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Membro criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        /**
         * Apresenta formulário para editar membro existente
         * 
         * @param id Identificador único do membro a editar (GUID)
         * @return Vista de edição com dados do membro
         */
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar membro com empréstimos para mostrar impacto das alterações */
            var member = await _context.Members
                .Include(m => m.Borrowings.Where(b => b.Status == "Emprestado"))
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
                return NotFound();

            /* Verificar se membro tem empréstimos ativos */
            var activeBorrowings = member.Borrowings.Count(b => b.Status == "Emprestado");
            if (activeBorrowings > 0)
            {
                ViewBag.WarningMessage = $"Atenção: Este membro tem {activeBorrowings} empréstimo(s) ativo(s).";
            }

            return View(member);
        }

        /**
         * Processa edição de membro existente
         * 
         * @param id Identificador único do membro
         * @param member Dados atualizados do membro
         * @return Redirecionamento para lista ou vista de edição em caso de erro
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("MemberId,Name,Email,Phone,Address,DateOfBirth,CardNumber,IsActive,MembershipDate")] Member member)
        {
            if (id != member.MemberId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    /* Validar unicidade do email (excluindo membro atual) */
                    var existingMemberByEmail = await _context.Members
                        .FirstOrDefaultAsync(m => m.Email.ToLower() == member.Email.ToLower() && m.MemberId != member.MemberId);

                    if (existingMemberByEmail != null)
                    {
                        ModelState.AddModelError("Email", "Já existe outro membro com este email.");
                        return View(member);
                    }

                    /* Validar unicidade do número de cartão (excluindo membro atual) */
                    var existingMemberByCard = await _context.Members
                        .FirstOrDefaultAsync(m => m.CardNumber == member.CardNumber && m.MemberId != member.MemberId);

                    if (existingMemberByCard != null)
                    {
                        ModelState.AddModelError("CardNumber", "Já existe outro membro com este número de cartão.");
                        return View(member);
                    }

                    _context.Update(member);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Membro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MemberExists(member.MemberId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        /**
         * Apresenta confirmação para eliminar membro
         * 
         * @param id Identificador único do membro a eliminar (GUID)
         * @return Vista de confirmação com validações de integridade
         */
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            /* Carregar membro com empréstimos e avaliações para validar eliminação */
            var member = await _context.Members
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                .Include(m => m.BookReviews)
                    .ThenInclude(br => br.Book)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
                return NotFound();

            /* Verificar empréstimos ativos */
            var activeBorrowings = member.Borrowings.Count(b => b.Status == "Emprestado");
            ViewBag.HasActiveBorrowings = activeBorrowings > 0;
            ViewBag.ActiveBorrowingsCount = activeBorrowings;

            /* Contar histórico total */
            ViewBag.TotalBorrowings = member.Borrowings.Count;
            ViewBag.TotalReviews = member.BookReviews.Count;

            return View(member);
        }

        /**
         * Processa eliminação de membro com validações de integridade
         * 
         * @param id Identificador único do membro a eliminar
         * @return Redirecionamento para lista com mensagem de sucesso ou erro
         */
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var member = await _context.Members
                .Include(m => m.Borrowings)
                .Include(m => m.BookReviews)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member != null)
            {
                /* Validar se tem empréstimos ativos */
                var activeBorrowings = member.Borrowings.Where(b => b.Status == "Emprestado").ToList();
                if (activeBorrowings.Any())
                {
                    TempData["ErrorMessage"] = $"Não é possível eliminar o membro '{member.Name}'. Tem {activeBorrowings.Count} empréstimo(s) ativo(s).";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                /* Remover dependências em cascata */
                if (member.BookReviews.Any())
                {
                    _context.BookReviews.RemoveRange(member.BookReviews);
                }

                if (member.Borrowings.Any())
                {
                    _context.Borrowings.RemoveRange(member.Borrowings);
                }

                _context.Members.Remove(member);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Membro '{member.Name}' e todo o seu histórico foram eliminados com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        /**
         * Alterna estado ativo/inativo de um membro
         * 
         * @param id Identificador único do membro (GUID)
         * @return Redirecionamento para lista com mensagem de confirmação
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var member = await _context.Members
                .Include(m => m.Borrowings)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Membro não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            /* Verificar empréstimos ativos antes de desativar */
            if (member.IsActive)
            {
                var activeBorrowings = member.Borrowings.Count(b => b.Status == "Emprestado");
                if (activeBorrowings > 0)
                {
                    TempData["ErrorMessage"] = $"Não é possível desativar o membro '{member.Name}'. Tem {activeBorrowings} empréstimo(s) ativo(s).";
                    return RedirectToAction(nameof(Index));
                }
            }

            member.IsActive = !member.IsActive;
            await _context.SaveChangesAsync();

            var statusMessage = member.IsActive ? "ativado" : "desativado";
            TempData["SuccessMessage"] = $"Membro '{member.Name}' {statusMessage} com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        /**
         * Histórico detalhado de empréstimos de um membro
         * 
         * @param id Identificador único do membro (GUID)
         * @return Vista com histórico completo de empréstimos
         */
        public async Task<IActionResult> BorrowingHistory(Guid? id)
        {
            if (id == null)
                return NotFound();

            var member = await _context.Members
                .Include(m => m.Borrowings)
                    .ThenInclude(b => b.Book)
                        .ThenInclude(b => b.Categories)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
                return NotFound();

            return View(member);
        }

        /**
         * Verifica se um membro existe na base de dados
         * @param id Identificador único do membro
         * @return True se o membro existir, false caso contrário
         */
        private bool MemberExists(Guid id)
        {
            return _context.Members.Any(e => e.MemberId == id);
        }
    }
}
