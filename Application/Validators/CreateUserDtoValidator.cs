using Application.DTOs;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
                .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder los 100 caracteres");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("El número de documento es obligatorio")
                .Matches("^[0-9]+$").WithMessage("El número de documento debe contener solo dígitos numéricos")
                .MaximumLength(20).WithMessage("El número de documento no puede exceder los 20 caracteres");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria")
                .Length(6, 100).WithMessage("La contraseña debe tener entre 6 y 100 caracteres");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("El rol es obligatorio");
        }
    }
}