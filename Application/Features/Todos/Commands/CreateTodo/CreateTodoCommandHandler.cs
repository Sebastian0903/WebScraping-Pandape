using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Todos.Commands.CreateTodo
{
    public class CreateTodoCommandHandler : IRequestHandler<CreateTodoCommand, TodoDto>
    {
        private readonly ITodoRepository _todoRepository;
        private readonly IMapper _mapper;

        public CreateTodoCommandHandler(ITodoRepository todoRepository, IMapper mapper)
        {
            _todoRepository = todoRepository;
            _mapper = mapper;
        }

        public async Task<TodoDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
        {
            var todo = new Todo
            {
                Title = request.Title,
                IsDone = false
            };

            var createdTodo = await _todoRepository.AddAsync(todo);

            return _mapper.Map<TodoDto>(createdTodo);
        }
    }
}