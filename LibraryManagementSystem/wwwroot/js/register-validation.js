// wwwroot/js/register-validation.js
document.addEventListener('DOMContentLoaded', function () {
    // Enhanced form validation
    const form = document.getElementById('registerForm');
    const submitBtn = document.getElementById('registerSubmit');

    // Add loading state to submit button
    if (form && submitBtn) {
        form.addEventListener('submit', function () {
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>A Criar Conta...';
            submitBtn.disabled = true;
        });
    }

    // Email validation enhancement
    const emailInput = document.querySelector('input[name="Input.Email"]');
    if (emailInput) {
        emailInput.addEventListener('blur', function () {
            const email = this.value;
            if (email && !isValidEmail(email)) {
                this.setCustomValidity('Por favor, digite um email válido');
            } else {
                this.setCustomValidity('');
            }
        });
    }

    // Password strength indicator
    const passwordInput = document.querySelector('input[name="Input.Password"]');
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            const password = this.value;
            const strength = calculatePasswordStrength(password);
            updatePasswordStrengthIndicator(strength);
        });
    }

    // Form field animations
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(function (input) {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'scale(1.02)';
            this.parentElement.style.transition = 'transform 0.2s ease';
        });

        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'scale(1)';
        });
    });
});

// Email validation using proper regex (safe in external file)
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Password strength calculation
function calculatePasswordStrength(password) {
    let strength = 0;
    if (password.length >= 6) strength++;
    if (password.match(/[a-z]/)) strength++;
    if (password.match(/[A-Z]/)) strength++;
    if (password.match(/[0-9]/)) strength++;
    if (password.match(/[^a-zA-Z0-9]/)) strength++;
    return strength;
}

// Password strength indicator
function updatePasswordStrengthIndicator(strength) {
    const passwordInput = document.querySelector('input[name="Input.Password"]');
    if (!passwordInput) return;

    // Remove existing strength indicator
    const existingIndicator = document.querySelector('.password-strength-indicator');
    if (existingIndicator) {
        existingIndicator.remove();
    }

    // Create strength indicator
    const indicator = document.createElement('div');
    indicator.className = 'password-strength-indicator mt-1';

    let strengthText = '';
    let strengthClass = '';

    switch (strength) {
        case 0:
        case 1:
            strengthText = 'Muito fraca';
            strengthClass = 'text-danger';
            break;
        case 2:
            strengthText = 'Fraca';
            strengthClass = 'text-warning';
            break;
        case 3:
            strengthText = 'Média';
            strengthClass = 'text-info';
            break;
        case 4:
            strengthText = 'Forte';
            strengthClass = 'text-success';
            break;
        case 5:
            strengthText = 'Muito forte';
            strengthClass = 'text-success fw-bold';
            break;
    }

    if (passwordInput.value) {
        indicator.innerHTML = '<small class="' + strengthClass + '">Força da palavra-passe: ' + strengthText + '</small>';
        passwordInput.parentElement.appendChild(indicator);
    }
}
