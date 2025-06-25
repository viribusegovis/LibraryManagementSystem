using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Borrowing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BorrowingId { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Book")]
        public Guid BookId { get; set; }

        [Required]
        [ForeignKey("Member")]
        public Guid MemberId { get; set; }

        [Display(Name = "Data de Empréstimo")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Display(Name = "Data de Devolução")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "Data Limite")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14); // Default 14 days

        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Status { get; set; } = "Emprestado";

        [StringLength(500)]
        [Display(Name = "Observações")]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Book Book { get; set; } = null!;
        public virtual Member Member { get; set; } = null!;
    }
}
