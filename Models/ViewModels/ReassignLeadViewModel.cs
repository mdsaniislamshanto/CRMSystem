using CRMSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class ReassignLeadViewModel
    {
        public long LeadId { get; set; }

        public long AssignmentId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string CurrentSalesOfficer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a Sales Officer.")]
        public long NewSalesOfficerId { get; set; }

        public List<SelectListItem> SalesOfficers { get; set; } = new();

        [Required(ErrorMessage = "Please select a Status.")]
        public LeadStatus Status { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}