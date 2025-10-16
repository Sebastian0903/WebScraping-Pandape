using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Application.Mappings;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Registrar todos los validadores de FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            // Registrar MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            
            // Registrar AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));
            
            return services;
        }
    }
}