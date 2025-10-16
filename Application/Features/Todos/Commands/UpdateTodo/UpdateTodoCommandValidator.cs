using FluentValidation;

namespace Application.Features.Todos.Commands.UpdateTodo
{
    public class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
    {
        public UpdateTodoCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("El ID es requerido");

            RuleFor(v => v.Title)
                .NotEmpty().WithMessage("El título es requerido")
                .MaximumLength(200).WithMessage("El título no puede exceder los 200 caracteres");
        }
    }
}