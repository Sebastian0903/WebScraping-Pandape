using FluentValidation;

namespace Application.Features.Todos.Commands.CreateTodo
{
    public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
    {
        public CreateTodoCommandValidator()
        {
            RuleFor(v => v.Title)
                .NotEmpty().WithMessage("El título es requerido")
                .MaximumLength(200).WithMessage("El título no puede exceder los 200 caracteres");
        }
    }
}