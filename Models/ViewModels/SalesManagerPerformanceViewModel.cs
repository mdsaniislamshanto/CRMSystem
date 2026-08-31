namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerPerformanceViewModel
    {
        // =====================================================
        // Filter
        // =====================================================

        public PerformanceFilterViewModel Filter { get; set; }
            = new();


        // =====================================================
        // Sales Officer Performance
        // =====================================================

        public List<SalesOfficerPerformanceViewModel> Performance { get; set; }
            = new();


        // =====================================================
        // Top Performer
        // =====================================================

        public PerformanceHighlightViewModel? TopPerformer { get; set; }


        // =====================================================
        // Needs Attention
        // =====================================================

        public PerformanceHighlightViewModel? NeedsAttention { get; set; }
    }
}