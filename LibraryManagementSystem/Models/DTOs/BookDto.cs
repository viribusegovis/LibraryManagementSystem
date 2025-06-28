// Models/DTOs/BookDto.cs
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.DTOs
{
    public class BookDto
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public int? YearPublished { get; set; }
        public bool Available { get; set; }
        public List<string> Categories { get; set; } = new();
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
    }

    public class BookDetailDto : BookDto
    {
        public DateTime CreatedDate { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
        public BookStatisticsDto Statistics { get; set; } = new();
    }

    public class CreateBookDto
    {
        [Required(ErrorMessage = "O título é obrigatório")]
        [StringLength(200, ErrorMessage = "O título não pode exceder 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "O autor é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome do autor não pode exceder 100 caracteres")]
        public string Author { get; set; } = string.Empty;

        [StringLength(13, ErrorMessage = "ISBN deve ter no máximo 13 caracteres")]
        public string? ISBN { get; set; }

        [Range(1000, 2030, ErrorMessage = "Ano de publicação deve estar entre 1000 e 2030")]
        public int? YearPublished { get; set; }

        public bool Available { get; set; } = true;

        public List<Guid>? CategoryIds { get; set; }
    }

    public class UpdateBookDto : CreateBookDto
    {
    }

    public class CategoryDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public bool IsLike { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

    public class BookStatisticsDto
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public int TotalBorrowings { get; set; }
        public bool CurrentlyBorrowed { get; set; }
    }
}
