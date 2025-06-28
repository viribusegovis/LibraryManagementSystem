using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.DTOs
{
    /**
     * DTO base para informações de livros na API
     * Implementa projeção de dados para otimização de performance
     */
    public class BookDto
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public int? YearPublished { get; set; }
        public bool Available { get; set; }

        /**
         * Lista de nomes das categorias associadas
         * Relacionamento muitos-para-muitos simplificado
         */
        public List<string> Categories { get; set; } = new();

        /* Estatísticas calculadas de avaliações */
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
    }

    /**
     * DTO detalhado para livros com relacionamentos completos
     * Usado em endpoints que requerem informação completa
     */
    public class BookDetailDto : BookDto
    {
        public DateTime CreatedDate { get; set; }

        /**
         * Categorias com informação completa
         * Relacionamento muitos-para-muitos detalhado
         */
        public new List<CategoryDto> Categories { get; set; } = new();

        /**
         * Lista de avaliações do livro
         * Relacionamento muitos-para-um detalhado
         */
        public List<ReviewDto> Reviews { get; set; } = new();

        /**
         * Estatísticas completas do livro
         */
        public BookStatisticsDto Statistics { get; set; } = new();
    }

    /**
     * DTO para criação de novos livros
     * Implementa validação adequada de dados conforme requisitos
     */
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

        /**
         * Lista de IDs das categorias a associar
         * Suporta relacionamento muitos-para-muitos
         */
        public List<Guid>? CategoryIds { get; set; }
    }

    /**
     * DTO para atualização de livros existentes
     * Herda validações do CreateBookDto
     */
    public class UpdateBookDto : CreateBookDto
    {
    }

    /**
     * DTO para informações de categorias
     */
    public class CategoryDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /**
     * DTO para avaliações de livros
     * Relacionamento muitos-para-um com Book e Member
     */
    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public bool IsLike { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

    /**
     * DTO para estatísticas completas de livros
     */
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