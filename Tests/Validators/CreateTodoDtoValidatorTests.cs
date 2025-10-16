using Application.Features.Todos.Commands.CreateTodo;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests.Validators
{
    public class CreateTodoCommandValidatorTests
    {
        private readonly CreateTodoCommandValidator _validator;

        public CreateTodoCommandValidatorTests()
        {
            _validator = new CreateTodoCommandValidator();
        }

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new CreateTodoCommand("Valid Todo Title");

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
            var command = new CreateTodoCommand("");

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
            var command = new CreateTodoCommand(null!);

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
            var longTitle = new string('A', 201); // El límite real es 200 caracteres
            var command = new CreateTodoCommand(longTitle);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(c => c.Title);
        }
    }
}