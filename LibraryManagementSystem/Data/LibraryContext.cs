using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Data
{
    /**
     * Contexto principal da base de dados para o Sistema de Gestão de Biblioteca
     * Herda de IdentityDbContext para suporte completo de autenticação e autorização
     * Implementa relacionamentos "muitos-para-um" e "muitos-para-muitos" obrigatórios
     */
    public class LibraryContext : IdentityDbContext<IdentityUser>
    {
        /**
         * Inicializa o contexto com opções de configuração
         * @param options Opções de configuração do Entity Framework
         */
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        /* DbSets para as tabelas obrigatórias - pelo menos três tabelas conforme requisitos */
        public DbSet<Book> Books { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Borrowing> Borrowings { get; set; }
        public DbSet<BookReview> BookReviews { get; set; }

        /**
         * Configura o modelo da base de dados com relacionamentos e restrições
         * Implementa relacionamentos "muitos-para-um" e "muitos-para-muitos" obrigatórios
         * 
         * @param modelBuilder Construtor do modelo Entity Framework
         */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* Configurar chaves primárias GUID com geração automática */
            modelBuilder.Entity<Book>()
                .Property(b => b.BookId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Member>()
                .Property(m => m.MemberId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Borrowing>()
                .Property(br => br.BorrowingId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<BookReview>()
                .Property(br => br.BookReviewId)
                .HasDefaultValueSql("NEWID()");

            /* RELACIONAMENTOS MUITOS-PARA-UM (obrigatórios conforme avaliação) */

            /* Member -> User (relacionamento com Identity) */
            modelBuilder.Entity<Member>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Permite manter membro se utilizador for eliminado

            /* Borrowing -> Book (muitos empréstimos para um livro) */
            modelBuilder.Entity<Borrowing>()
                .HasOne(br => br.Book)
                .WithMany(b => b.Borrowings)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict); // Impede eliminação de livro com empréstimos

            /* Borrowing -> Member (muitos empréstimos para um membro) */
            modelBuilder.Entity<Borrowing>()
                .HasOne(br => br.Member)
                .WithMany(m => m.Borrowings)
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict); // Impede eliminação de membro com empréstimos

            /* BookReview -> Book (muitas avaliações para um livro) */
            modelBuilder.Entity<BookReview>()
                .HasOne(br => br.Book)
                .WithMany(b => b.BookReviews)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict); // Impede eliminação de livro com avaliações

            /* BookReview -> Member (muitas avaliações para um membro) */
            modelBuilder.Entity<BookReview>()
                .HasOne(br => br.Member)
                .WithMany(m => m.BookReviews)
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict); // Impede eliminação de membro com avaliações

            /* RELACIONAMENTO MUITOS-PARA-MUITOS (obrigatório conforme avaliação)
               Book <-> Category: Um livro pode ter várias categorias, uma categoria pode ter vários livros */
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Categories)
                .WithMany(c => c.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookCategory", // Nome da entidade de junção
                    j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    j => j.HasOne<Book>().WithMany().HasForeignKey("BookId"),
                    j =>
                    {
                        j.HasKey("BookId", "CategoryId"); // Chave composta
                        j.ToTable("BookCategories"); // Nome da tabela de junção
                    });
        }
    }
}
