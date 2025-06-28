using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    /**
     * ViewModel para o dashboard administrativo da biblioteca
     * Agrega estatísticas e dados para interface de administração
     */
    public class AdminDashboardViewModel
    {
        /* Contadores gerais do sistema */
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalCategories { get; set; }

        /* Métricas de empréstimos */
        public int ActiveBorrowings { get; set; }
        public int OverdueBooks { get; set; }
        public int BooksReturnedToday { get; set; }

        /**
         * Lista dos empréstimos mais recentes
         * Limitada aos últimos 5 registos para performance
         */
        public List<Borrowing> RecentBorrowings { get; set; } = new List<Borrowing>();

        /**
         * Lista de livros em atraso
         * Ordenada por urgência (data de vencimento)
         */
        public List<Borrowing> OverdueList { get; set; } = new List<Borrowing>();

        /**
         * Lista dos livros mais populares
         * Baseada no número de empréstimos
         */
        public List<PopularBookViewModel> PopularBooks { get; set; } = new List<PopularBookViewModel>();
    }

    /**
     * ViewModel para livros populares com estatísticas
     * Combina dados do livro com contagem de empréstimos
     */
    public class PopularBookViewModel
    {
        /**
         * Dados do livro
         */
        public Book Book { get; set; } = null!;

        /**
         * Número total de empréstimos do livro
         */
        public int BorrowCount { get; set; }
    }
}
