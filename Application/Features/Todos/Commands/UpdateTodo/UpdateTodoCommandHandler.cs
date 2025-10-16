using Application.DTOs;
using AutoMapper;
using Domain.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Todos.Commands.UpdateTodo
{
    public class UpdateTodoCommandHandler : IRequestHandler<UpdateTodoCommand, TodoDto>
    {
        private readonly ITodoRepository _todoRepository;
        private readonly IMapper _mapper;

        public UpdateTodoCommandHandler(ITodoRepository todoRepository, IMapper mapper)
        {
            _todoRepository = todoRepository;
            _mapper = mapper;
        }

        public async Task<TodoDto> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
        {
            var todo = await _todoRepository.GetByIdAsync(request.Id);

            if (todo == null)
            {
                throw new KeyNotFoundException($"Todo con ID {request.Id} no encontrado");
            }

            todo.Title = request.Title;
            todo.IsDone = request.IsDone;

            await _todoRepository.UpdateAsync(todo);

            return _mapper.Map<TodoDto>(todo);
        }
    }
}