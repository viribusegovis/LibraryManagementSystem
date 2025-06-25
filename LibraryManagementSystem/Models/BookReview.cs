using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class BookReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid BookReviewId { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Book")]
        public Guid BookId { get; set; }

        [Required]
        [ForeignKey("Member")]
        public Guid MemberId { get; set; }

        [Range(1, 5, ErrorMessage = "A classificação deve estar entre 1 e 5")]
        [Display(Name = "Classificação")]
        public int? Rating { get; set; }

        [StringLength(1000, ErrorMessage = "O comentário não pode exceder 1000 caracteres")]
        [Display(Name = "Comentário")]
        public string? Comment { get; set; }

        [Display(Name = "Gostei")]
        public bool? IsLiked { get; set; } // true = like, false = dislike, null = no reaction

        [Display(Name = "Data da Avaliação")]
        public DateTime ReviewDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Book Book { get; set; } = null!;
        public virtual Member Member { get; set; } = null!;
    }
}
