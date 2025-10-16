using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UserLoginDtoValidator : AbstractValidator<UserLoginDto>
    {
        public UserLoginDtoValidator()
        {
            RuleFor(x => x.UsernameOrDocumentNumber)
                .NotEmpty().WithMessage("El nombre de usuario o número de documento es obligatorio");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria");
        }
    }
}