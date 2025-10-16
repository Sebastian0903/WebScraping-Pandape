using FluentValidation;

namespace Application.Features.Todos.Commands.DeleteTodo
{
    public class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
    {
        public DeleteTodoCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("El ID es requerido");
        }
    }
}