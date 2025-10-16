namespace Application.DTOs
{
    public class TodoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }

    public class CreateTodoDto
    {
        public string Title { get; set; } = string.Empty;
    }

    public class UpdateTodoDto
    {
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
    }
}