using MediatR;

namespace Application.Features.Todos.Commands.DeleteTodo
{
    public record DeleteTodoCommand(int Id) : IRequest<Unit>;
}