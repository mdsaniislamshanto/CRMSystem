using ClosedXML.Excel;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CRMSystem.Services
{
    public class PerformanceExportService : IPerformanceExportService
    {
        private readonly ISalesOfficerPerformanceService _performanceService;

        public PerformanceExportService(
            ISalesOfficerPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }


        // =========================================================
        // EXPORT SALES OFFICER PERFORMANCE TO EXCEL
        // =========================================================

        public async Task<byte[]> ExportPerformanceToExcelAsync(
            PerformanceFilterViewModel? filter = null)
        {
            var performanceList =
                await _performanceService.GetPerformanceAsync(filter);

            using var workbook = new XLWorkbook();


            // -----------------------------------------------------
            // PERFORMANCE SUMMARY SHEET
            // -----------------------------------------------------

            var summarySheet =
                workbook.Worksheets.Add("Performance Summary");

            BuildPerformanceSummarySheet(
                summarySheet,
                performanceList,
                filter);


            // -----------------------------------------------------
            // SALES OFFICER RANKING SHEET
            // -----------------------------------------------------

            var rankingSheet =
                workbook.Worksheets.Add("Sales Officer Ranking");

            BuildRankingSheet(
                rankingSheet,
                performanceList,
                filter);


            // -----------------------------------------------------
            // SAVE WORKBOOK
            // -----------------------------------------------------

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }


        // =========================================================
        // PERFORMANCE SUMMARY SHEET
        // =========================================================

        private static void BuildPerformanceSummarySheet(
            IXLWorksheet worksheet,
            List<SalesOfficerPerformanceViewModel> performanceList,
            PerformanceFilterViewModel? filter)
        {
            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            worksheet.Cell("A1").Value =
                "Sales Officer Performance Report";

            worksheet.Range("A1:R1").Merge();


            worksheet.Cell("A2").Value =
                $"Period: {GetRangeLabel(filter)}";

            worksheet.Range("A2:R2").Merge();


            // -----------------------------------------------------
            // HEADERS
            // -----------------------------------------------------

            const int headerRow = 4;

            var headers = new[]
            {
                "Rank",
                "Sales Officer",
                "Assigned Leads",
                "Accepted Leads",
                "Pending Acceptance",
                "Acceptance Rate",
                "Acceptance SLA",
                "First Feedback SLA",
                "Next Feedback SLA",
                "Completed Leads",
                "Completion Rate",
                "Total Feedbacks",
                "Follow-up On Time",
                "Follow-up Late",
                "Overdue Follow-ups",
                "Follow-up Timeliness",
                "Performance Score",
                "Status"
            };


            for (var column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(
                    headerRow,
                    column + 1)
                    .Value = headers[column];
            }


            // -----------------------------------------------------
            // RANK OFFICERS
            // -----------------------------------------------------

            var rankedList =
                performanceList
                    .OrderByDescending(
                        x => x.PerformanceScore)
                    .ToList();


            // -----------------------------------------------------
            // DATA ROWS
            // -----------------------------------------------------

            var currentRow = headerRow + 1;

            for (var index = 0;
                 index < rankedList.Count;
                 index++)
            {
                var officer = rankedList[index];

                var rank = index + 1;


                worksheet.Cell(
                    currentRow,
                    1).Value = rank;


                worksheet.Cell(
                    currentRow,
                    2).Value =
                    officer.SalesOfficerName;


                worksheet.Cell(
                    currentRow,
                    3).Value =
                    officer.TotalAssignedLeads;


                worksheet.Cell(
                    currentRow,
                    4).Value =
                    officer.AcceptedLeads;


                worksheet.Cell(
                    currentRow,
                    5).Value =
                    officer.PendingAcceptance;


                worksheet.Cell(
                    currentRow,
                    6).Value =
                    officer.AcceptanceRate / 100;


                worksheet.Cell(
                    currentRow,
                    7).Value =
                    officer.AcceptanceSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    8).Value =
                    officer.FirstFeedbackSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    9).Value =
                    officer.NextFeedbackSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    10).Value =
                    officer.CompletedLeads;


                worksheet.Cell(
                    currentRow,
                    11).Value =
                    officer.CompletionRate / 100;


                worksheet.Cell(
                    currentRow,
                    12).Value =
                    officer.TotalFeedbacks;


                worksheet.Cell(
                    currentRow,
                    13).Value =
                    officer.FollowUpsCompletedOnTime;


                worksheet.Cell(
                    currentRow,
                    14).Value =
                    officer.FollowUpsCompletedLate;


                worksheet.Cell(
                    currentRow,
                    15).Value =
                    officer.OverdueFollowUps;


                worksheet.Cell(
                    currentRow,
                    16).Value =
                    officer.FollowUpTimelinessRate / 100;


                worksheet.Cell(
                    currentRow,
                    17).Value =
                    officer.PerformanceScore / 100;


                worksheet.Cell(
                    currentRow,
                    18).Value =
                    GetPerformanceStatus(
                        officer.PerformanceScore);


                currentRow++;
            }


            // -----------------------------------------------------
            // FORMAT SHEET
            // -----------------------------------------------------

            FormatSummarySheet(
                worksheet,
                headerRow,
                currentRow - 1);
        }


        // =========================================================
        // SALES OFFICER RANKING SHEET
        // =========================================================

        private static void BuildRankingSheet(
            IXLWorksheet worksheet,
            List<SalesOfficerPerformanceViewModel> performanceList,
            PerformanceFilterViewModel? filter)
        {
            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            worksheet.Cell("A1").Value =
                "Sales Officer Ranking";

            worksheet.Range("A1:K1").Merge();


            worksheet.Cell("A2").Value =
                $"Ranking Period: {GetRangeLabel(filter)}";

            worksheet.Range("A2:K2").Merge();


            // -----------------------------------------------------
            // HEADERS
            // -----------------------------------------------------

            const int headerRow = 4;

            var headers = new[]
            {
                "Rank",
                "Sales Officer",
                "Performance Score",
                "Completion Rate",
                "Acceptance Rate",
                "Acceptance SLA",
                "First Feedback SLA",
                "Next Feedback SLA",
                "Follow-up Timeliness",
                "Assigned Leads",
                "Completed Leads"
            };


            for (var column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(
                    headerRow,
                    column + 1)
                    .Value = headers[column];
            }


            // -----------------------------------------------------
            // SORT
            // -----------------------------------------------------

            var rankedList =
                performanceList
                    .OrderByDescending(
                        x => x.PerformanceScore)
                    .ToList();


            // -----------------------------------------------------
            // DATA
            // -----------------------------------------------------

            var currentRow = headerRow + 1;

            for (var index = 0;
                 index < rankedList.Count;
                 index++)
            {
                var officer = rankedList[index];

                var rank = index + 1;


                worksheet.Cell(
                    currentRow,
                    1).Value = rank;


                worksheet.Cell(
                    currentRow,
                    2).Value =
                    officer.SalesOfficerName;


                worksheet.Cell(
                    currentRow,
                    3).Value =
                    officer.PerformanceScore / 100;


                worksheet.Cell(
                    currentRow,
                    4).Value =
                    officer.CompletionRate / 100;


                worksheet.Cell(
                    currentRow,
                    5).Value =
                    officer.AcceptanceRate / 100;


                worksheet.Cell(
                    currentRow,
                    6).Value =
                    officer.AcceptanceSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    7).Value =
                    officer.FirstFeedbackSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    8).Value =
                    officer.NextFeedbackSLAComplianceRate / 100;


                worksheet.Cell(
                    currentRow,
                    9).Value =
                    officer.FollowUpTimelinessRate / 100;


                worksheet.Cell(
                    currentRow,
                    10).Value =
                    officer.TotalAssignedLeads;


                worksheet.Cell(
                    currentRow,
                    11).Value =
                    officer.CompletedLeads;


                currentRow++;
            }


            // -----------------------------------------------------
            // FORMAT
            // -----------------------------------------------------

            FormatRankingSheet(
                worksheet,
                headerRow,
                currentRow - 1);
        }


        // =========================================================
        // FORMAT PERFORMANCE SUMMARY EXCEL
        // =========================================================

        private static void FormatSummarySheet(
            IXLWorksheet worksheet,
            int headerRow,
            int lastRow)
        {
            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            var title =
                worksheet.Range("A1:R1");

            title.Style.Font.Bold = true;

            title.Style.Font.FontSize = 18;

            title.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            title.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            worksheet.Row(1).Height = 30;


            // -----------------------------------------------------
            // PERIOD
            // -----------------------------------------------------

            var period =
                worksheet.Range("A2:R2");

            period.Style.Font.Italic = true;

            period.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            var header =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    18);

            header.Style.Font.Bold = true;

            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            header.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            header.Style.Alignment.WrapText = true;

            worksheet.Row(headerRow).Height = 35;


            // -----------------------------------------------------
            // PERCENTAGES
            // -----------------------------------------------------

            if (lastRow >= headerRow + 1)
            {
                worksheet.Range(
                    headerRow + 1,
                    6,
                    lastRow,
                    9)
                    .Style.NumberFormat.Format =
                    "0.00%";


                worksheet.Range(
                    headerRow + 1,
                    11,
                    lastRow,
                    11)
                    .Style.NumberFormat.Format =
                    "0.00%";


                worksheet.Range(
                    headerRow + 1,
                    16,
                    lastRow,
                    17)
                    .Style.NumberFormat.Format =
                    "0.00%";
            }


            // -----------------------------------------------------
            // BORDERS
            // -----------------------------------------------------

            worksheet.Range(
                headerRow,
                1,
                Math.Max(lastRow, headerRow),
                18)
                .Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;


            worksheet.Range(
                headerRow,
                1,
                Math.Max(lastRow, headerRow),
                18)
                .Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;


            // -----------------------------------------------------
            // ALIGNMENT
            // -----------------------------------------------------

            worksheet.Range(
                headerRow + 1,
                1,
                Math.Max(lastRow, headerRow + 1),
                18)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;


            worksheet.Columns()
                .AdjustToContents();


            // -----------------------------------------------------
            // FREEZE HEADER
            // -----------------------------------------------------

            worksheet.SheetView.FreezeRows(headerRow);


            // -----------------------------------------------------
            // FILTER
            // -----------------------------------------------------

            if (lastRow >= headerRow)
            {
                worksheet.Range(
                    headerRow,
                    1,
                    lastRow,
                    18)
                    .SetAutoFilter();
            }
        }


        // =========================================================
        // FORMAT RANKING EXCEL
        // =========================================================

        private static void FormatRankingSheet(
            IXLWorksheet worksheet,
            int headerRow,
            int lastRow)
        {
            // -----------------------------------------------------
            // TITLE
            // -----------------------------------------------------

            var title =
                worksheet.Range("A1:K1");

            title.Style.Font.Bold = true;

            title.Style.Font.FontSize = 18;

            title.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            title.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            worksheet.Row(1).Height = 30;


            // -----------------------------------------------------
            // PERIOD
            // -----------------------------------------------------

            var period =
                worksheet.Range("A2:K2");

            period.Style.Font.Italic = true;

            period.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            var header =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    11);

            header.Style.Font.Bold = true;

            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            header.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            header.Style.Alignment.WrapText = true;

            worksheet.Row(headerRow).Height = 35;


            // -----------------------------------------------------
            // PERCENTAGE FORMAT
            // -----------------------------------------------------

            if (lastRow >= headerRow + 1)
            {
                worksheet.Range(
                    headerRow + 1,
                    3,
                    lastRow,
                    9)
                    .Style.NumberFormat.Format =
                    "0.00%";
            }


            // -----------------------------------------------------
            // BORDERS
            // -----------------------------------------------------

            worksheet.Range(
                headerRow,
                1,
                Math.Max(lastRow, headerRow),
                11)
                .Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;


            worksheet.Range(
                headerRow,
                1,
                Math.Max(lastRow, headerRow),
                11)
                .Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;


            // -----------------------------------------------------
            // ALIGNMENT
            // -----------------------------------------------------

            worksheet.Range(
                headerRow + 1,
                1,
                Math.Max(lastRow, headerRow + 1),
                11)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;


            worksheet.Columns()
                .AdjustToContents();


            // -----------------------------------------------------
            // FREEZE
            // -----------------------------------------------------

            worksheet.SheetView.FreezeRows(headerRow);


            // -----------------------------------------------------
            // FILTER
            // -----------------------------------------------------

            if (lastRow >= headerRow)
            {
                worksheet.Range(
                    headerRow,
                    1,
                    lastRow,
                    11)
                    .SetAutoFilter();
            }
        }


        // =========================================================
        // PERFORMANCE STATUS
        // =========================================================

        private static string GetPerformanceStatus(
            double score)
        {
            if (score >= 80)
            {
                return "Excellent";
            }

            if (score >= 60)
            {
                return "Good";
            }

            if (score >= 40)
            {
                return "Needs Improvement";
            }

            return "Needs Attention";
        }


        // =========================================================
        // RANGE LABEL
        // =========================================================

        private static string GetRangeLabel(
            PerformanceFilterViewModel? filter)
        {
            if (filter == null)
            {
                return "All Time";
            }


            return filter.Range switch
            {
                "Today" =>
                    "Today",

                "ThisWeek" =>
                    "This Week",

                "ThisMonth" =>
                    "This Month",

                "Custom" =>
                    $"Custom Range ({filter.FromDate:dd MMM yyyy} - {filter.ToDate:dd MMM yyyy})",

                _ =>
                    filter.Range ?? "Selected Period"
            };
        }


        // =========================================================
        // EXPORT PERFORMANCE TO PDF
        // =========================================================

        public async Task<byte[]> ExportPerformanceToPdfAsync(
            PerformanceFilterViewModel? filter = null)
        {
            // -----------------------------------------------------
            // GET PERFORMANCE DATA
            // -----------------------------------------------------

            var performanceData =
                await _performanceService.GetPerformanceAsync(filter);


            // -----------------------------------------------------
            // SORT BY PERFORMANCE SCORE
            // -----------------------------------------------------

            var sortedData =
                performanceData
                    .OrderByDescending(
                        x => x.PerformanceScore)
                    .ToList();


            // -----------------------------------------------------
            // PERIOD
            // -----------------------------------------------------

            var periodName =
                GetPerformancePeriodName(filter);


            // -----------------------------------------------------
            // CREATE DOCUMENT
            // -----------------------------------------------------

            var document =
                Document.Create(container =>
                {
                    // =================================================
                    // PAGE 1
                    // PERFORMANCE SUMMARY
                    // =================================================

                    container.Page(page =>
                    {
                        // -------------------------------------------------
                        // A3 LANDSCAPE
                        // -------------------------------------------------
                        //
                        // The summary contains 18 columns.
                        // A3 landscape gives the table enough horizontal
                        // space to remain readable.
                        // -------------------------------------------------

                        page.Size(
                            PageSizes.A3.Landscape());

                        page.MarginHorizontal(30);
                        page.MarginVertical(25);

                        page.DefaultTextStyle(
                            text => text.FontSize(9));


                        // -------------------------------------------------
                        // HEADER
                        // -------------------------------------------------

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .AlignCenter()
                                    .Text("CRM SYSTEM")
                                    .Bold()
                                    .FontSize(12);


                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        "Sales Officer Performance Report")
                                    .Bold()
                                    .FontSize(20);


                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        $"Period: {periodName}")
                                    .Italic()
                                    .FontSize(10);


                                column.Item()
                                    .PaddingBottom(10);
                            });


                        // -------------------------------------------------
                        // CONTENT
                        // -------------------------------------------------

                        page.Content()
                            .Column(column =>
                            {
                                column.Item()
                                    .PaddingBottom(8)
                                    .Text("Performance Summary")
                                    .Bold()
                                    .FontSize(14);


                                if (sortedData.Count == 0)
                                {
                                    column.Item()
                                        .AlignCenter()
                                        .Padding(25)
                                        .Text(
                                            "No performance data available for the selected period.")
                                        .FontSize(12);
                                }
                                else
                                {
                                    column.Item()
                                        .Table(table =>
                                        {
                                            BuildSummaryTable(
                                                table,
                                                sortedData);
                                        });
                                }
                            });


                        // -------------------------------------------------
                        // FOOTER
                        // -------------------------------------------------

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "CRM System • Performance Summary");

                                text.Span("  |  ");

                                text.CurrentPageNumber();

                                text.Span(" / ");

                                text.TotalPages();
                            });
                    });


                    // =================================================
                    // PAGE 2
                    // SALES OFFICER RANKING
                    // =================================================

                    container.Page(page =>
                    {
                        // -------------------------------------------------
                        // A3 LANDSCAPE
                        // -------------------------------------------------

                        page.Size(
                            PageSizes.A3.Landscape());

                        page.MarginHorizontal(30);
                        page.MarginVertical(25);

                        page.DefaultTextStyle(
                            text => text.FontSize(10));


                        // -------------------------------------------------
                        // HEADER
                        // -------------------------------------------------

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .AlignCenter()
                                    .Text("CRM SYSTEM")
                                    .Bold()
                                    .FontSize(12);


                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        "Sales Officer Ranking")
                                    .Bold()
                                    .FontSize(20);


                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        $"Ranking Period: {periodName}")
                                    .Italic()
                                    .FontSize(10);


                                column.Item()
                                    .PaddingBottom(10);
                            });


                        // -------------------------------------------------
                        // CONTENT
                        // -------------------------------------------------

                        page.Content()
                            .Column(column =>
                            {
                                column.Item()
                                    .PaddingBottom(8)
                                    .Text("Performance Ranking")
                                    .Bold()
                                    .FontSize(14);


                                if (sortedData.Count == 0)
                                {
                                    column.Item()
                                        .AlignCenter()
                                        .Padding(25)
                                        .Text(
                                            "No ranking data available for the selected period.")
                                        .FontSize(12);
                                }
                                else
                                {
                                    column.Item()
                                        .Table(table =>
                                        {
                                            BuildRankingPdfTable(
                                                table,
                                                sortedData);
                                        });
                                }
                            });


                        // -------------------------------------------------
                        // FOOTER
                        // -------------------------------------------------

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "CRM System • Sales Officer Ranking");

                                text.Span("  |  ");

                                text.CurrentPageNumber();

                                text.Span(" / ");

                                text.TotalPages();
                            });
                    });
                });


            // -----------------------------------------------------
            // GENERATE PDF
            // -----------------------------------------------------

            return document.GeneratePdf();
        }


        // =========================================================
        // BUILD PDF SUMMARY TABLE
        // =========================================================

        private static void BuildSummaryTable(
            TableDescriptor table,
            List<SalesOfficerPerformanceViewModel> sortedData)
        {
            // -----------------------------------------------------
            // RELATIVE COLUMNS
            // -----------------------------------------------------
            //
            // Every column uses RelativeColumn().
            //
            // This is important because fixed widths previously caused
            // QuestPDF layout conflicts.
            // -----------------------------------------------------

            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.45f); // Rank

                columns.RelativeColumn(2.40f); // Sales Officer

                columns.RelativeColumn(0.85f); // Assigned
                columns.RelativeColumn(0.85f); // Accepted
                columns.RelativeColumn(0.95f); // Pending

                columns.RelativeColumn(1.00f); // Acceptance
                columns.RelativeColumn(1.10f); // Acceptance SLA

                columns.RelativeColumn(1.20f); // First Feedback SLA
                columns.RelativeColumn(1.20f); // Next Feedback SLA

                columns.RelativeColumn(0.95f); // Completed
                columns.RelativeColumn(0.95f); // Completion

                columns.RelativeColumn(0.85f); // Feedbacks

                columns.RelativeColumn(1.10f); // Follow-up On Time
                columns.RelativeColumn(1.00f); // Follow-up Late
                columns.RelativeColumn(0.90f); // Overdue

                columns.RelativeColumn(1.20f); // Follow-up Timeliness
                columns.RelativeColumn(0.95f); // Score
                columns.RelativeColumn(1.25f); // Status
            });


            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            table.Header(header =>
            {
                AddSummaryHeaderCell(
                    header,
                    "Rank");

                AddSummaryHeaderCell(
                    header,
                    "Sales Officer");

                AddSummaryHeaderCell(
                    header,
                    "Assigned");

                AddSummaryHeaderCell(
                    header,
                    "Accepted");

                AddSummaryHeaderCell(
                    header,
                    "Pending");

                AddSummaryHeaderCell(
                    header,
                    "Acceptance");

                AddSummaryHeaderCell(
                    header,
                    "Acceptance SLA");

                AddSummaryHeaderCell(
                    header,
                    "First Feedback SLA");

                AddSummaryHeaderCell(
                    header,
                    "Next Feedback SLA");

                AddSummaryHeaderCell(
                    header,
                    "Completed");

                AddSummaryHeaderCell(
                    header,
                    "Completion");

                AddSummaryHeaderCell(
                    header,
                    "Feedbacks");

                AddSummaryHeaderCell(
                    header,
                    "Follow-up On Time");

                AddSummaryHeaderCell(
                    header,
                    "Follow-up Late");

                AddSummaryHeaderCell(
                    header,
                    "Overdue");

                AddSummaryHeaderCell(
                    header,
                    "Follow-up Timeliness");

                AddSummaryHeaderCell(
                    header,
                    "Score");

                AddSummaryHeaderCell(
                    header,
                    "Status");
            });


            // -----------------------------------------------------
            // DATA
            // -----------------------------------------------------

            var rank = 1;

            foreach (var officer in sortedData)
            {
                AddPdfBodyCell(
                    table,
                    rank.ToString());


                AddPdfBodyCell(
                    table,
                    officer.SalesOfficerName,
                    alignCenter: false);


                AddPdfBodyCell(
                    table,
                    officer.TotalAssignedLeads.ToString());


                AddPdfBodyCell(
                    table,
                    officer.AcceptedLeads.ToString());


                AddPdfBodyCell(
                    table,
                    officer.PendingAcceptance.ToString());


                AddPdfBodyCell(
                    table,
                    $"{officer.AcceptanceRate:F2}%");


                AddPdfBodyCell(
                    table,
                    $"{officer.AcceptanceSLAComplianceRate:F2}%");


                AddPdfBodyCell(
                    table,
                    $"{officer.FirstFeedbackSLAComplianceRate:F2}%");


                AddPdfBodyCell(
                    table,
                    $"{officer.NextFeedbackSLAComplianceRate:F2}%");


                AddPdfBodyCell(
                    table,
                    officer.CompletedLeads.ToString());


                AddPdfBodyCell(
                    table,
                    $"{officer.CompletionRate:F2}%");


                AddPdfBodyCell(
                    table,
                    officer.TotalFeedbacks.ToString());


                AddPdfBodyCell(
                    table,
                    officer.FollowUpsCompletedOnTime.ToString());


                AddPdfBodyCell(
                    table,
                    officer.FollowUpsCompletedLate.ToString());


                AddPdfBodyCell(
                    table,
                    officer.OverdueFollowUps.ToString());


                AddPdfBodyCell(
                    table,
                    $"{officer.FollowUpTimelinessRate:F2}%");


                AddPdfScoreCell(
                    table,
                    $"{officer.PerformanceScore:F2}%");


                AddPdfStatusCell(
                    table,
                    GetPerformanceStatus(
                        officer.PerformanceScore));


                rank++;
            }
        }


        // =========================================================
        // BUILD PDF RANKING TABLE
        // =========================================================

        private static void BuildRankingPdfTable(
            TableDescriptor table,
            List<SalesOfficerPerformanceViewModel> sortedData)
        {
            // -----------------------------------------------------
            // RELATIVE COLUMNS
            // -----------------------------------------------------

            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.55f); // Rank

                columns.RelativeColumn(2.50f); // Sales Officer

                columns.RelativeColumn(1.20f); // Performance Score

                columns.RelativeColumn(1.15f); // Completion Rate

                columns.RelativeColumn(1.15f); // Acceptance Rate

                columns.RelativeColumn(1.15f); // Acceptance SLA

                columns.RelativeColumn(1.35f); // First Feedback SLA

                columns.RelativeColumn(1.35f); // Next Feedback SLA

                columns.RelativeColumn(1.35f); // Follow-up Timeliness

                columns.RelativeColumn(1.00f); // Assigned

                columns.RelativeColumn(1.00f); // Completed
            });


            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------

            table.Header(header =>
            {
                AddRankingHeaderCell(
                    header,
                    "Rank");


                AddRankingHeaderCell(
                    header,
                    "Sales Officer");


                AddRankingHeaderCell(
                    header,
                    "Performance Score");


                AddRankingHeaderCell(
                    header,
                    "Completion Rate");


                AddRankingHeaderCell(
                    header,
                    "Acceptance Rate");


                AddRankingHeaderCell(
                    header,
                    "Acceptance SLA");


                AddRankingHeaderCell(
                    header,
                    "First Feedback SLA");


                AddRankingHeaderCell(
                    header,
                    "Next Feedback SLA");


                AddRankingHeaderCell(
                    header,
                    "Follow-up Timeliness");


                AddRankingHeaderCell(
                    header,
                    "Assigned Leads");


                AddRankingHeaderCell(
                    header,
                    "Completed Leads");
            });


            // -----------------------------------------------------
            // DATA
            // -----------------------------------------------------

            var rank = 1;

            foreach (var officer in sortedData)
            {
                AddRankingBodyCell(
                    table,
                    rank.ToString());


                AddRankingBodyCell(
                    table,
                    officer.SalesOfficerName,
                    alignCenter: false);


                AddRankingScoreCell(
                    table,
                    $"{officer.PerformanceScore:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.CompletionRate:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.AcceptanceRate:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.AcceptanceSLAComplianceRate:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.FirstFeedbackSLAComplianceRate:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.NextFeedbackSLAComplianceRate:F2}%");


                AddRankingBodyCell(
                    table,
                    $"{officer.FollowUpTimelinessRate:F2}%");


                AddRankingBodyCell(
                    table,
                    officer.TotalAssignedLeads.ToString());


                AddRankingBodyCell(
                    table,
                    officer.CompletedLeads.ToString());


                rank++;
            }
        }


        // =========================================================
        // SUMMARY HEADER CELL
        // =========================================================

        private static void AddSummaryHeaderCell(
            TableCellDescriptor header,
            string text)
        {
            header
                .Cell()
                .Element(PdfHeaderCell)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(7.5f);
        }


        // =========================================================
        // RANKING HEADER CELL
        // =========================================================

        private static void AddRankingHeaderCell(
            TableCellDescriptor header,
            string text)
        {
            header
                .Cell()
                .Element(PdfHeaderCell)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(8.5f);
        }


        // =========================================================
        // SUMMARY BODY CELL
        // =========================================================

        private static void AddPdfBodyCell(
            TableDescriptor table,
            string text,
            bool alignCenter = true)
        {
            var cell =
                table.Cell()
                    .Element(PdfBodyCell);


            if (alignCenter)
            {
                cell.AlignCenter();
            }


            cell
                .Text(text)
                .FontSize(8);
        }


        // =========================================================
        // SUMMARY SCORE CELL
        // =========================================================

        private static void AddPdfScoreCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Element(PdfScoreCell)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(8);
        }


        // =========================================================
        // SUMMARY STATUS CELL
        // =========================================================

        private static void AddPdfStatusCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Element(PdfStatusCell)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(7.5f);
        }


        // =========================================================
        // RANKING BODY CELL
        // =========================================================

        private static void AddRankingBodyCell(
            TableDescriptor table,
            string text,
            bool alignCenter = true)
        {
            var cell =
                table.Cell()
                    .Element(PdfRankingBodyCell);


            if (alignCenter)
            {
                cell.AlignCenter();
            }


            cell
                .Text(text)
                .FontSize(9);
        }


        // =========================================================
        // RANKING SCORE CELL
        // =========================================================

        private static void AddRankingScoreCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Element(PdfRankingScoreCell)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(9);
        }


        // =========================================================
        // COMMON PDF HEADER CELL
        // =========================================================

        private static IContainer PdfHeaderCell(
            IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .PaddingHorizontal(4)
                .PaddingVertical(5)
                .AlignMiddle();
        }


        // =========================================================
        // COMMON PDF BODY CELL
        // =========================================================

        private static IContainer PdfBodyCell(
            IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(3)
                .PaddingVertical(4)
                .AlignMiddle();
        }


        // =========================================================
        // PDF SCORE CELL
        // =========================================================

        private static IContainer PdfScoreCell(
            IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten4)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(3)
                .PaddingVertical(4)
                .AlignMiddle()
                .AlignCenter();
        }


        // =========================================================
        // PDF STATUS CELL
        // =========================================================

        private static IContainer PdfStatusCell(
            IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten4)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(3)
                .PaddingVertical(4)
                .AlignMiddle()
                .AlignCenter();
        }


        // =========================================================
        // RANKING BODY CELL
        // =========================================================

        private static IContainer PdfRankingBodyCell(
            IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(3)
                .PaddingVertical(4)
                .AlignMiddle();
        }


        // =========================================================
        // RANKING SCORE CELL
        // =========================================================

        private static IContainer PdfRankingScoreCell(
            IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten4)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(3)
                .PaddingVertical(4)
                .AlignMiddle()
                .AlignCenter();
        }


        // =========================================================
        // GET PERFORMANCE PERIOD NAME
        // =========================================================

        private static string GetPerformancePeriodName(
            PerformanceFilterViewModel? filter)
        {
            if (filter == null)
            {
                return "All Time";
            }


            if (!string.IsNullOrWhiteSpace(filter.Range))
            {
                return filter.Range switch
                {
                    "Today" =>
                        "Today",

                    "ThisWeek" =>
                        "This Week",

                    "ThisMonth" =>
                        "This Month",

                    "Custom" =>
                        "Custom Range",

                    _ =>
                        filter.Range
                };
            }


            if (filter.FromDate.HasValue &&
                filter.ToDate.HasValue)
            {
                return
                    $"{filter.FromDate.Value:dd MMM yyyy} - " +
                    $"{filter.ToDate.Value:dd MMM yyyy}";
            }


            return "Selected Period";
        }
    }
}