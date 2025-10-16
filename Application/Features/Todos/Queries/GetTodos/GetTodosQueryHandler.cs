using Application.DTOs;
using AutoMapper;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Todos.Queries.GetTodos
{
    public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, List<TodoDto>>
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public GetTodosQueryHandler(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<TodoDto>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
        {
            var todos = await _context.Todos
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<TodoDto>>(todos);
        }
    }
}