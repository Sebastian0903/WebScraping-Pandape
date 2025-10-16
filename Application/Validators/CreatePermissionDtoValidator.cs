using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
    {
        public CreatePermissionDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del permiso es obligatorio")
                .Length(3, 50).WithMessage("El nombre del permiso debe tener entre 3 y 50 caracteres");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del permiso es obligatorio")
                .Length(3, 50).WithMessage("El código del permiso debe tener entre 3 y 50 caracteres");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres");

            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("La categoría no puede exceder los 50 caracteres");
        }
    }
}