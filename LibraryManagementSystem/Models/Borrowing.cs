using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Borrowing
    {
        public Guid BorrowingId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Selecione um livro")]
        [Display(Name = "Livro")]
        public Guid BookId { get; set; }

        [Required(ErrorMessage = "Selecione um membro")]
        [Display(Name = "Membro")]
        public Guid MemberId { get; set; }

        [Display(Name = "Data de Empréstimo")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "A data limite é obrigatória")]
        [Display(Name = "Data Limite")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Data de Devolução")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Emprestado";

        // Navigation properties
        public virtual Book Book { get; set; }
        public virtual Member Member { get; set; }
    }
}
