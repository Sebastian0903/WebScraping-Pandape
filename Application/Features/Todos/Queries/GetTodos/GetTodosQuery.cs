using Application.DTOs;
using MediatR;

namespace Application.Features.Todos.Queries.GetTodos
{
    public record GetTodosQuery : IRequest<List<TodoDto>>;
}