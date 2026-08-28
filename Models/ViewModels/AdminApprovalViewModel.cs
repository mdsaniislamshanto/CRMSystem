using System.Collections.Generic;

namespace CRMSystem.Models.ViewModels
{
    public class AdminApprovalViewModel
    {
        // =====================================================
        // Pending Profile Change Requests
        // =====================================================

        public List<ProfileChangeRequestViewModel>
            ProfileChangeRequests
        { get; set; }
            = new();


        // =====================================================
        // Pending Auto Assignment Requests
        // =====================================================

        public List<AutoAssignmentRequestViewModel>
            AutoAssignmentRequests
        { get; set; }
            = new();
    }
}