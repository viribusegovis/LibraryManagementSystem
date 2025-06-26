// Data/Seed/DbInitializer.cs - Fixed for Many-to-Many relationship
using Microsoft.AspNetCore.Identity;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data.Seed
{
    internal class DbInitializer
    {
        internal static async Task Initialize(LibraryContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));
            dbContext.Database.EnsureCreated();

            bool haAdicao = false;

            // 1. CREATE ROLES FIRST
            if (!dbContext.Roles.Any())
            {
                var roles = new[]
                {
                    new IdentityRole
                    {
                        Id = "bibliotecario-role",
                        Name = "Bibliotecário",
                        NormalizedName = "BIBLIOTECÁRIO"
                    },
                    new IdentityRole
                    {
                        Id = "membro-role",
                        Name = "Membro",
                        NormalizedName = "MEMBRO"
                    }
                };
                await dbContext.Roles.AddRangeAsync(roles);
                haAdicao = true;
            }

            // 2. CREATE CATEGORIES
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

            // 3. CREATE USERS WITH PROPER PASSWORD HASHING
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
                        SecurityStamp = Guid.NewGuid().ToString(),
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
                        SecurityStamp = Guid.NewGuid().ToString(),
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
                        SecurityStamp = Guid.NewGuid().ToString(),
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
                        SecurityStamp = Guid.NewGuid().ToString(),
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

            // 4. ASSIGN USERS TO ROLES - CRITICAL STEP!
            if (!dbContext.UserRoles.Any())
            {
                var userRoles = new[]
                {
                    new IdentityUserRole<string>
                    {
                        UserId = "bibliotecario1",
                        RoleId = "bibliotecario-role"
                    },
                    new IdentityUserRole<string>
                    {
                        UserId = "membro1",
                        RoleId = "membro-role"
                    },
                    new IdentityUserRole<string>
                    {
                        UserId = "membro2",
                        RoleId = "membro-role"
                    },
                    new IdentityUserRole<string>
                    {
                        UserId = "membro3",
                        RoleId = "membro-role"
                    }
                };
                await dbContext.UserRoles.AddRangeAsync(userRoles);
                haAdicao = true;
            }

            // 5. CREATE MEMBERS - ALL LINKED TO USERS
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
                        UserId = "membro1"
                    },
                    new Member {
                        Name = "Maria Santos",
                        Email = "maria.santos@email.com",
                        Phone = "923456789",
                        Address = "Avenida da Liberdade, 456, Porto",
                        CardNumber = "LIB002",
                        DateOfBirth = new DateTime(1985, 8, 22),
                        MembershipDate = DateTime.Now.AddMonths(-4),
                        UserId = "membro2"
                    },
                    new Member {
                        Name = "Pedro Costa",
                        Email = "pedro.costa@email.com",
                        Phone = "934567890",
                        Address = "Praça do Comércio, 789, Coimbra",
                        CardNumber = "LIB003",
                        DateOfBirth = new DateTime(1988, 12, 10),
                        MembershipDate = DateTime.Now.AddMonths(-2),
                        UserId = "membro3"
                    }
                ];
                await dbContext.Members.AddRangeAsync(membros);
                haAdicao = true;
            }
            else
            {
                membros = dbContext.Members.ToArray();
            }

            // 6. CREATE BOOKS - REMOVED CategoryId (now many-to-many)
            var livros = Array.Empty<Book>();
            if (!dbContext.Books.Any())
            {
                livros = [
                    new Book { Title = "O Alquimista", Author = "Paulo Coelho", ISBN = "9788576651234", YearPublished = 1988 },
                    new Book { Title = "Dom Casmurro", Author = "Machado de Assis", ISBN = "9788576651235", YearPublished = 1899 },
                    new Book { Title = "1984", Author = "George Orwell", ISBN = "9788576651236", YearPublished = 1949 },
                    new Book { Title = "Uma Breve História do Tempo", Author = "Stephen Hawking", ISBN = "9788576651237", YearPublished = 1988 },
                    new Book { Title = "O Pequeno Príncipe", Author = "Antoine de Saint-Exupéry", ISBN = "9788576651238", YearPublished = 1943 },
                    new Book { Title = "Clean Code", Author = "Robert C. Martin", ISBN = "9780132350884", YearPublished = 2008 },
                    new Book { Title = "Os Lusíadas", Author = "Luís de Camões", ISBN = "9789722040495", YearPublished = 1572 },
                    new Book { Title = "O Código Da Vinci", Author = "Dan Brown", ISBN = "9788576651240", YearPublished = 2003 }
                ];
                await dbContext.Books.AddRangeAsync(livros);
                haAdicao = true;
            }
            else
            {
                livros = dbContext.Books.ToArray();
            }

            // Save changes before assigning many-to-many relationships
            if (haAdicao)
            {
                await dbContext.SaveChangesAsync();
                haAdicao = false;
            }

            // 7. ASSIGN CATEGORIES TO BOOKS (Many-to-Many) - FIXED
            if (categorias.Length > 0 && livros.Length > 0)
            {
                // Load books with their categories to check if already assigned
                var booksWithCategories = await dbContext.Books
                    .Include(b => b.Categories)
                    .ToListAsync();

                // O Alquimista - Ficção + História
                var livroAlquimista = booksWithCategories.FirstOrDefault(b => b.Title == "O Alquimista");
                if (livroAlquimista != null && !livroAlquimista.Categories.Any())
                {
                    livroAlquimista.Categories.Add(categorias[0]); // Ficção
                    livroAlquimista.Categories.Add(categorias[3]); // História
                    haAdicao = true;
                }

                // Dom Casmurro - Ficção + Literatura Clássica
                var livroDomCasmurro = booksWithCategories.FirstOrDefault(b => b.Title == "Dom Casmurro");
                if (livroDomCasmurro != null && !livroDomCasmurro.Categories.Any())
                {
                    livroDomCasmurro.Categories.Add(categorias[0]); // Ficção
                    livroDomCasmurro.Categories.Add(categorias[1]); // Literatura Clássica
                    haAdicao = true;
                }

                // 1984 - Ficção + História
                var livro1984 = booksWithCategories.FirstOrDefault(b => b.Title == "1984");
                if (livro1984 != null && !livro1984.Categories.Any())
                {
                    livro1984.Categories.Add(categorias[0]); // Ficção
                    livro1984.Categories.Add(categorias[3]); // História
                    haAdicao = true;
                }

                // Uma Breve História do Tempo - Ciência
                var livroHistoriaTempo = booksWithCategories.FirstOrDefault(b => b.Title == "Uma Breve História do Tempo");
                if (livroHistoriaTempo != null && !livroHistoriaTempo.Categories.Any())
                {
                    livroHistoriaTempo.Categories.Add(categorias[2]); // Ciência
                    haAdicao = true;
                }

                // O Pequeno Príncipe - Infantil + Arte
                var livroPequenoPrincipe = booksWithCategories.FirstOrDefault(b => b.Title == "O Pequeno Príncipe");
                if (livroPequenoPrincipe != null && !livroPequenoPrincipe.Categories.Any())
                {
                    livroPequenoPrincipe.Categories.Add(categorias[4]); // Infantil
                    livroPequenoPrincipe.Categories.Add(categorias[6]); // Arte
                    haAdicao = true;
                }

                // Clean Code - Tecnologia + Ciência
                var livroCleanCode = booksWithCategories.FirstOrDefault(b => b.Title == "Clean Code");
                if (livroCleanCode != null && !livroCleanCode.Categories.Any())
                {
                    livroCleanCode.Categories.Add(categorias[5]); // Tecnologia
                    livroCleanCode.Categories.Add(categorias[2]); // Ciência
                    haAdicao = true;
                }

                // Os Lusíadas - Literatura Clássica + História
                var livroLusiadas = booksWithCategories.FirstOrDefault(b => b.Title == "Os Lusíadas");
                if (livroLusiadas != null && !livroLusiadas.Categories.Any())
                {
                    livroLusiadas.Categories.Add(categorias[1]); // Literatura Clássica
                    livroLusiadas.Categories.Add(categorias[3]); // História
                    haAdicao = true;
                }

                // O Código Da Vinci - Ficção + História
                var livroCodigoDaVinci = booksWithCategories.FirstOrDefault(b => b.Title == "O Código Da Vinci");
                if (livroCodigoDaVinci != null && !livroCodigoDaVinci.Categories.Any())
                {
                    livroCodigoDaVinci.Categories.Add(categorias[0]); // Ficção
                    livroCodigoDaVinci.Categories.Add(categorias[3]); // História
                    haAdicao = true;
                }
            }

            // Save many-to-many relationships
            if (haAdicao)
            {
                await dbContext.SaveChangesAsync();
                haAdicao = false;
            }

            // 8. CREATE BORROWINGS INCLUDING OVERDUE
            if (!dbContext.Borrowings.Any() && livros.Length > 0 && membros.Length > 0)
            {
                var emprestimos = new[]
                {
                    new Borrowing {
                        BookId = livros[0].BookId, // O Alquimista
                        MemberId = membros[0].MemberId, // João Silva
                        BorrowDate = DateTime.Now.AddDays(-10),
                        DueDate = DateTime.Now.AddDays(4),
                        Status = "Emprestado"
                    },
                    new Borrowing {
                        BookId = livros[2].BookId, // 1984
                        MemberId = membros[1].MemberId, // Maria Santos
                        BorrowDate = DateTime.Now.AddDays(-15),
                        ReturnDate = DateTime.Now.AddDays(-3),
                        DueDate = DateTime.Now.AddDays(-1),
                        Status = "Devolvido"
                    },
                    // OVERDUE ENTRIES
                    new Borrowing {
                        BookId = livros[1].BookId, // Dom Casmurro
                        MemberId = membros[2].MemberId, // Pedro Costa
                        BorrowDate = DateTime.Now.AddDays(-25),
                        DueDate = DateTime.Now.AddDays(-10), // 10 days overdue
                        Status = "Emprestado"
                    },
                    new Borrowing {
                        BookId = livros[3].BookId, // Uma Breve História do Tempo
                        MemberId = membros[0].MemberId, // João Silva
                        BorrowDate = DateTime.Now.AddDays(-30),
                        DueDate = DateTime.Now.AddDays(-5), // 5 days overdue
                        Status = "Emprestado"
                    }
                };
                await dbContext.Borrowings.AddRangeAsync(emprestimos);
                haAdicao = true;
            }

            // 9. CREATE BOOK REVIEWS FOR TESTING
            if (!dbContext.BookReviews.Any() && livros.Length > 0 && membros.Length > 0)
            {
                var reviews = new[]
                {
                    new BookReview {
                        BookId = livros[0].BookId, // O Alquimista
                        MemberId = membros[0].MemberId, // João Silva
                        IsLike = true,
                        Comment = "Livro inspirador e bem escrito!",
                        ReviewDate = DateTime.Now.AddDays(-5)
                    },
                    new BookReview {
                        BookId = livros[1].BookId, // Dom Casmurro
                        MemberId = membros[1].MemberId, // Maria Santos
                        IsLike = true,
                        Comment = "Clássico da literatura brasileira, recomendo!",
                        ReviewDate = DateTime.Now.AddDays(-3)
                    },
                    new BookReview {
                        BookId = livros[2].BookId, // 1984
                        MemberId = membros[2].MemberId, // Pedro Costa
                        IsLike = false,
                        Comment = "Muito pesado e depressivo para o meu gosto.",
                        ReviewDate = DateTime.Now.AddDays(-2)
                    }
                };
                await dbContext.BookReviews.AddRangeAsync(reviews);
                haAdicao = true;
            }

            try
            {
                if (haAdicao)
                {
                    // FINAL SAVE
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
