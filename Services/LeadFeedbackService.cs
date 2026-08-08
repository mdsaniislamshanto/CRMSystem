using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class LeadFeedbackService : ILeadFeedbackService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
 

        // Allowed file extensions and size limits
        private static readonly string[] AllowedImageExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly string[] AllowedVoiceExtensions =
            {
                ".mp3",
                ".wav",
                ".m4a"
            };

        private const long MaxImageSize = 5 * 1024 * 1024;   // 5 MB

        private const long MaxVoiceSize = 20 * 1024 * 1024;  // 20 MB



        public LeadFeedbackService(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task SubmitFeedbackAsync( SubmitFeedbackViewModel model, long salesOfficerId)
        {
            var assignment = await _context.LeadAssignments
                .FirstOrDefaultAsync(a => a.AssignmentId == model.AssignmentId);

            if (assignment == null)
            {
                throw new Exception("Assignment not found.");
            }

            if (assignment.SalesOfficerId != salesOfficerId)
            {
                throw new Exception("Unauthorized feedback submission.");
            }

            if (assignment.AssignmentStatus != AssignmentStatus.Accepted)
            {
                throw new Exception("Only accepted assignments can submit feedback.");
            }

            if (string.IsNullOrWhiteSpace(model.Summary))
            {
                throw new Exception("Summary is required.");
            }

            if (model.ProofImage == null && model.VoiceRecording == null)
            {
                throw new Exception("Please upload either a proof image or a voice recording.");
            }

            if (model.Status != FeedbackStatus.Completed &&
                model.Status != FeedbackStatus.Closed)
            {
                if (!model.NextFollowUpDate.HasValue)
                {
                    throw new Exception("Next Follow-up Date is required.");
                }

                if (model.NextFollowUpDate.Value.Date < DateTime.Today)
                {
                    throw new Exception("Next Follow-up Date cannot be earlier than today.");
                }
            }

            string? imagePath = null;
            string? voicePath = null;

            if (model.ProofImage != null)
            {
                imagePath = await SaveFileAsync(
                    model.ProofImage,
                    "images",
                    AllowedImageExtensions,
                    MaxImageSize);
            }

            if (model.VoiceRecording != null)
            {
                voicePath = await SaveFileAsync(
                    model.VoiceRecording,
                    "voices",
                    AllowedVoiceExtensions,
                    MaxVoiceSize);
            }

            var feedback = new Feedback
            {
                AssignmentId = model.AssignmentId,
                Summary = model.Summary.Trim(),
                Status = model.Status,
                ProofImage = imagePath,
                VoiceRecording = voicePath,
                Notes = model.Notes?.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);

            await _context.SaveChangesAsync();
        }

        // Retrieves the feedback history for a specific sales officer
        public async Task<List<FeedbackHistoryViewModel>> GetFeedbackHistoryAsync(long salesOfficerId)
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)
                .Where(f => f.LeadAssignment!.SalesOfficerId == salesOfficerId)
                .OrderByDescending(f => f.SubmittedAt)
                .Select(f => new FeedbackHistoryViewModel
                {
                    FeedbackId = f.FeedbackId,
                    AssignmentId = f.AssignmentId,
                    LeadId = f.LeadAssignment!.LeadId,
                    CompanyName = f.LeadAssignment.Lead!.CompanyName,
                    LeadName = f.LeadAssignment.Lead.LeadName,
                    Status = f.Status,
                    SubmittedAt = f.SubmittedAt,
                    NextFollowUpDate = f.NextFollowUpDate
                })
                .ToListAsync();

            return feedbacks;
        }

 
        //// Retrieves the feedback history for a specific sales officer
        //public async Task<List<FeedbackHistoryViewModel>> GetFeedbackHistoryAsync(long salesOfficerId)
        //{
        //    var feedbacks = await _context.Feedbacks

        //        .Include(f => f.LeadAssignment)
        //            .ThenInclude(a => a!.Lead)

        //        .Where(f => f.LeadAssignment!.SalesOfficerId == salesOfficerId)

        //        .OrderByDescending(f => f.SubmittedAt)

        //        .Select(f => new FeedbackHistoryViewModel
        //        {
        //            FeedbackId = f.FeedbackId,

        //            AssignmentId = f.AssignmentId,

        //            LeadId = f.LeadAssignment!.LeadId,

        //            CompanyName = f.LeadAssignment.Lead!.CompanyName,

        //            LeadName = f.LeadAssignment.Lead.LeadName,

        //            Status = f.Status,

        //            SubmittedAt = f.SubmittedAt,

        //            //NextFollowUpDate = f.NextFollowUpDate
        //        })

        //        .ToListAsync();

        //    return feedbacks;
        //}




        // Helper method to save files
        private async Task<string> SaveFileAsync(IFormFile file,string folderName,string[] allowedExtensions,long maxSize)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Invalid file format.");
            }

            if (file.Length > maxSize)
            {
                throw new Exception("File size exceeded.");
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "feedback",
                folderName);

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";

            var fullPath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);

            await file.CopyToAsync(stream);

            return $"/uploads/feedback/{folderName}/{fileName}";
        }

        // Retrieves detailed feedback information for a specific feedback entry
        public async Task<FeedbackDetailsViewModel?> GetFeedbackDetailsAsync( long feedbackId,long salesOfficerId)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)
                .Where(f =>
                    f.FeedbackId == feedbackId &&
                    f.LeadAssignment!.SalesOfficerId == salesOfficerId)
                .Select(f => new FeedbackDetailsViewModel
                {
                    FeedbackId = f.FeedbackId,

                    AssignmentId = f.AssignmentId,

                    LeadId = f.LeadAssignment!.LeadId,

                    CompanyName = f.LeadAssignment.Lead!.CompanyName,

                    LeadName = f.LeadAssignment.Lead.LeadName,

                    Email = f.LeadAssignment.Lead.Email,

                    Phone = f.LeadAssignment.Lead.Phone,

                    Summary = f.Summary,

                    Status = f.Status,

                    SubmittedAt = f.SubmittedAt,

                    NextFollowUpDate = f.NextFollowUpDate,

                    ProofImage = f.ProofImage,

                    VoiceRecording = f.VoiceRecording,

                    Notes = f.Notes
                })
                .FirstOrDefaultAsync();

            return feedback;
        }


    }
}