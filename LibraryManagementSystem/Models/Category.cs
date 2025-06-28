using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    /**
     * Entidade para categorização de livros
     * Implementa relacionamento muitos-para-muitos com Book
     */
    public class Category
    {
        /**
         * Identificador único da categoria
         * Chave primária gerada automaticamente
         */
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid CategoryId { get; set; } = Guid.NewGuid();

        /**
         * Nome da categoria
         * @required Campo obrigatório, máximo 100 caracteres
         */
        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
        [Display(Name = "Nome da Categoria")]
        public string Name { get; set; } = string.Empty;

        /**
         * Descrição da categoria (opcional)
         * Máximo 500 caracteres
         */
        [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres")]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        /**
         * Navegação para livros desta categoria
         * Relacionamento muitos-para-muitos (uma categoria pode ter vários livros)
         */
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
