using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    /**
     * ViewModel para o dashboard do membro da biblioteca
     * Agrega dados do membro e livros para interface de utilizador
     */
    public class MemberDashboardViewModel
    {
        /**
         * Dados do membro autenticado
         */
        public Member? Member { get; set; }

        /**
         * Lista de livros com estatísticas
         * Varia conforme a aba ativa (disponíveis, emprestados, histórico)
         */
        public List<BookViewModel> Books { get; set; } = new List<BookViewModel>();

        /**
         * Aba atualmente ativa na interface
         * Valores: "available", "borrowed", "overdue", "history"
         */
        public string CurrentTab { get; set; } = "available";
    }

    /**
     * ViewModel para livros com dados agregados
     * Combina informações do livro, empréstimo e avaliações
     */
    public class BookViewModel
    {
        /**
         * Dados do livro
         */
        public Book Book { get; set; } = null!;

        /**
         * Dados do empréstimo (se aplicável)
         * Usado nas abas de livros emprestados e histórico
         */
        public Borrowing? Borrowing { get; set; }

        /**
         * Número de avaliações positivas do livro
         */
        public int LikesCount { get; set; }

        /**
         * Número de avaliações negativas do livro
         */
        public int DislikesCount { get; set; }

        /**
         * Lista de avaliações com comentários
         * Filtrada para mostrar apenas avaliações com texto
         */
        public List<BookReview> Comments { get; set; } = new List<BookReview>();
    }
}
