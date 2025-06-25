// Data/LibraryContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Data
{
    public class LibraryContext : IdentityDbContext<IdentityUser>
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Borrowing> Borrowings { get; set; }
        public DbSet<BookReview> BookReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure GUID primary keys
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

            // Configure optional relationship between Member and IdentityUser
            modelBuilder.Entity<Member>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure other relationships
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Borrowing>()
                .HasOne(br => br.Book)
                .WithMany(b => b.Borrowings)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Borrowing>()
                .HasOne(br => br.Member)
                .WithMany(m => m.Borrowings)
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookReview>()
                .HasOne(br => br.Book)
                .WithMany(b => b.Reviews)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookReview>()
                .HasOne(br => br.Member)
                .WithMany(m => m.Reviews)
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
