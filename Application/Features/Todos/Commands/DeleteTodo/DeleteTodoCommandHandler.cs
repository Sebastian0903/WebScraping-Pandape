using Domain.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Todos.Commands.DeleteTodo
{
    public class DeleteTodoCommandHandler : IRequestHandler<DeleteTodoCommand, Unit>
    {
        private readonly ITodoRepository _todoRepository;

        public DeleteTodoCommandHandler(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }

        public async Task<Unit> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
        {
            var todo = await _todoRepository.GetByIdAsync(request.Id);

            if (todo == null)
            {
                throw new KeyNotFoundException($"Todo con ID {request.Id} no encontrado");
            }

            await _todoRepository.DeleteAsync(request.Id);

            return Unit.Value;
        }
    }
}