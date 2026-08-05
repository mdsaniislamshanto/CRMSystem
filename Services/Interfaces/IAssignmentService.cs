using CRMSystem.Models.DTOs;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<ServiceResult> AssignLeadAsync(AssignmentViewModel model);
    }
}