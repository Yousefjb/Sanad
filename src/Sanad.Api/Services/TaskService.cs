using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class TaskService : ITaskService
{
    private readonly SanadDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly DiskQuotaService _quotaService;

    public TaskService(SanadDbContext db, ITenantProvider tenantProvider, DiskQuotaService quotaService)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _quotaService = quotaService;
    }

    public async Task<List<TaskItem>> GetTasksAsync(string? project = null, Models.TaskStatus? status = null, bool? unscheduledOnly = null)
    {
        var query = _db.TaskItems.AsQueryable();
        if (!string.IsNullOrEmpty(project))
        {
            if (project == "__NONE__" || project.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => string.IsNullOrEmpty(t.Project));
            else
                query = query.Where(t => t.Project == project);
        }
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (unscheduledOnly == true)
            query = query.Where(t => t.StartDate == null);

        return await query.OrderBy(t => t.Order).ThenByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<TaskItem?> GetTaskDetailsAsync(Guid id)
    {
        return await _db.TaskItems
            .Include(t => t.Comments.OrderBy(c => c.CreatedAt))
            .Include(t => t.Attachments)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem input)
    {
        if (input.Id == Guid.Empty)
            input.Id = Guid.NewGuid();
        if (input.CreatedAt == default)
            input.CreatedAt = DateTime.UtcNow;
        input.UpdatedAt = DateTime.UtcNow;

        _db.TaskItems.Add(input);
        await _db.SaveChangesAsync();
        return input;
    }

    public async Task<TaskItem?> UpdateTaskAsync(Guid id, TaskItem updatedTask)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null) return null;

        task.Title = updatedTask.Title;
        task.Content = updatedTask.Content;
        task.Status = updatedTask.Status;
        task.Tags = updatedTask.Tags;
        task.Project = updatedTask.Project;
        task.EstimatedMinutes = updatedTask.EstimatedMinutes;
        task.Order = updatedTask.Order;
        task.StartDate = updatedTask.StartDate;
        task.EndDate = updatedTask.EndDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> UpdateTaskStatusAsync(Guid taskId, string statusStr)
    {
        if (Enum.TryParse<Models.TaskStatus>(statusStr, true, out var newStatus))
        {
            return await UpdateTaskStatusAsync(taskId, newStatus);
        }
        return false;
    }

    public async Task<bool> UpdateTaskStatusAsync(Guid taskId, Models.TaskStatus status)
    {
        var task = await _db.TaskItems.FindAsync(taskId);
        if (task == null) return false;

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTaskAsync(Guid id)
    {
        var task = await _db.TaskItems
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return false;

        var filesToDelete = new List<string>();
        try
        {
            var username = _tenantProvider.GetUsername();
            if (task.Attachments != null)
            {
                foreach (var attachment in task.Attachments)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", username, attachment.FilePath.TrimStart('/'));
                    filesToDelete.Add(filePath);
                }
            }

            filesToDelete.AddRange(Utils.UploadHelper.GetAttachmentPathsFromHtml(task.Content, username));
        }
        catch
        {
            // Ignore tenant username error in test/dummy environments
        }

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();

        if (filesToDelete.Count > 0)
        {
            Utils.UploadHelper.DeleteFiles(filesToDelete);
        }

        return true;
    }

    public async Task<bool> ReorderTasksAsync(List<TaskUpdateDto> tasks)
    {
        var ids = tasks.Select(t => t.Id).ToList();
        var dbTasks = await _db.TaskItems.Where(t => ids.Contains(t.Id)).ToListAsync();

        foreach (var taskUpdate in tasks)
        {
            var task = dbTasks.FirstOrDefault(t => t.Id == taskUpdate.Id);
            if (task != null)
            {
                task.Status = taskUpdate.Status;
                task.Order = taskUpdate.Order;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RenameProjectAsync(string oldName, string newName)
    {
        var tasks = await _db.TaskItems.Where(t => t.Project == oldName).ToListAsync();
        foreach (var task in tasks)
        {
            task.Project = newName.Trim();
            task.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProjectAsync(string projectName)
    {
        var tasks = await _db.TaskItems.Where(t => t.Project == projectName).ToListAsync();
        foreach (var task in tasks)
        {
            task.Project = null;
            task.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TaskComment?> AddCommentAsync(Guid taskId, string text)
    {
        var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == taskId);
        if (!taskExists) return null;

        var comment = new TaskComment { TaskItemId = taskId, Text = text, CreatedAt = DateTime.UtcNow };
        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    public Task<bool> DeleteCommentAsync(Guid commentId) => DeleteCommentAsync(Guid.Empty, commentId);

    public async Task<bool> DeleteCommentAsync(Guid taskId, Guid commentId)
    {
        var query = _db.TaskComments.Where(c => c.Id == commentId);
        if (taskId != Guid.Empty) query = query.Where(c => c.TaskItemId == taskId);
        var comment = await query.FirstOrDefaultAsync();
        if (comment == null) return false;

        _db.TaskComments.Remove(comment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TaskAttachment?> AttachUploadedFileAsync(Guid taskId, IFormFile file)
    {
        var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == taskId);
        if (!taskExists) return null;

        var username = _tenantProvider.GetUsername();
        var canUpload = await _quotaService.CanUploadAsync(username, file.Length);
        if (!canUpload) return null;

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", username, "attachments");
        Directory.CreateDirectory(uploadsDir);

        var (uniqueFileName, filePath) = Utils.FileUtils.GenerateUniqueFile(uploadsDir, Path.GetExtension(file.FileName));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new TaskAttachment
        {
            TaskItemId = taskId,
            FileName = file.FileName,
            FilePath = $"/api/attachments/{uniqueFileName}",
            CreatedAt = DateTime.UtcNow
        };

        _db.TaskAttachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task<TaskAttachment?> AttachLocalFileAsync(Guid taskId, string localFilePath)
    {
        var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == taskId);
        if (!taskExists || !File.Exists(localFilePath)) return null;

        var fileInfo = new FileInfo(localFilePath);
        var username = _tenantProvider.GetUsername();

        var canUpload = await _quotaService.CanUploadAsync(username, fileInfo.Length);
        if (!canUpload) return null;

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", username, "attachments");
        Directory.CreateDirectory(uploadsDir);

        var fileName = Path.GetFileName(localFilePath);
        var (uniqueFileName, destPath) = Utils.FileUtils.GenerateUniqueFile(uploadsDir, Path.GetExtension(localFilePath));

        try
        {
            using var sourceStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var destinationStream = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
            await sourceStream.CopyToAsync(destinationStream);
        }
        catch
        {
            return null;
        }

        var attachment = new TaskAttachment
        {
            TaskItemId = taskId,
            FileName = fileName,
            FilePath = $"/api/attachments/{uniqueFileName}",
            CreatedAt = DateTime.UtcNow
        };

        _db.TaskAttachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public Task<bool> DeleteAttachmentAsync(Guid attachmentId) => DeleteAttachmentAsync(Guid.Empty, attachmentId);

    public async Task<bool> DeleteAttachmentAsync(Guid taskId, Guid attachmentId)
    {
        var query = _db.TaskAttachments.Where(a => a.Id == attachmentId);
        if (taskId != Guid.Empty) query = query.Where(a => a.TaskItemId == taskId);
        var attachment = await query.FirstOrDefaultAsync();
        if (attachment == null) return false;

        var username = _tenantProvider.GetUsername();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", username, attachment.FilePath.TrimStart('/'));

        _db.TaskAttachments.Remove(attachment);
        await _db.SaveChangesAsync();

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
            }
        }

        return true;
    }
}
