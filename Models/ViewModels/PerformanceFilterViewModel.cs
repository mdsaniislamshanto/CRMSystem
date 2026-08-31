namespace CRMSystem.Models.ViewModels
{
    public class PerformanceFilterViewModel
    {
        // =====================================================
        // Selected Date Range
        // =====================================================

        public string Range { get; set; } = "ThisMonth";


        // =====================================================
        // Custom Date Range
        // =====================================================

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}