using Application.DTOs;
using MediatR;

namespace Application.Features.Todos.Commands.CreateTodo
{
    public record CreateTodoCommand(string Title) : IRequest<TodoDto>;
}