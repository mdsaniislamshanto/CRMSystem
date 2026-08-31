namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerPerformanceOverviewViewModel
    {
        // =====================================================
        // Top Performer
        // =====================================================

        public SalesOfficerPerformanceViewModel? TopPerformer { get; set; }


        // =====================================================
        // Needs Attention
        // =====================================================

        public SalesOfficerPerformanceViewModel? NeedsAttention { get; set; }


        // =====================================================
        // Performance Trend
        // =====================================================

        public List<PerformanceTrendViewModel> PerformanceTrend { get; set; }
            = new();
    }


  
}