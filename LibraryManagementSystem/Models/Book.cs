using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    /**
     * Entidade para representar livros no sistema
     * Suporta relacionamentos muitos-para-um e muitos-para-muitos
     */
    public class Book
    {
        /**
         * Identificador único do livro
         * Chave primária gerada automaticamente
         */
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BookId { get; set; } = Guid.NewGuid();

        /**
         * Título do livro
         * @required Campo obrigatório, máximo 200 caracteres
         */
        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título não pode exceder 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        /**
         * Nome do autor
         * @required Campo obrigatório, máximo 100 caracteres
         */
        [Required(ErrorMessage = "O autor é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome do autor não pode exceder 100 caracteres")]
        public string Author { get; set; } = string.Empty;

        /**
         * Código ISBN do livro (opcional)
         * Máximo 13 caracteres
         */
        [StringLength(13, ErrorMessage = "ISBN deve ter no máximo 13 caracteres")]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

        /**
         * Ano de publicação (opcional)
         * Intervalo válido: 1000-2030
         */
        [Range(1000, 2030, ErrorMessage = "Ano de publicação deve estar entre 1000 e 2030")]
        [Display(Name = "Ano de Publicação")]
        public int? YearPublished { get; set; }

        /**
         * Estado de disponibilidade para empréstimo
         */
        [Display(Name = "Disponível")]
        public bool Available { get; set; } = true;

        /**
         * Data de criação do registo
         */
        [Display(Name = "Data de Criação")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /* Relacionamentos da entidade */

        /**
         * Relacionamento muitos-para-muitos com categorias
         * Um livro pode ter várias categorias
         */
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        /**
         * Relacionamento muitos-para-um com empréstimos
         * Um livro pode ter vários empréstimos
         */
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

        /**
         * Relacionamento muitos-para-um com avaliações
         * Um livro pode ter várias avaliações
         */
        public virtual ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
    }
}
