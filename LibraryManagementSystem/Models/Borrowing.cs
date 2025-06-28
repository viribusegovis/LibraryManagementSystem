using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    /**
     * Entidade para gestão de empréstimos de livros
     * Implementa relacionamento muitos-para-um com Book e Member
     */
    public class Borrowing
    {
        /**
         * Identificador único do empréstimo
         * Chave primária gerada automaticamente
         */
        public Guid BorrowingId { get; set; } = Guid.NewGuid();

        /**
         * Identificador do livro emprestado
         * @required Chave estrangeira para Book
         */
        [Required(ErrorMessage = "Selecione um livro")]
        [Display(Name = "Livro")]
        public Guid BookId { get; set; }

        /**
         * Identificador do membro que fez o empréstimo
         * @required Chave estrangeira para Member
         */
        [Required(ErrorMessage = "Selecione um membro")]
        [Display(Name = "Membro")]
        public Guid MemberId { get; set; }

        /**
         * Data em que o empréstimo foi realizado
         * Definida automaticamente na criação
         */
        [Display(Name = "Data de Empréstimo")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        /**
         * Data limite para devolução do livro
         * @required Campo obrigatório para controlo de prazos
         */
        [Required(ErrorMessage = "A data limite é obrigatória")]
        [Display(Name = "Data Limite")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        /**
         * Data de devolução do livro (opcional)
         * Preenchida quando o livro é devolvido
         */
        [Display(Name = "Data de Devolução")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        /**
         * Estado atual do empréstimo
         * Valores possíveis: "Emprestado", "Devolvido"
         */
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Emprestado";

        /* Propriedades de navegação para relacionamentos */

        /**
         * Navegação para o livro emprestado
         * Relacionamento muitos-para-um (muitos empréstimos para um livro)
         */
        public virtual Book Book { get; set; }

        /**
         * Navegação para o membro que fez o empréstimo
         * Relacionamento muitos-para-um (muitos empréstimos para um membro)
         */
        public virtual Member Member { get; set; }
    }
}
