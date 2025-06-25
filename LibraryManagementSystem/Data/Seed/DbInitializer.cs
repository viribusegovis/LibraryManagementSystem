// Data/Seed/DbSeeder.cs
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data.Seed
{
    public static class DbSeeder
    {
        public static void SeedData(ModelBuilder modelBuilder)
        {
            // Predefined GUIDs for consistent seeding
            var categoryFiction = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var categoryClassic = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var categoryScience = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var categoryHistory = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var categoryChildren = Guid.Parse("55555555-5555-5555-5555-555555555555");

            // Seed Identity Roles
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "bibliotecario",
                    Name = "Bibliotecário",
                    NormalizedName = "BIBLIOTECÁRIO"
                },
                new IdentityRole
                {
                    Id = "membro",
                    Name = "Membro",
                    NormalizedName = "MEMBRO"
                }
            );

            // Seed Identity Users
            var hasher = new PasswordHasher<IdentityUser>();
            modelBuilder.Entity<IdentityUser>().HasData(
                new IdentityUser
                {
                    Id = "admin",
                    UserName = "bibliotecario@biblioteca.pt",
                    NormalizedUserName = "BIBLIOTECARIO@BIBLIOTECA.PT",
                    Email = "bibliotecario@biblioteca.pt",
                    NormalizedEmail = "BIBLIOTECARIO@BIBLIOTECA.PT",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PasswordHash = hasher.HashPassword(null!, "Biblioteca123!")
                },
                new IdentityUser
                {
                    Id = "membro1",
                    UserName = "joao.silva@email.com",
                    NormalizedUserName = "JOAO.SILVA@EMAIL.COM",
                    Email = "joao.silva@email.com",
                    NormalizedEmail = "JOAO.SILVA@EMAIL.COM",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PasswordHash = hasher.HashPassword(null!, "Membro123!")
                }
            );

            // Associate users with roles
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "admin", RoleId = "bibliotecario" },
                new IdentityUserRole<string> { UserId = "membro1", RoleId = "membro" }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    CategoryId = categoryFiction,
                    Name = "Ficção",
                    Description = "Livros de ficção e romance",
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    CategoryId = categoryClassic,
                    Name = "Literatura Clássica",
                    Description = "Obras clássicas da literatura",
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    CategoryId = categoryScience,
                    Name = "Ciência",
                    Description = "Livros científicos e técnicos",
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    CategoryId = categoryHistory,
                    Name = "História",
                    Description = "Livros de história e biografias",
                    CreatedDate = DateTime.Now
                },
                new Category
                {
                    CategoryId = categoryChildren,
                    Name = "Infantil",
                    Description = "Livros para crianças",
                    CreatedDate = DateTime.Now
                }
            );

            // Seed Books
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    BookId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Title = "O Alquimista",
                    Author = "Paulo Coelho",
                    ISBN = "9788576651234",
                    CategoryId = categoryFiction,
                    YearPublished = 1988,
                    Available = true,
                    CreatedDate = DateTime.Now.AddDays(-30)
                },
                new Book
                {
                    BookId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Title = "Dom Casmurro",
                    Author = "Machado de Assis",
                    ISBN = "9788576651235",
                    CategoryId = categoryClassic,
                    YearPublished = 1899,
                    Available = true,
                    CreatedDate = DateTime.Now.AddDays(-25)
                },
                new Book
                {
                    BookId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Title = "1984",
                    Author = "George Orwell",
                    ISBN = "9788576651236",
                    CategoryId = categoryFiction,
                    YearPublished = 1949,
                    Available = true,
                    CreatedDate = DateTime.Now.AddDays(-20)
                }
            );

            // Seed Members
            modelBuilder.Entity<Member>().HasData(
                new Member
                {
                    MemberId = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111"),
                    Name = "João Silva",
                    Email = "joao.silva@email.com",
                    Phone = "912345678",
                    Address = "Rua das Flores, 123, Lisboa",
                    MembershipDate = DateTime.Now.AddMonths(-6),
                    IsActive = true
                },
                new Member
                {
                    MemberId = Guid.Parse("22222222-bbbb-bbbb-bbbb-222222222222"),
                    Name = "Maria Santos",
                    Email = "maria.santos@email.com",
                    Phone = "923456789",
                    Address = "Avenida da Liberdade, 456, Porto",
                    MembershipDate = DateTime.Now.AddMonths(-4),
                    IsActive = true
                }
            );
        }
    }
}
