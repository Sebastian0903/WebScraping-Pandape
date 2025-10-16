using Application.DTOs;
using FluentValidation;

namespace Application.Features.Todos.Validators
{
    public class UpdateTodoDtoValidator : AbstractValidator<UpdateTodoDto>
    {
        public UpdateTodoDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es requerido")
                .MaximumLength(200).WithMessage("El título no puede exceder los 200 caracteres");
        }
    }
}