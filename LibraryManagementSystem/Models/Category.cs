using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid CategoryId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(50, ErrorMessage = "O nome da categoria não pode exceder 50 caracteres")]
        [Display(Name = "Nome da Categoria")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "A descrição não pode exceder 200 caracteres")]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        [Display(Name = "Ativa")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
