using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.Entities
{
    public class GoogleOAuthCredential : BaseEntity
    {
        [Key]
        public long CredentialId { get; set; }

        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = "GoogleForms";

        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime? TokenExpiry { get; set; }
    }
}