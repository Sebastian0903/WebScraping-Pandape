using Application.DTOs;
using Application.Features.Todos.Commands.CreateTodo;
using Application.Features.Todos.Commands.DeleteTodo;
using Application.Features.Todos.Commands.UpdateTodo;
using Application.Features.Todos.Queries.GetTodos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace Api.Controllers
{
	[ApiController]
	[Route("api/v1/[controller]")]
	[Authorize]
	public class TodosController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly ILogger<TodosController> _logger;

		public TodosController(IMediator mediator, ILogger<TodosController> logger)
		{
			_mediator = mediator;
			_logger = logger;
		}

		/// <summary>
		/// Obtiene todos los todos
		/// </summary>
		/// <returns>Lista de todos</returns>
		[HttpGet]
		[ProducesResponseType(typeof(List<TodoDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<List<TodoDto>>> Get()
		{
			_logger.LogInformation("Iniciando obtención de todos los todos");
			// Punto de interrupción aquí para depuración
			var result = await _mediator.Send(new GetTodosQuery());
			_logger.LogInformation("Se obtuvieron {Count} todos", result.Count);
			return result;
		}

		/// <summary>
		/// Crea un nuevo todo
		/// </summary>
		/// <param name="command">Datos del todo</param>
		/// <returns>Todo creado</returns>
		[HttpPost]
		[ProducesResponseType(typeof(TodoDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoCommand command)
		{
			var result = await _mediator.Send(command);
			return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
		}

		/// <summary>
		/// Actualiza un todo existente
		/// </summary>
		/// <param name="id">ID del todo</param>
		/// <param name="command">Datos actualizados</param>
		/// <returns>Todo actualizado</returns>
		[HttpPut("{id}")]
		[ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<TodoDto>> Update(int id, [FromBody] UpdateTodoCommand command)
		{
			if (id != command.Id)
			{
				return BadRequest("El ID de la ruta no coincide con el ID del objeto");
			}

			var result = await _mediator.Send(command);
			return Ok(result);
		}

		/// <summary>
		/// Elimina un todo existente
		/// </summary>
		/// <param name="id">ID del todo a eliminar</param>
		/// <returns>Sin contenido</returns>
		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Delete(int id)
		{
			await _mediator.Send(new DeleteTodoCommand(id));
			return NoContent();
		}
	}
}
