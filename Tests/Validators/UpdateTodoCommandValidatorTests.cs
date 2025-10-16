using Application.Features.Todos.Commands.UpdateTodo;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests.Validators
{
    public class UpdateTodoCommandValidatorTests
    {
        private readonly UpdateTodoCommandValidator _validator;

        public UpdateTodoCommandValidatorTests()
        {
            _validator = new UpdateTodoCommandValidator();
        }

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 1, Title = "Valid Todo Title", IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyTitle_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 1, Title = "", IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Title);
        }

        [Fact]
        public void Validate_WithNullTitle_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 1, Title = null!, IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Title);
        }

        [Fact]
        public void Validate_WithTooLongTitle_ShouldHaveValidationError()
        {
            // Arrange
            var longTitle = new string('A', 101); // Asumiendo que el límite es 100 caracteres
            var command = new UpdateTodoCommand { Id = 1, Title = longTitle, IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Title);
        }

        [Fact]
        public void Validate_WithInvalidId_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 0, Title = "Valid Title", IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Id);
        }

        [Fact]
        public void Validate_WithNegativeId_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = -1, Title = "Valid Title", IsDone = true };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Id);
        }
    }
}