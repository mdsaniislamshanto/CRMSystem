namespace CRMSystem.Services.Interfaces
{
    public interface IAutoAssignmentService     
    {
        Task AutoAssignLeadAsync(long leadId, long? assignedBy = null);
    }
}
