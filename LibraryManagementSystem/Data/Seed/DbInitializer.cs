// Data/Seed/DbInitializer.cs
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Data.Seed
{
    internal class DbInitializer
    {
        internal static async Task Initialize(LibraryContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
            dbContext.Database.EnsureCreated();

            // Variável auxiliar
            bool haAdicao = false;

            // Se não houver Categorias, cria-as
            var categorias = Array.Empty<Category>();
            if (!dbContext.Categories.Any())
            {
                categorias = [
                    new Category { Name = "Ficção", Description = "Livros de ficção e romance" },
                    new Category { Name = "Literatura Clássica", Description = "Obras clássicas da literatura" },
                    new Category { Name = "Ciência", Description = "Livros científicos e técnicos" },
                    new Category { Name = "História", Description = "Livros de história e biografias" },
                    new Category { Name = "Infantil", Description = "Livros para crianças" },
                    new Category { Name = "Tecnologia", Description = "Livros sobre tecnologia e programação" },
                    new Category { Name = "Arte", Description = "Livros sobre arte e cultura" }
                ];
                await dbContext.Categories.AddRangeAsync(categorias);
                haAdicao = true;
            }
            else
            {
                categorias = dbContext.Categories.ToArray();
            }

            // Se não houver Utilizadores Identity, cria-os
            var newIdentityUsers = Array.Empty<IdentityUser>();
            var hasher = new PasswordHasher<IdentityUser>();

            if (!dbContext.Users.Any())
            {
                newIdentityUsers = [
                    new IdentityUser{
                        Id = "bibliotecario1",
                        UserName = "bibliotecario@biblioteca.pt",
                        NormalizedUserName = "BIBLIOTECARIO@BIBLIOTECA.PT",
                        Email = "bibliotecario@biblioteca.pt",
                        NormalizedEmail = "BIBLIOTECARIO@BIBLIOTECA.PT",
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("N").ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        PasswordHash = hasher.HashPassword(null!, "Biblioteca123!")
                    },
                    new IdentityUser{
                        Id = "membro1",
                        UserName = "joao.silva@email.com",
                        NormalizedUserName = "JOAO.SILVA@EMAIL.COM",
                        Email = "joao.silva@email.com",
                        NormalizedEmail = "JOAO.SILVA@EMAIL.COM",
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("N").ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        PasswordHash = hasher.HashPassword(null!, "Membro123!")
                    },
                    new IdentityUser{
                        Id = "membro2",
                        UserName = "maria.santos@email.com",
                        NormalizedUserName = "MARIA.SANTOS@EMAIL.COM",
                        Email = "maria.santos@email.com",
                        NormalizedEmail = "MARIA.SANTOS@EMAIL.COM",
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("N").ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        PasswordHash = hasher.HashPassword(null!, "Membro123!")
                    },
                    new IdentityUser{
                        Id = "membro3",
                        UserName = "pedro.costa@email.com",
                        NormalizedUserName = "PEDRO.COSTA@EMAIL.COM",
                        Email = "pedro.costa@email.com",
                        NormalizedEmail = "PEDRO.COSTA@EMAIL.COM",
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString("N").ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        PasswordHash = hasher.HashPassword(null!, "Membro123!")
                    }
                ];
                await dbContext.Users.AddRangeAsync(newIdentityUsers);
                haAdicao = true;
            }
            else
            {
                newIdentityUsers = dbContext.Users.ToArray();
            }

            // Se não houver Membros, cria-os
            var membros = Array.Empty<Member>();
            if (!dbContext.Members.Any())
            {
                membros = [
                    new Member {
                        Name = "João Silva",
                        Email = "joao.silva@email.com",
                        Phone = "912345678",
                        Address = "Rua das Flores, 123, Lisboa",
                        CardNumber = "LIB001",
                        DateOfBirth = new DateTime(1990, 5, 15),
                        MembershipDate = DateTime.Now.AddMonths(-6),
                        UserId = newIdentityUsers.FirstOrDefault(u => u.Email == "joao.silva@email.com")?.Id
                    },
                    new Member {
                        Name = "Maria Santos",
                        Email = "maria.santos@email.com",
                        Phone = "923456789",
                        Address = "Avenida da Liberdade, 456, Porto",
                        CardNumber = "LIB002",
                        DateOfBirth = new DateTime(1985, 8, 22),
                        MembershipDate = DateTime.Now.AddMonths(-4),
                        UserId = newIdentityUsers.FirstOrDefault(u => u.Email == "maria.santos@email.com")?.Id
                    },
                    new Member {
                        Name = "Pedro Costa",
                        Email = "pedro.costa@email.com",
                        Phone = "934567890",
                        Address = "Praça do Comércio, 789, Coimbra",
                        CardNumber = "LIB003",
                        DateOfBirth = new DateTime(1988, 12, 10),
                        MembershipDate = DateTime.Now.AddMonths(-2),
                        UserId = newIdentityUsers.FirstOrDefault(u => u.Email == "pedro.costa@email.com")?.Id
                    }
                ];
                await dbContext.Members.AddRangeAsync(membros);
                haAdicao = true;
            }
            else
            {
                membros = dbContext.Members.ToArray();
            }

            // Se não houver Livros, cria-os
            var livros = Array.Empty<Book>();
            if (!dbContext.Books.Any())
            {
                livros = [
                    new Book { Title = "O Alquimista", Author = "Paulo Coelho", ISBN = "9788576651234", CategoryId = categorias[0].CategoryId, YearPublished = 1988 },
                    new Book { Title = "Dom Casmurro", Author = "Machado de Assis", ISBN = "9788576651235", CategoryId = categorias[1].CategoryId, YearPublished = 1899 },
                    new Book { Title = "1984", Author = "George Orwell", ISBN = "9788576651236", CategoryId = categorias[0].CategoryId, YearPublished = 1949 },
                    new Book { Title = "Uma Breve História do Tempo", Author = "Stephen Hawking", ISBN = "9788576651237", CategoryId = categorias[2].CategoryId, YearPublished = 1988 },
                    new Book { Title = "O Pequeno Príncipe", Author = "Antoine de Saint-Exupéry", ISBN = "9788576651238", CategoryId = categorias[4].CategoryId, YearPublished = 1943 },
                    new Book { Title = "Clean Code", Author = "Robert C. Martin", ISBN = "9780132350884", CategoryId = categorias[5].CategoryId, YearPublished = 2008 },
                    new Book { Title = "Os Lusíadas", Author = "Luís de Camões", ISBN = "9789722040495", CategoryId = categorias[1].CategoryId, YearPublished = 1572 },
                    new Book { Title = "O Código Da Vinci", Author = "Dan Brown", ISBN = "9788576651240", CategoryId = categorias[0].CategoryId, YearPublished = 2003 }
                ];
                await dbContext.Books.AddRangeAsync(livros);
                haAdicao = true;
            }
            else
            {
                livros = dbContext.Books.ToArray();
            }

            // Se não houver Empréstimos, cria alguns exemplos
            if (!dbContext.Borrowings.Any() && livros.Length > 0 && membros.Length > 0)
            {
                var emprestimos = Array.Empty<Borrowing>();
                emprestimos = [
                
                    new Borrowing {
                        BookId = livros[0].BookId,
                        MemberId = membros[0].MemberId,
                        BorrowDate = DateTime.Now.AddDays(-10),
                        DueDate = DateTime.Now.AddDays(4),
                        Status = "Emprestado"
                    },
                    new Borrowing {
                        BookId = livros[2].BookId,
                        MemberId = membros[1].MemberId,
                        BorrowDate = DateTime.Now.AddDays(-15),
                        ReturnDate = DateTime.Now.AddDays(-3),
                        DueDate = DateTime.Now.AddDays(-1),
                        Status = "Devolvido"
                    },
                ];
                await dbContext.Borrowings.AddRangeAsync(emprestimos);
                haAdicao = true;
            }

            try
            {
                if (haAdicao)
                {
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao guardar dados de seed: {ex.Message}");
                throw;
            }
        }
    }
}
