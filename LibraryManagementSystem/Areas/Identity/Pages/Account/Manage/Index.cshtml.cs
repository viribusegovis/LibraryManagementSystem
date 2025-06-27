// Areas/Identity/Pages/Account/Manage/Index.cshtml.cs - Phone number made optional
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagementSystem.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly LibraryContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            LibraryContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome completo é obrigatório")]
            [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres")]
            [Display(Name = "Nome Completo")]
            public string FullName { get; set; }

            // FIXED: Made phone number optional by removing [Required] attribute
            [StringLength(15, ErrorMessage = "O telefone não pode exceder 15 caracteres")]
            [Phone(ErrorMessage = "Número de telefone inválido")]
            [Display(Name = "Telefone")]
            public string? PhoneNumber { get; set; } // Made nullable

            [StringLength(200, ErrorMessage = "A morada não pode exceder 200 caracteres")]
            [Display(Name = "Morada")]
            public string? Address { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Data de Nascimento")]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            Username = userName;

            // Load member information from database
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            Input = new InputModel
            {
                FullName = member?.Name ?? "",
                PhoneNumber = phoneNumber, // Can be null/empty
                Address = member?.Address ?? "",
                DateOfBirth = member?.DateOfBirth,
                Email = email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o utilizador com ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Input.Email");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o utilizador com ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update phone number in Identity (can be null/empty)
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                // FIXED: Handle null/empty phone numbers properly
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Erro inesperado ao tentar definir o número de telefone.";
                    return RedirectToPage();
                }
            }

            // Update member information in database
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (member != null)
            {
                member.Name = Input.FullName;
                member.Phone = Input.PhoneNumber; // Can be null/empty
                member.Address = Input.Address;
                member.DateOfBirth = Input.DateOfBirth;
                member.Email = Input.Email.IsNullOrEmpty()? user.Email : Input.Email;

                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "O seu perfil foi atualizado com sucesso.";
            return RedirectToPage();
        }
    }
}
