using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetTasksAsync(string? project = null, Models.TaskStatus? status = null, bool? unscheduledOnly = null);
    Task<TaskItem?> GetTaskDetailsAsync(Guid id);
    Task<TaskItem> CreateTaskAsync(TaskItem input);
    Task<TaskItem?> UpdateTaskAsync(Guid id, TaskItem updatedTask);
    Task<bool> UpdateTaskStatusAsync(Guid taskId, string statusStr);
    Task<bool> UpdateTaskStatusAsync(Guid taskId, Models.TaskStatus status);
    Task<bool> DeleteTaskAsync(Guid id);
    Task<bool> ReorderTasksAsync(List<TaskUpdateDto> tasks);
    Task<bool> RenameProjectAsync(string oldName, string newName);
    Task<bool> DeleteProjectAsync(string projectName);
    Task<TaskComment?> AddCommentAsync(Guid taskId, string text);
    Task<bool> DeleteCommentAsync(Guid commentId);
    Task<bool> DeleteCommentAsync(Guid taskId, Guid commentId);
    Task<TaskAttachment?> AttachUploadedFileAsync(Guid taskId, IFormFile file);
    Task<TaskAttachment?> AttachLocalFileAsync(Guid taskId, string localFilePath);
    Task<bool> DeleteAttachmentAsync(Guid attachmentId);
    Task<bool> DeleteAttachmentAsync(Guid taskId, Guid attachmentId);
}
