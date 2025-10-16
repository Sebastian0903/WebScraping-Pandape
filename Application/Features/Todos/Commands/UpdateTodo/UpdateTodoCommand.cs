using Application.DTOs;
using MediatR;

namespace Application.Features.Todos.Commands.UpdateTodo
{
    public record UpdateTodoCommand : IRequest<TodoDto>
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsDone { get; init; }
    }
}