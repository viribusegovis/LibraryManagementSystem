using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Models
{
    /**
     * Entidade para membros da biblioteca
     * Implementa relacionamento com IdentityUser e relacionamentos muitos-para-um
     */
    public class Member
    {
        /**
         * Identificador único do membro
         * Chave primária gerada automaticamente
         */
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid MemberId { get; set; } = Guid.NewGuid();

        /**
         * Nome completo do membro
         * @required Campo obrigatório, máximo 100 caracteres
         */
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
        [Display(Name = "Nome Completo")]
        public string Name { get; set; } = string.Empty;

        /**
         * Endereço de email do membro
         * @required Campo obrigatório com validação de formato
         */
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(150, ErrorMessage = "O email não pode exceder 150 caracteres")]
        public string Email { get; set; } = string.Empty;

        /**
         * Número de telefone (opcional)
         * Validação de formato de telefone
         */
        [Phone(ErrorMessage = "Formato de telefone inválido")]
        [StringLength(15, ErrorMessage = "O telefone não pode exceder 15 caracteres")]
        public string? Phone { get; set; }

        /**
         * Morada do membro (opcional)
         * Máximo 200 caracteres
         */
        [StringLength(200, ErrorMessage = "A morada não pode exceder 200 caracteres")]
        [Display(Name = "Morada")]
        public string? Address { get; set; }

        /**
         * Data de nascimento (opcional)
         */
        [Display(Name = "Data de Nascimento")]
        public DateTime? DateOfBirth { get; set; }

        /**
         * Número único do cartão de membro (opcional)
         * Máximo 20 caracteres
         */
        [StringLength(20)]
        [Display(Name = "Número de Cartão")]
        public string? CardNumber { get; set; }

        /**
         * Data de inscrição na biblioteca
         * Definida automaticamente na criação
         */
        [Display(Name = "Data de Inscrição")]
        public DateTime MembershipDate { get; set; } = DateTime.Now;

        /**
         * Estado ativo/inativo do membro
         * Controla acesso aos serviços da biblioteca
         */
        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;

        /**
         * Identificador do utilizador do sistema
         * Chave estrangeira para IdentityUser
         */
        [Display(Name = "Utilizador do Sistema")]
        public string? UserId { get; set; }

        /* Propriedades de navegação para relacionamentos */

        /**
         * Navegação para utilizador do sistema de autenticação
         * Relacionamento um-para-um com IdentityUser
         */
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        /**
         * Navegação para empréstimos do membro
         * Relacionamento muitos-para-um (um membro pode ter vários empréstimos)
         */
        public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

        /**
         * Navegação para avaliações feitas pelo membro
         * Relacionamento muitos-para-um (um membro pode fazer várias avaliações)
         */
        public virtual ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
    }
}
