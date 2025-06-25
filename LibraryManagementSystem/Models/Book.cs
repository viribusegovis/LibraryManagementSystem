using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BookId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título não pode exceder 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "O autor é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome do autor não pode exceder 100 caracteres")]
        public string Author { get; set; } = string.Empty;

        [StringLength(13, ErrorMessage = "ISBN deve ter no máximo 13 caracteres")]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

        [Required(ErrorMessage = "A categoria é obrigatória")]
        [ForeignKey("Category")]
        public Guid CategoryId { get; set; }

        [Range(1000, 2030, ErrorMessage = "Ano de publicação deve estar entre 1000 e 2030")]
        [Display(Name = "Ano de Publicação")]
        public int? YearPublished { get; set; }

        [Display(Name = "Disponível")]
        public bool Available { get; set; } = true;

        [Display(Name = "Data de Criação")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
        public virtual ICollection<BookReview> Reviews { get; set; } = new List<BookReview>();
    }
}
