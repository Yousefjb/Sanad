using System.ComponentModel;
using ModelContextProtocol.Server;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

[McpServerToolType]
public class McpEndpoints
{
    private readonly IStorageService _storageService;
    private readonly IThoughtService _thoughtService;
    private readonly ITaskService _taskService;
    private readonly IFinanceService _financeService;
    private readonly IDebtService _debtService;
    private readonly INoteService _noteService;
    private readonly IBookService _bookService;
    private readonly IBookSearchService _searchService;
    private readonly IReadingService _readingService;
    private readonly IGoalService _goalService;
    private readonly IHabitService _habitService;
    private readonly ICalendarService _calendarService;
    private readonly IAppService _appService;
    private readonly FileManagerService _fileManager;
    private readonly ITenantProvider _tenantProvider;

    public McpEndpoints(
        IStorageService storageService,
        IThoughtService thoughtService,
        ITaskService taskService,
        IFinanceService financeService,
        IDebtService debtService,
        INoteService noteService,
        IBookService bookService,
        IBookSearchService searchService,
        IReadingService readingService,
        IGoalService goalService,
        IHabitService habitService,
        ICalendarService calendarService,
        IAppService appService,
        FileManagerService fileManager,
        ITenantProvider tenantProvider)
    {
        _storageService = storageService;
        _thoughtService = thoughtService;
        _taskService = taskService;
        _financeService = financeService;
        _debtService = debtService;
        _noteService = noteService;
        _bookService = bookService;
        _searchService = searchService;
        _readingService = readingService;
        _goalService = goalService;
        _habitService = habitService;
        _calendarService = calendarService;
        _appService = appService;
        _fileManager = fileManager;
        _tenantProvider = tenantProvider;
    }

    public McpEndpoints(
        Sanad.Api.Data.SanadDbContext db,
        IBookSearchService searchService,
        FileManagerService fileManager,
        ITenantProvider tenantProvider,
        DiskQuotaService quotaService,
        Sanad.Api.Data.AdminDbContext adminDb)
        : this(
            new StorageService(adminDb, quotaService),
            new ThoughtService(db),
            new TaskService(db, tenantProvider, quotaService),
            new FinanceService(db),
            new DebtService(db),
            new NoteService(db, tenantProvider),
            new BookService(db),
            searchService,
            new ReadingService(db),
            new GoalService(db),
            new HabitService(db),
            new CalendarService(db),
            new AppService(db),
            fileManager,
            tenantProvider)
    {
    }

    // Storage Tools
    [McpServerTool, Description("Get all available storage tiers")]
    public async Task<List<StorageTier>> GetTiers() => await _storageService.GetTiersAsync();

    [McpServerTool, Description("Get current storage status for the authenticated user")]
    public async Task<object> GetStorageStatus() => await _storageService.GetStorageStatusAsync(_tenantProvider.GetUsername());

    // Thoughts Tools
    [McpServerTool, Description("Get a list of thoughts")]
    public async Task<List<Thought>> GetThoughts() => await _thoughtService.GetThoughtsAsync(1, 20);

    [McpServerTool, Description("Create a new thought")]
    public async Task<Thought> CreateThought(string content) => await _thoughtService.CreateThoughtAsync(content);

    [McpServerTool, Description("Delete a thought by ID")]
    public async Task<bool> DeleteThought(string id) => await _thoughtService.DeleteThoughtAsync(id);

    // Tasks Tools
    [McpServerTool, Description("Get a list of tasks")]
    public async Task<List<TaskItem>> GetTasks(string? project = null, Models.TaskStatus? status = null, bool? unscheduledOnly = null) =>
        await _taskService.GetTasksAsync(project, status, unscheduledOnly);

    [McpServerTool, Description("Create a new task")]
    public async Task<TaskItem> CreateTask(string title, string? content = null, string? project = null) =>
        await _taskService.CreateTaskAsync(new TaskItem { Title = title, Content = content, Project = project, Status = Models.TaskStatus.ToDo });

    [McpServerTool, Description("Delete a task by ID")]
    public async Task<bool> DeleteTask(Guid id) => await _taskService.DeleteTaskAsync(id);

    [McpServerTool, Description("Get specific task details including comments and attachments")]
    public async Task<object?> GetTaskDetails(Guid id) => await _taskService.GetTaskDetailsAsync(id);

    [McpServerTool, Description("Update the status of a specific task (e.g. ToDo, InProgress, Done)")]
    public async Task<bool> UpdateTaskStatus(Guid taskId, string statusStr) => await _taskService.UpdateTaskStatusAsync(taskId, statusStr);

    [McpServerTool, Description("Add a rich-text comment to a task")]
    public async Task<TaskComment?> AddTaskComment(Guid taskId, string text) => await _taskService.AddCommentAsync(taskId, text);

    [McpServerTool, Description("Delete a specific task comment")]
    public async Task<bool> DeleteTaskComment(Guid commentId) => await _taskService.DeleteCommentAsync(commentId);

    [McpServerTool, Description("Attach a local file to a task. Provide the absolute file path on disk.")]
    public async Task<TaskAttachment?> AttachFileToTask(Guid taskId, string localFilePath) => await _taskService.AttachLocalFileAsync(taskId, localFilePath);

    [McpServerTool, Description("Delete a specific task attachment")]
    public async Task<bool> DeleteTaskAttachment(Guid attachmentId) => await _taskService.DeleteAttachmentAsync(attachmentId);

    // Transactions Tools
    [McpServerTool, Description("Get transaction categories")]
    public async Task<List<TransactionCategory>> GetCategories() => await _financeService.GetCategoriesAsync();

    [McpServerTool, Description("Create a transaction category")]
    public async Task<TransactionCategory> CreateCategory(string name, decimal monthlyBudget, string colorHex = "#cccccc") =>
        await _financeService.CreateCategoryAsync(name, monthlyBudget, colorHex);

    [McpServerTool, Description("Get recent transactions")]
    public async Task<List<Transaction>> GetTransactions() => await _financeService.GetRecentTransactionsAsync(20);

    [McpServerTool, Description("Create a new transaction")]
    public async Task<Transaction> CreateTransaction(decimal amount, string description, string type, Guid categoryId) =>
        await _financeService.CreateTransactionAsync(new Transaction { Amount = amount, CategoryId = categoryId, Description = description, Type = type, Date = DateTime.UtcNow })
        ?? throw new InvalidOperationException("Failed to create transaction. Ensure category exists.");

    [McpServerTool, Description("Delete a transaction by ID")]
    public async Task<bool> DeleteTransaction(Guid id) => await _financeService.DeleteTransactionAsync(id);

    // Debts Tools
    [McpServerTool, Description("Get all debts / liabilities")]
    public async Task<List<Debt>> GetDebts() => await _debtService.GetDebtsAsync();

    [McpServerTool, Description("Create a new debt / liability")]
    public async Task<Debt> CreateDebt(string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null) =>
        await _debtService.CreateDebtAsync(name, type, currentAmount, currencyId, icon);

    [McpServerTool, Description("Update a debt / liability")]
    public async Task<Debt?> UpdateDebt(Guid id, string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null) =>
        await _debtService.UpdateDebtAsync(id, name, type, currentAmount, currencyId, icon);

    [McpServerTool, Description("Delete a debt by ID")]
    public async Task<bool> DeleteDebt(Guid id) => await _debtService.DeleteDebtAsync(id);

    // Notes Tools
    [McpServerTool, Description("Get recent notes")]
    public async Task<List<Note>> GetNotes() => await _noteService.GetRecentNotesAsync(20);

    [McpServerTool, Description("Create a new note")]
    public async Task<Note> CreateNote(string title, string content, Guid notebookId) =>
        await _noteService.CreateNoteAsync(notebookId, title, content) ?? throw new InvalidOperationException("Notebook not found");

    [McpServerTool, Description("Delete a note by ID")]
    public async Task<bool> DeleteNote(Guid id) => await _noteService.DeleteNoteAsync(id);

    // Books Tools
    [McpServerTool, Description("Get all books")]
    public async Task<List<Book>> GetBooks() => await _bookService.GetBooksAsync();

    [McpServerTool, Description("Create a new book")]
    public async Task<Book> CreateBook(string title, string author, string coverUrl, int totalPages) =>
        await _bookService.CreateBookAsync(title, author, coverUrl, totalPages);

    [McpServerTool, Description("Search for books from external sources (Google Books, OpenLibrary, Apple Books)")]
    public async Task<List<BookSearchResult>> SearchBooks(string query) => await _searchService.SearchBooksAsync(query);

    [McpServerTool, Description("Update an existing book")]
    public async Task<Book?> UpdateBook(int id, string title, string author, string coverUrl, int totalPages) =>
        await _bookService.UpdateBookAsync(id, title, author, coverUrl, totalPages);

    [McpServerTool, Description("Delete a book by ID")]
    public async Task<bool> DeleteBook(int id) => await _bookService.DeleteBookAsync(id);

    // Reading Tools
    [McpServerTool, Description("Get all reading periods")]
    public async Task<List<ReadingPeriod>> GetReadingPeriods() => await _readingService.GetReadingPeriodsAsync();

    [McpServerTool, Description("Get current active reading period")]
    public async Task<object?> GetCurrentReading() => await _readingService.GetCurrentReadingAsync();

    [McpServerTool, Description("Start a new reading period")]
    public async Task<ReadingPeriod> StartReadingPeriod(int bookId) => await _readingService.StartReadingPeriodAsync(bookId);

    [McpServerTool, Description("Log reading progress")]
    public async Task<ReadingLog?> LogReading(int readingPeriodId, int startPage, int endPage) =>
        await _readingService.LogReadingAsync(readingPeriodId, startPage, endPage);

    [McpServerTool, Description("Update reading status (e.g. Reading, Paused, Completed)")]
    public async Task<ReadingPeriod?> UpdateReadingStatus(int id, string status) => await _readingService.UpdateStatusAsync(id, status);

    [McpServerTool, Description("Update reading plans for a period")]
    public async Task<List<ReadingPlan>?> UpdateReadingPlans(int id, List<PlanDto> plans) => await _readingService.UpdatePlansAsync(id, plans);

    [McpServerTool, Description("Delete a reading period")]
    public async Task<bool> DeleteReadingPeriod(int id) => await _readingService.DeleteReadingPeriodAsync(id);

    // Goals Tools
    [McpServerTool, Description("Get today's goal")]
    public async Task<DailyGoal?> GetTodaysGoal() => await _goalService.GetTodaysGoalAsync();

    [McpServerTool, Description("Set today's goal")]
    public async Task<DailyGoal> SetTodaysGoal(string goalText) =>
        await _goalService.SetGoalAsync(DateTime.Now.ToString("yyyy-MM-dd"), goalText);

    // Habits Tools
    [McpServerTool, Description("Get all habits and their logs")]
    public async Task<List<Habit>> GetHabits() => await _habitService.GetHabitsAsync();

    [McpServerTool, Description("Create a new habit")]
    public async Task<Habit> CreateHabit(string name, string icon, string frequency) =>
        await _habitService.CreateHabitAsync(name, icon, frequency);

    [McpServerTool, Description("Update an existing habit")]
    public async Task<Habit?> UpdateHabit(string id, string name, string icon, string frequency) =>
        await _habitService.UpdateHabitAsync(id, name, icon, frequency);

    [McpServerTool, Description("Delete a habit by ID")]
    public async Task<bool> DeleteHabit(string id) => await _habitService.DeleteHabitAsync(id);

    [McpServerTool, Description("Toggle habit completion for a specific date")]
    public async Task<HabitLog?> ToggleHabitLog(string id, DateTime date) => await _habitService.ToggleHabitLogAsync(id, date);

    [McpServerTool, Description("Reorder habits using a list of their IDs")]
    public async Task<bool> ReorderHabits(List<string> habitIds) => await _habitService.ReorderHabitsAsync(habitIds);

    // File Manager Tools
    [McpServerTool, Description("Get contents of a specific folder in the File Manager. If folderId is null, returns the root folder.")]
    public async Task<object> GetFolderContents(int? folderId = null) => await _fileManager.GetFolderContentsAsync(folderId);

    [McpServerTool, Description("Search for files and folders in the File Manager by name.")]
    public async Task<object> SearchFiles(string query) => await _fileManager.SearchFilesAsync(query);

    [McpServerTool, Description("Upload a local file from disk to the File Manager. Provide the absolute local file path.")]
    public async Task<FileItem?> UploadFileToSanad(string localFilePath, int? folderId = null) =>
        await _fileManager.UploadLocalFileAsync(localFilePath, folderId);

    [McpServerTool, Description("Upload a local folder (recursively) to the File Manager. Provide the absolute local folder path and optionally the target parent Folder ID.")]
    public async Task<Folder?> UploadFolderToSanad(string localFolderPath, int? targetParentId = null) =>
        await _fileManager.UploadLocalFolderAsync(localFolderPath, targetParentId);

    [McpServerTool, Description("Download a file from the File Manager to a local directory. Provide the File ID and destination absolute path (or directory).")]
    public async Task<bool> DownloadFileFromSanad(int fileId, string destinationPath) =>
        await _fileManager.DownloadFileToLocalAsync(fileId, destinationPath);

    [McpServerTool, Description("Download an entire folder (recursively) from the File Manager to a local directory.")]
    public async Task<bool> DownloadFolderFromSanad(int folderId, string destinationDirectory) =>
        await _fileManager.DownloadFolderToLocalAsync(folderId, destinationDirectory);

    // Calendar Tools
    [McpServerTool, Description("Get all calendar event categories")]
    public async Task<List<EventCategory>> GetEventCategories() => await _calendarService.GetCategoriesAsync();

    [McpServerTool, Description("Create a new calendar event category")]
    public async Task<EventCategory> CreateEventCategory(string name, string colorCode) =>
        await _calendarService.CreateCategoryAsync(name, colorCode);

    [McpServerTool, Description("Update an existing calendar event category")]
    public async Task<EventCategory?> UpdateEventCategory(Guid id, string name, string colorCode) =>
        await _calendarService.UpdateCategoryAsync(id, name, colorCode);

    [McpServerTool, Description("Delete a calendar event category by ID")]
    public async Task<bool> DeleteEventCategory(Guid id) => await _calendarService.DeleteCategoryAsync(id);

    [McpServerTool, Description("Get calendar events optionally filtered by start and end dates")]
    public async Task<List<CalendarEvent>> GetCalendarEvents(DateTime? start = null, DateTime? end = null) =>
        await _calendarService.GetEventsAsync(start, end);

    [McpServerTool, Description("Create a new calendar event")]
    public async Task<CalendarEvent> CreateCalendarEvent(
        string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay,
        string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId) =>
        await _calendarService.CreateEventAsync(title, description, startDate, endDate, isAllDay, recurrenceRule, notificationPreference, categoryId, taskItemId);

    [McpServerTool, Description("Update an existing calendar event")]
    public async Task<CalendarEvent?> UpdateCalendarEvent(
        Guid id, string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay,
        string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId) =>
        await _calendarService.UpdateEventAsync(id, title, description, startDate, endDate, isAllDay, recurrenceRule, notificationPreference, categoryId, taskItemId);

    [McpServerTool, Description("Delete a calendar event by ID")]
    public async Task<bool> DeleteCalendarEvent(Guid id) => await _calendarService.DeleteEventAsync(id);

    // Custom Apps Tools
    [McpServerTool, Description("Get all custom apps")]
    public async Task<List<CustomApp>> GetApps() => await _appService.GetAppsAsync();

    [McpServerTool, Description("Create a new custom app")]
    public async Task<CustomApp> CreateApp(string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone) =>
        await _appService.CreateAppAsync(name, htmlContent, icon, showInDashboard, isStandalone);

    [McpServerTool, Description("Update an existing custom app")]
    public async Task<CustomApp?> UpdateApp(Guid id, string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone) =>
        await _appService.UpdateAppAsync(id, name, htmlContent, icon, showInDashboard, isStandalone);

    [McpServerTool, Description("Delete a custom app by ID")]
    public async Task<bool> DeleteApp(Guid id) => await _appService.DeleteAppAsync(id);
}
