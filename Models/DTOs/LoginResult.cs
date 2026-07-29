using CRMSystem.Models.Entities;

namespace CRMSystem.Models.DTOs
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public User? User { get; set; }
    }
}