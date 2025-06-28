// Controllers/Api/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Controllers.Api
{
    /// <summary>
    /// API para autenticação e geração de tokens JWT
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly LibraryContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            LibraryContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Autenticar utilizador e obter token JWT
        /// </summary>
        /// <param name="loginDto">Credenciais de login</param>
        /// <returns>Token JWT para acesso à API</returns>
        /// <response code="200">Login bem-sucedido, token gerado</response>
        /// <response code="401">Credenciais inválidas</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar credenciais
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Credenciais inválidas" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Credenciais inválidas" });
            }

            // Obter roles do utilizador
            var roles = await _userManager.GetRolesAsync(user);

            // Gerar token JWT
            var token = GenerateJwtToken(user, roles);

            // Obter informações adicionais do utilizador
            string userType = "Utilizador";
            string userName = user.Email;

            if (roles.Contains("Bibliotecário"))
            {
                userType = "Bibliotecário";
                userName = "Administrador";
            }
            else if (roles.Contains("Membro"))
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
                if (member != null)
                {
                    userName = member.Name;
                    userType = "Membro";
                }
            }

            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = userName,
                UserType = userType,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }

        /// <summary>
        /// Gerar token JWT para utilizador autenticado
        /// </summary>
        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? "MinhaChaveSecretaSuperSeguraParaJWT123456789");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Adicionar roles como claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "LibraryManagementSystem",
                Audience = _configuration["Jwt:Audience"] ?? "LibraryManagementSystem"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    /// <summary>
    /// DTO para pedido de login
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Email do utilizador
        /// </summary>
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password do utilizador
        /// </summary>
        [Required(ErrorMessage = "Password é obrigatória")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de login
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// Token JWT para autenticação
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Email do utilizador
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nome do utilizador
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de utilizador
        /// </summary>
        public string UserType { get; set; } = string.Empty;

        /// <summary>
        /// Roles do utilizador
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Data de expiração do token
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
