using System;

namespace Tests.Models
{
    public class ErrorResponse
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}