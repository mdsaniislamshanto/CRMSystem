using CRMSystem.Attributes;
using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize(RoleKeys.Admin)]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(
            IReportService reportService)
        {
            _reportService = reportService;
        }


        // GET: Report/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reports";
            ViewData["Breadcrumb"] = "Reports";

            var model =
                await _reportService.GetAdminReportAsync();

            return View(model);
        }
    }
}