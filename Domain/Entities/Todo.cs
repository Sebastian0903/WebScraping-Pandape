namespace Domain.Entities
{
    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public bool IsDone { get; set; }
    }
}