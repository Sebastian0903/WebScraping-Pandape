using Application.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Username));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder los 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.DocumentNumber)
                .Matches("^[0-9]+$").WithMessage("El número de documento debe contener solo dígitos numéricos")
                .MaximumLength(20).WithMessage("El número de documento no puede exceder los 20 caracteres")
                .When(x => !string.IsNullOrEmpty(x.DocumentNumber));

            RuleFor(x => x.Password)
                .Length(6, 100).WithMessage("La contraseña debe tener entre 6 y 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Password));
        }
    }
}