using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    /**
     * Entidade para avaliações de livros pelos membros
     * Implementa relacionamento muitos-para-um com Book e Member
     */
    public class BookReview
    {
        /**
         * Identificador único da avaliação
         * Chave primária gerada automaticamente
         */
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BookReviewId { get; set; } = Guid.NewGuid();

        /**
         * Identificador do livro avaliado
         * @required Chave estrangeira para Book
         */
        [Required]
        [ForeignKey("Book")]
        public Guid BookId { get; set; }

        /**
         * Identificador do membro que fez a avaliação
         * @required Chave estrangeira para Member
         */
        [Required]
        [ForeignKey("Member")]
        public Guid MemberId { get; set; }

        /**
         * Classificação do livro em estrelas
         * Intervalo válido: 1-5 estrelas (opcional)
         */
        [Range(1, 5, ErrorMessage = "A classificação deve estar entre 1 e 5")]
        [Display(Name = "Classificação")]
        public int? Rating { get; set; }

        /**
         * Comentário sobre o livro (opcional)
         * Máximo 1000 caracteres
         */
        [StringLength(1000, ErrorMessage = "O comentário não pode exceder 1000 caracteres")]
        [Display(Name = "Comentário")]
        public string? Comment { get; set; }

        /**
         * Indica se o membro gosta do livro
         * true = gosta, false = não gosta
         */
        [Display(Name = "É Like")]
        public bool IsLike { get; set; }

        /**
         * Data em que a avaliação foi criada
         */
        [Display(Name = "Data da Avaliação")]
        public DateTime ReviewDate { get; set; } = DateTime.Now;

        /* Propriedades de navegação para relacionamentos */

        /**
         * Navegação para o livro avaliado
         * Relacionamento muitos-para-um (muitas avaliações para um livro)
         */
        public virtual Book Book { get; set; } = null!;

        /**
         * Navegação para o membro que fez a avaliação
         * Relacionamento muitos-para-um (muitas avaliações para um membro)
         */
        public virtual Member Member { get; set; } = null!;
    }
}
