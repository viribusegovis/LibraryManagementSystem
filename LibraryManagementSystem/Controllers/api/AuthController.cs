using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using LibraryManagementSystem.Models.DTOs;

namespace LibraryManagementSystem.Controllers.Api
{
    /// <summary>
    /// Controlador API responsável pela autenticação de utilizadores e geração de tokens JWT
    /// Sistema de Gestão de Biblioteca - Desenvolvimento Web
    /// Licenciatura em Engenharia Informática - Instituto Politécnico de Tomar
    /// </summary>
    /// <remarks>
    /// Este controlador implementa o sistema de autenticação JWT para a API REST,
    /// permitindo que utilizadores (Bibliotecários e Membros) obtenham tokens de acesso
    /// para operações protegidas. Suporta autenticação baseada em roles do ASP.NET Identity.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        #region Campos Privados e Dependências

        /// <summary>
        /// Gestor de utilizadores do ASP.NET Identity para operações de autenticação
        /// </summary>
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// Gestor de sessões do ASP.NET Identity para validação de credenciais
        /// </summary>
        private readonly SignInManager<IdentityUser> _signInManager;

        /// <summary>
        /// Contexto da base de dados Entity Framework para acesso aos dados da biblioteca
        /// </summary>
        private readonly LibraryContext _context;

        /// <summary>
        /// Configuração da aplicação para acesso às definições JWT
        /// </summary>
        private readonly IConfiguration _configuration;

        #endregion

        #region Construtor

        /// <summary>
        /// Inicializa uma nova instância do controlador de autenticação
        /// </summary>
        /// <param name="userManager">Gestor de utilizadores para operações de Identity</param>
        /// <param name="signInManager">Gestor de sessões para validação de credenciais</param>
        /// <param name="context">Contexto da base de dados para acesso aos dados</param>
        /// <param name="configuration">Configuração da aplicação para definições JWT</param>
        /// <exception cref="ArgumentNullException">Lançada quando qualquer parâmetro é nulo</exception>
        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            LibraryContext context,
            IConfiguration configuration)
        {
            // Validação de parâmetros obrigatórios para funcionamento correto
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #endregion

        #region Endpoints Públicos da API

        /// <summary>
        /// Endpoint para autenticação de utilizadores e obtenção de token JWT
        /// </summary>
        /// <param name="loginDto">Objeto contendo as credenciais de login (email e password)</param>
        /// <returns>
        /// Token JWT válido com informações do utilizador em caso de sucesso,
        /// ou mensagem de erro em caso de credenciais inválidas
        /// </returns>
        /// <response code="200">
        /// Autenticação bem-sucedida. Retorna token JWT, informações do utilizador e data de expiração.
        /// O token deve ser incluído no cabeçalho Authorization como "Bearer {token}" para acesso a endpoints protegidos.
        /// </response>
        /// <response code="400">
        /// Dados de entrada inválidos. Verifica se o email está no formato correto e se a password foi fornecida.
        /// </response>
        /// <response code="401">
        /// Credenciais inválidas. Email não encontrado ou password incorreta.
        /// Por segurança, a mesma mensagem é retornada para ambos os casos.
        /// </response>
        /// <response code="500">
        /// Erro interno do servidor durante o processo de autenticação.
        /// </response>
        /// <remarks>
        /// Este endpoint implementa o fluxo de autenticação JWT para a API REST:
        /// 
        /// 1. Valida o formato dos dados de entrada
        /// 2. Verifica se o utilizador existe na base de dados
        /// 3. Valida a password fornecida
        /// 4. Obtém as roles/permissões do utilizador
        /// 5. Gera um token JWT válido por 24 horas
        /// 6. Retorna informações completas do utilizador autenticado
        /// 
        /// Tipos de utilizadores suportados:
        /// - Bibliotecário: Acesso completo a todas as operações CRUD
        /// - Membro: Acesso limitado a operações de leitura e criação de avaliações
        /// 
        /// Exemplo de utilização:
        /// POST /api/auth/login
        /// {
        ///   "email": "admin@biblioteca.pt",
        ///   "password": "Admin123!"
        /// }
        /// 
        /// Resposta esperada:
        /// {
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "email": "admin@biblioteca.pt",
        ///   "name": "Administrador",
        ///   "userType": "Bibliotecário",
        ///   "roles": ["Bibliotecário"],
        ///   "expiresAt": "2025-06-29T14:00:00Z"
        /// }
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Validação inicial dos dados de entrada usando Data Annotations
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Dados de entrada inválidos",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Tentativa de localização do utilizador por email na base de dados
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    // Por segurança, retorna a mesma mensagem para email inexistente
                    return Unauthorized(new { message = "Credenciais inválidas" });
                }

                // Verificação da password sem bloquear a conta (lockoutOnFailure: false)
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    // Retorna erro genérico por segurança (não revela se email existe)
                    return Unauthorized(new { message = "Credenciais inválidas" });
                }

                // Obtenção das roles/permissões associadas ao utilizador autenticado
                var roles = await _userManager.GetRolesAsync(user);

                // Geração do token JWT com as informações do utilizador e roles
                var token = GenerateJwtToken(user, roles);

                // Determinação do tipo de utilizador e nome para exibição
                string userType = "Utilizador";
                string userName = user.Email ?? "Utilizador Desconhecido";

                // Lógica específica para diferentes tipos de utilizadores
                if (roles.Contains("Bibliotecário"))
                {
                    userType = "Bibliotecário";
                    userName = "Administrador do Sistema";
                }
                else if (roles.Contains("Membro"))
                {
                    // Para membros, busca informações adicionais na tabela Members
                    var member = await _context.Members
                        .FirstOrDefaultAsync(m => m.UserId == user.Id);

                    if (member != null)
                    {
                        userName = member.Name;
                        userType = "Membro da Biblioteca";
                    }
                }

                // Construção da resposta com todas as informações necessárias
                var response = new LoginResponseDto
                {
                    Token = token,
                    Email = user.Email ?? string.Empty,
                    Name = userName,
                    UserType = userType,
                    Roles = roles.ToList(),
                    ExpiresAt = DateTime.UtcNow.AddHours(24) // Token válido por 24 horas
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log do erro seria implementado aqui em ambiente de produção
                return StatusCode(500, new
                {
                    message = "Erro interno do servidor durante a autenticação",
                    details = ex.Message
                });
            }
        }

        #endregion

        #region Métodos Privados de Apoio

        /// <summary>
        /// Gera um token JWT (JSON Web Token) para o utilizador autenticado
        /// </summary>
        /// <param name="user">Utilizador autenticado do ASP.NET Identity</param>
        /// <param name="roles">Lista de roles/permissões associadas ao utilizador</param>
        /// <returns>
        /// String contendo o token JWT assinado e codificado, válido por 24 horas
        /// </returns>
        /// <remarks>
        /// Este método implementa a geração de tokens JWT seguindo as melhores práticas:
        /// 
        /// 1. Utiliza chave secreta configurável para assinatura
        /// 2. Inclui claims standard (NameIdentifier, Name, Email, etc.)
        /// 3. Adiciona roles como claims para autorização
        /// 4. Define tempo de expiração de 24 horas
        /// 5. Utiliza algoritmo HMAC SHA-256 para assinatura
        /// 
        /// Claims incluídas no token:
        /// - NameIdentifier: ID único do utilizador
        /// - Name: Nome de utilizador ou email
        /// - Email: Endereço de email do utilizador
        /// - Role: Todas as roles associadas (Bibliotecário, Membro, etc.)
        /// - Jti: ID único do token para rastreabilidade
        /// - Iat: Timestamp de criação do token
        /// 
        /// O token gerado deve ser incluído no cabeçalho Authorization
        /// das requisições HTTP como: "Bearer {token}"
        /// </remarks>
        /// <exception cref="SecurityTokenException">
        /// Lançada quando há erro na criação ou assinatura do token
        /// </exception>
        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            // Inicialização do handler para criação e manipulação de tokens JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            // Obtenção da chave secreta da configuração com fallback para desenvolvimento
            var secretKey = _configuration["Jwt:SecretKey"] ?? "MinhaChaveSecretaSuperSeguraParaJWT123456789";
            var key = Encoding.ASCII.GetBytes(secretKey);

            // Construção da lista de claims (declarações) do utilizador
            var claims = new List<Claim>
            {
                // Identificador único do utilizador no sistema
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                
                // Nome do utilizador (username ou email como fallback)
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "Utilizador"),
                
                // Endereço de email do utilizador
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                
                // ID único do token para rastreabilidade e invalidação
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // Timestamp de criação do token (Issued At)
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Adição das roles do utilizador como claims para autorização
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Configuração do descritor do token com todas as propriedades necessárias
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                // Identidade do utilizador com todas as claims
                Subject = new ClaimsIdentity(claims),

                // Data de expiração do token (24 horas a partir da criação)
                Expires = DateTime.UtcNow.AddHours(24),

                // Credenciais de assinatura usando HMAC SHA-256
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),

                // Emissor do token (aplicação que criou o token)
                Issuer = _configuration["Jwt:Issuer"] ?? "LibraryManagementSystem",

                // Audiência do token (aplicação que vai consumir o token)
                Audience = _configuration["Jwt:Audience"] ?? "LibraryManagementSystem"
            };

            // Criação e serialização do token JWT
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        #endregion
    }
}
