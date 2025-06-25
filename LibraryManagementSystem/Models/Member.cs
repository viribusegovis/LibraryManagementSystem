using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Member
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid MemberId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
        [Display(Name = "Nome Completo")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(150, ErrorMessage = "O email não pode exceder 150 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de telefone inválido")]
        [StringLength(15, ErrorMessage = "O telefone não pode exceder 15 caracteres")]
        public string? Phone { get; set; }

        [StringLength(200, ErrorMessage = "A morada não pode exceder 200 caracteres")]
        [Display(Name = "Morada")]
        public string? Address { get; set; }

        [Display(Name = "Data de Inscrição")]
        public DateTime MembershipDate { get; set; } = DateTime.Now;

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
        public virtual ICollection<BookReview> Reviews { get; set; } = new List<BookReview>();
    }
}
