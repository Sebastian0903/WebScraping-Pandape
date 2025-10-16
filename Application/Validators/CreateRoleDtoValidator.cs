using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
    {
        public CreateRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del rol es obligatorio")
                .Length(3, 50).WithMessage("El nombre del rol debe tener entre 3 y 50 caracteres");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres");
        }
    }
}