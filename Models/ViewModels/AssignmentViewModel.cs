using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class AssignmentViewModel
    {
        [Required(ErrorMessage = "Please select a lead.")]
        public long LeadId { get; set; }

        [Required(ErrorMessage = "Please select a sales officer.")]
        public long SalesOfficerId { get; set; }
    }
}