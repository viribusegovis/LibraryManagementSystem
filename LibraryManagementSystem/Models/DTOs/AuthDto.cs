using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Models.DTOs
{
    /**
     * DTO para pedido de autenticação na API
     * Implementa validação de dados obrigatória conforme requisitos de avaliação
     */
    public class LoginDto
    {
        /**
         * Email do utilizador para autenticação
         * @required Campo obrigatório com validação de formato de email
         */
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        /**
         * Password do utilizador para autenticação
         * @required Campo obrigatório para validação de credenciais
         */
        [Required(ErrorMessage = "Password é obrigatória")]
        public string Password { get; set; } = string.Empty;
    }

    /**
     * DTO para resposta de autenticação bem-sucedida
     * Contém token JWT e informações do utilizador autenticado
     */
    public class LoginResponseDto
    {
        /**
         * Token JWT para autenticação em endpoints protegidos
         * Válido por 24 horas após geração
         */
        public string Token { get; set; } = string.Empty;

        /**
         * Email do utilizador autenticado
         */
        public string Email { get; set; } = string.Empty;

        /**
         * Nome de exibição do utilizador
         */
        public string Name { get; set; } = string.Empty;

        /**
         * Tipo de utilizador (Bibliotecário ou Membro)
         * Implementa controlo de acesso obrigatório
         */
        public string UserType { get; set; } = string.Empty;

        /**
         * Lista de roles associadas ao utilizador
         * Suporta controlo de acesso diferenciado conforme requisitos
         */
        public List<string> Roles { get; set; } = new();

        /**
         * Data e hora de expiração do token JWT
         */
        public DateTime ExpiresAt { get; set; }
    }
}
