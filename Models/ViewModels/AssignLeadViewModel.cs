using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRMSystem.Models.ViewModels
{
    public class AssignLeadViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a Sales Officer.")]
        public long SalesOfficerId { get; set; }

        public List<SelectListItem> SalesOfficers { get; set; } = new();
    }
}