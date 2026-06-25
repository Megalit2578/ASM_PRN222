using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Services.Interfaces;
using System.Security.Claims;

namespace PresentationLayer.Pages.Documents;

[Authorize(Policy = "CanUploadDocuments")]
public class UploadModel : PageModel
{
    private readonly IDocumentService _docService;
    private readonly ISubjectService _subjectService;
    private readonly IChapterService _chapterService;

    public UploadModel(IDocumentService docService, ISubjectService subjectService, IChapterService chapterService)
    {
        _docService = docService;
        _subjectService = subjectService;
        _chapterService = chapterService;
    }

    public List<ServiceLayer.DTOs.SubjectDto> Subjects { get; private set; } = [];
    public List<Chapter> Chapters { get; private set; } = [];

    [BindProperty] public IFormFile? UploadFile { get; set; }
    [BindProperty] public string SubjectId { get; set; } = "";
    [BindProperty] public string? ChapterId { get; set; }
    [BindProperty] public string? Title { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Upload tài liệu";
        ViewData["TopbarTitle"] = "⬆️ Upload tài liệu";

        var assignedSubjectId = User.FindFirst("AssignedSubjectId")?.Value;

        // Người upload chỉ được thao tác đúng môn được admin giao (admin không vào được trang này).
        if (!string.IsNullOrEmpty(assignedSubjectId))
        {
            Subjects = (await _subjectService.GetAllAsync())
                .Where(s => s.Id == assignedSubjectId)
                .ToList();
            SubjectId = assignedSubjectId;
        }
        else
        {
            Subjects = []; // Không có môn được phân công -> không được upload
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Upload tài liệu";

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var assignedSubjectId = User.FindFirst("AssignedSubjectId")?.Value;

        // Admin không được upload; chỉ giảng viên được giao môn mới upload (và đúng môn đó).
        if (role == "Admin")
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(assignedSubjectId))
        {
            Subjects = [];
            ModelState.AddModelError("", "Bạn chưa được phân công môn học nào nên không thể upload.");
            return Page();
        }

        Subjects = (await _subjectService.GetAllAsync())
            .Where(s => s.Id == assignedSubjectId)
            .ToList();

        if (UploadFile == null || UploadFile.Length == 0)
        {
            ModelState.AddModelError("File", "Vui lòng chọn file.");
            return Page();
        }

        // Luôn ép về môn được giao — chặn mọi cố gắng upload cho môn khác.
        SubjectId = assignedSubjectId;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        try
        {
            await using var stream = UploadFile.OpenReadStream();
            var result = await _docService.UploadAsync(
                stream, UploadFile.FileName, UploadFile.ContentType, UploadFile.Length,
                SubjectId, userId, Title, ChapterId);

            TempData["Success"] = result.Outcome switch
            {
                UploadOutcome.Created => $"✅ Đã upload '{result.Document.Title}' thành công.",
                UploadOutcome.Replaced => $"🔄 Đã thay thế phiên bản cũ của '{result.Document.Title}'.",
                UploadOutcome.Duplicate => "⚠️ File này đã được upload trước đó (nội dung giống hệt).",
                _ => "Upload thành công."
            };

            return RedirectToPage("/Documents/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Lỗi khi upload: {ex.Message}");
            return Page();
        }
    }
}


