using ClosedXML.Excel;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CRMSystem.Services
{
    public class ReportExportService : IReportExportService
    {
        public ReportExportService()
        {
            QuestPDF.Settings.License =
                LicenseType.Community;
        }


        // =========================================================
        // Generate Team Lead PDF Report
        // =========================================================

        public byte[] GenerateTeamLeadPdf(
            TeamLeadReportViewModel report)
        {
            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);

                        // -------------------------------------------------
                        // Header
                        // -------------------------------------------------

                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text("CRM System")
                                    .FontSize(20)
                                    .Bold();

                                column.Item()
                                    .PaddingTop(5)
                                    .Text(
                                        "Team Lead Performance Report")
                                    .FontSize(16)
                                    .Bold();

                                column.Item()
                                    .PaddingTop(5)
                                    .Text(
                                        $"Team Lead: {report.TeamLeadName}")
                                    .FontSize(11);

                                column.Item()
                                    .Text(
                                        $"Period: " +
                                        $"{report.FromDate:dd MMM yyyy} - " +
                                        $"{report.ToDate:dd MMM yyyy}")
                                    .FontSize(10);
                            });


                        // -------------------------------------------------
                        // Content
                        // -------------------------------------------------

                        page.Content()
                            .PaddingTop(20)
                            .Column(column =>
                            {
                                // =========================================
                                // Team Summary
                                // =========================================

                                column.Item()
                                    .Text("Team Summary")
                                    .FontSize(14)
                                    .Bold();

                                column.Item()
                                    .PaddingTop(8)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn();
                                        });


                                        AddPdfRow(
                                            table,
                                            "Sales Officers",
                                            report.TotalSalesOfficers
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Assigned Leads",
                                            report.TotalAssignedLeads
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Accepted Leads",
                                            report.TotalAcceptedLeads
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Completed Leads",
                                            report.TotalCompletedLeads
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Total Feedbacks",
                                            report.TotalFeedbacks
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Pending Follow-ups",
                                            report.TotalPendingFollowUps
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Overdue Follow-ups",
                                            report.TotalOverdueFollowUps
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Team Target",
                                            report.TeamTarget
                                                .ToString());

                                        AddPdfRow(
                                            table,
                                            "Target Achievement",
                                            $"{report.TargetAchievementPercentage:0.00}%");

                                        AddPdfRow(
                                            table,
                                            "Performance Score",
                                            $"{report.PerformanceScore:0.00}%");
                                    });


                                // =========================================
                                // KPI Performance
                                // =========================================

                                column.Item()
                                    .PaddingTop(20)
                                    .Text("KPI Performance")
                                    .FontSize(14)
                                    .Bold();

                                column.Item()
                                    .PaddingTop(8)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });


                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Text("KPI")
                                                .Bold();

                                            header.Cell()
                                                .Text("Score")
                                                .Bold();

                                            header.Cell()
                                                .Text("Weight")
                                                .Bold();
                                        });


                                        AddPdfKpiRow(
                                            table,
                                            "Completion Rate",
                                            report.CompletionRate,
                                            "25%");


                                        AddPdfKpiRow(
                                            table,
                                            "First Feedback SLA",
                                            report.FirstFeedbackSLAComplianceRate,
                                            "20%");


                                        AddPdfKpiRow(
                                            table,
                                            "Follow-up Timeliness",
                                            report.FollowUpTimelinessRate,
                                            "20%");


                                        AddPdfKpiRow(
                                            table,
                                            "Acceptance SLA",
                                            report.AcceptanceSLAComplianceRate,
                                            "15%");


                                        AddPdfKpiRow(
                                            table,
                                            "Next Feedback SLA",
                                            report.NextFeedbackSLAComplianceRate,
                                            "10%");


                                        AddPdfKpiRow(
                                            table,
                                            "Acceptance Rate",
                                            report.AcceptanceRate,
                                            "10%");
                                    });


                                // =========================================
                                // Officer Performance
                                // =========================================

                                column.Item()
                                    .PaddingTop(20)
                                    .Text(
                                        "Sales Officer Performance")
                                    .FontSize(14)
                                    .Bold();


                                column.Item()
                                    .PaddingTop(8)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(25);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });


                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Text("#")
                                                .Bold();

                                            header.Cell()
                                                .Text("Sales Officer")
                                                .Bold();

                                            header.Cell()
                                                .Text("Assigned")
                                                .Bold();

                                            header.Cell()
                                                .Text("Accepted")
                                                .Bold();

                                            header.Cell()
                                                .Text("Completed")
                                                .Bold();

                                            header.Cell()
                                                .Text("Score")
                                                .Bold();
                                        });


                                        var rank = 1;


                                        foreach (
                                            var officer
                                            in report.OfficerReports)
                                        {
                                            table.Cell()
                                                .Text(
                                                    rank.ToString());

                                            table.Cell()
                                                .Text(
                                                    officer.SalesOfficerName);

                                            table.Cell()
                                                .Text(
                                                    officer.TotalAssignedLeads
                                                        .ToString());

                                            table.Cell()
                                                .Text(
                                                    officer.AcceptedLeads
                                                        .ToString());

                                            table.Cell()
                                                .Text(
                                                    officer.CompletedLeads
                                                        .ToString());

                                            table.Cell()
                                                .Text(
                                                    $"{officer.PerformanceScore:0.00}%");

                                            rank++;
                                        }
                                    });
                            });


                        // -------------------------------------------------
                        // Footer
                        // -------------------------------------------------

                        page.Footer()
                            .AlignCenter()
                            .Text(
                                $"Generated on " +
                                $"{DateTime.Now:dd MMM yyyy HH:mm}");
                    });
                });


            return document.GeneratePdf();
        }


        // =========================================================
        // Generate Team Lead Excel Report
        // =========================================================

        public byte[] GenerateTeamLeadExcel(
            TeamLeadReportViewModel report)
        {
            using var workbook =
                new XLWorkbook();


            // -----------------------------------------------------
            // Team Summary Sheet
            // -----------------------------------------------------

            var summary =
                workbook.Worksheets.Add("Team Report");


            summary.Cell("A1")
                .Value =
                "CRM System - Team Lead Performance Report";

            summary.Cell("A1")
                .Style.Font.Bold = true;


            summary.Cell("A2")
                .Value =
                $"Team Lead: {report.TeamLeadName}";


            summary.Cell("A3")
                .Value =
                $"Period: " +
                $"{report.FromDate:dd MMM yyyy} - " +
                $"{report.ToDate:dd MMM yyyy}";


            summary.Cell("A5")
                .Value = "Metric";

            summary.Cell("B5")
                .Value = "Value";


            summary.Range("A5:B5")
                .Style.Font.Bold = true;


            var row = 6;


            AddExcelRow(
                summary,
                ref row,
                "Sales Officers",
                report.TotalSalesOfficers);


            AddExcelRow(
                summary,
                ref row,
                "Assigned Leads",
                report.TotalAssignedLeads);


            AddExcelRow(
                summary,
                ref row,
                "Accepted Leads",
                report.TotalAcceptedLeads);


            AddExcelRow(
                summary,
                ref row,
                "Completed Leads",
                report.TotalCompletedLeads);


            AddExcelRow(
                summary,
                ref row,
                "Total Feedbacks",
                report.TotalFeedbacks);


            AddExcelRow(
                summary,
                ref row,
                "Pending Follow-ups",
                report.TotalPendingFollowUps);


            AddExcelRow(
                summary,
                ref row,
                "Overdue Follow-ups",
                report.TotalOverdueFollowUps);


            AddExcelRow(
                summary,
                ref row,
                "Team Target",
                report.TeamTarget);


            AddExcelRow(
                summary,
                ref row,
                "Target Achievement %",
                report.TargetAchievementPercentage);


            AddExcelRow(
                summary,
                ref row,
                "Acceptance Rate %",
                report.AcceptanceRate);


            AddExcelRow(
                summary,
                ref row,
                "Acceptance SLA %",
                report.AcceptanceSLAComplianceRate);


            AddExcelRow(
                summary,
                ref row,
                "First Feedback SLA %",
                report.FirstFeedbackSLAComplianceRate);


            AddExcelRow(
                summary,
                ref row,
                "Follow-up Timeliness %",
                report.FollowUpTimelinessRate);


            AddExcelRow(
                summary,
                ref row,
                "Next Feedback SLA %",
                report.NextFeedbackSLAComplianceRate);


            AddExcelRow(
                summary,
                ref row,
                "Completion Rate %",
                report.CompletionRate);


            AddExcelRow(
                summary,
                ref row,
                "Performance Score %",
                report.PerformanceScore);


            summary.Columns()
                .AdjustToContents();


            // -----------------------------------------------------
            // Officer Performance Sheet
            // -----------------------------------------------------

            var officerSheet =
                workbook.Worksheets.Add(
                    "Officer Performance");


            string[] headers =
            {
                "Rank",
                "Sales Officer",
                "Assigned",
                "Accepted",
                "Acceptance Rate %",
                "Completed",
                "Completion Rate %",
                "First Feedback SLA %",
                "Follow-up Timeliness %",
                "Acceptance SLA %",
                "Next Feedback SLA %",
                "Performance Score %"
            };


            for (var i = 0;
                 i < headers.Length;
                 i++)
            {
                officerSheet.Cell(1, i + 1)
                    .Value = headers[i];
            }


            officerSheet.Range(
                    1,
                    1,
                    1,
                    headers.Length)
                .Style.Font.Bold = true;


            var officerRow = 2;
            var officerRank = 1;


            foreach (
                var officer
                in report.OfficerReports)
            {
                officerSheet.Cell(
                        officerRow,
                        1)
                    .Value = officerRank;


                officerSheet.Cell(
                        officerRow,
                        2)
                    .Value =
                    officer.SalesOfficerName;


                officerSheet.Cell(
                        officerRow,
                        3)
                    .Value =
                    officer.TotalAssignedLeads;


                officerSheet.Cell(
                        officerRow,
                        4)
                    .Value =
                    officer.AcceptedLeads;


                officerSheet.Cell(
                        officerRow,
                        5)
                    .Value =
                    officer.AcceptanceRate;


                officerSheet.Cell(
                        officerRow,
                        6)
                    .Value =
                    officer.CompletedLeads;


                officerSheet.Cell(
                        officerRow,
                        7)
                    .Value =
                    officer.CompletionRate;


                officerSheet.Cell(
                        officerRow,
                        8)
                    .Value =
                    officer.FirstFeedbackSLAComplianceRate;


                officerSheet.Cell(
                        officerRow,
                        9)
                    .Value =
                    officer.FollowUpTimelinessRate;


                officerSheet.Cell(
                        officerRow,
                        10)
                    .Value =
                    officer.AcceptanceSLAComplianceRate;


                officerSheet.Cell(
                        officerRow,
                        11)
                    .Value =
                    officer.NextFeedbackSLAComplianceRate;


                officerSheet.Cell(
                        officerRow,
                        12)
                    .Value =
                    officer.PerformanceScore;


                officerRow++;
                officerRank++;
            }


            officerSheet.Columns()
                .AdjustToContents();


            // -----------------------------------------------------
            // Convert Workbook To Byte Array
            // -----------------------------------------------------

            using var stream =
                new MemoryStream();


            workbook.SaveAs(stream);


            return stream.ToArray();
        }


        // =========================================================
        // PDF Helper - Normal Row
        // =========================================================

        private static void AddPdfRow(
            TableDescriptor table,
            string label,
            string value)
        {
            table.Cell()
                .Text(label);

            table.Cell()
                .Text(value);
        }


        // =========================================================
        // PDF Helper - KPI Row
        // =========================================================

        private static void AddPdfKpiRow(
            TableDescriptor table,
            string name,
            double score,
            string weight)
        {
            table.Cell()
                .Text(name);

            table.Cell()
                .Text(
                    $"{score:0.00}%");

            table.Cell()
                .Text(weight);
        }


        // =========================================================
        // Excel Helper
        // =========================================================

        private static void AddExcelRow(
            IXLWorksheet worksheet,
            ref int row,
            string label,
            XLCellValue value)
        {
            worksheet.Cell(row, 1)
                .Value = label;

            worksheet.Cell(row, 2)
                .Value = value;

            row++;
        }
    }
}