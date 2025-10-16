using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
    {
        public UpdateRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .Length(3, 50).WithMessage("El nombre del rol debe tener entre 3 y 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}