using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity to DTO
            CreateMap<Todo, TodoDto>();

            // DTO to Entity
            CreateMap<CreateTodoDto, Todo>();
            CreateMap<UpdateTodoDto, Todo>();
        }
    }
}