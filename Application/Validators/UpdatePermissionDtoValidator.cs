using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UpdatePermissionDtoValidator : AbstractValidator<UpdatePermissionDto>
    {
        public UpdatePermissionDtoValidator()
        {
            RuleFor(x => x.Name)
                .Length(3, 50).WithMessage("El nombre del permiso debe tener entre 3 y 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Code)
                .Length(3, 50).WithMessage("El código del permiso debe tener entre 3 y 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Code));

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("La categoría no puede exceder los 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Category));
        }
    }
}