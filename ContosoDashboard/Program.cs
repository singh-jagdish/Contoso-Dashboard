using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<DocumentAuthorizationService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IMalwareScanner, LocalMalwareScanner>();
builder.Services.AddSingleton<IDocumentScanQueue, LocalDocumentScanQueue>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddHostedService<LocalDocumentScanWorker>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For development - use migrations in production
        EnsureDocumentSchema(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static void EnsureDocumentSchema(ApplicationDbContext context)
{
    var statements = new[]
    {
        "IF OBJECT_ID(N'[Documents]', N'U') IS NULL CREATE TABLE [Documents] ([DocumentId] int NOT NULL IDENTITY, [Title] nvarchar(255) NOT NULL, [Description] nvarchar(2000) NULL, [Category] nvarchar(100) NOT NULL, [OriginalFileName] nvarchar(255) NOT NULL, [FilePath] nvarchar(500) NOT NULL, [FileType] nvarchar(255) NOT NULL, [FileSize] bigint NOT NULL, [UploadedDate] datetime2 NOT NULL, [UploadedByUserId] int NOT NULL, [ProjectId] int NULL, [TaskId] int NULL, [ScanStatus] nvarchar(20) NOT NULL, [ScanRequestedDate] datetime2 NULL, [ScanCompletedDate] datetime2 NULL, [ScanAttemptCount] int NOT NULL, [ScanError] nvarchar(1000) NULL, CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]), CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]), CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE SET NULL, CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId]) ON DELETE SET NULL)",
        "IF OBJECT_ID(N'[DocumentTags]', N'U') IS NULL CREATE TABLE [DocumentTags] ([DocumentTagId] int NOT NULL IDENTITY, [DocumentId] int NOT NULL, [Value] nvarchar(100) NOT NULL, CONSTRAINT [PK_DocumentTags] PRIMARY KEY ([DocumentTagId]), CONSTRAINT [FK_DocumentTags_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE)",
        "IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL CREATE TABLE [DocumentShares] ([DocumentShareId] int NOT NULL IDENTITY, [DocumentId] int NOT NULL, [SharedByUserId] int NOT NULL, [RecipientUserId] int NULL, [RecipientTeamKey] nvarchar(100) NULL, [CreatedDate] datetime2 NOT NULL, CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]), CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE, CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users] ([UserId]), CONSTRAINT [FK_DocumentShares_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([UserId]))",
        "IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL CREATE TABLE [DocumentActivities] ([DocumentActivityId] int NOT NULL IDENTITY, [DocumentId] int NOT NULL, [ActorUserId] int NOT NULL, [Action] nvarchar(40) NOT NULL, [CreatedDate] datetime2 NOT NULL, [Details] nvarchar(1000) NULL, CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]), CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE, CONSTRAINT [FK_DocumentActivities_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([UserId]))",
        "IF OBJECT_ID(N'[DocumentScanJobs]', N'U') IS NULL CREATE TABLE [DocumentScanJobs] ([DocumentScanJobId] int NOT NULL IDENTITY, [DocumentId] int NOT NULL, [StoragePath] nvarchar(500) NOT NULL, [RequestedDate] datetime2 NOT NULL, [StartedDate] datetime2 NULL, [CompletedDate] datetime2 NULL, [Status] nvarchar(20) NOT NULL, [AttemptCount] int NOT NULL, [FailureReason] nvarchar(1000) NULL, CONSTRAINT [PK_DocumentScanJobs] PRIMARY KEY ([DocumentScanJobId]), CONSTRAINT [FK_DocumentScanJobs_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE)",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents] ([UploadedByUserId], [UploadedDate])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId_ScanStatus' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_ProjectId_ScanStatus] ON [Documents] ([ProjectId], [ScanStatus])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ScanStatus' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_ScanStatus] ON [Documents] ([ScanStatus])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentTags_DocumentId_Value' AND object_id = OBJECT_ID(N'[DocumentTags]')) CREATE UNIQUE INDEX [IX_DocumentTags_DocumentId_Value] ON [DocumentTags] ([DocumentId], [Value])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentActivities_DocumentId_CreatedDate' AND object_id = OBJECT_ID(N'[DocumentActivities]')) CREATE INDEX [IX_DocumentActivities_DocumentId_CreatedDate] ON [DocumentActivities] ([DocumentId], [CreatedDate])",
        "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentScanJobs_Status_RequestedDate' AND object_id = OBJECT_ID(N'[DocumentScanJobs]')) CREATE INDEX [IX_DocumentScanJobs_Status_RequestedDate] ON [DocumentScanJobs] ([Status], [RequestedDate])"
    };

    foreach (var statement in statements)
        context.Database.ExecuteSqlRaw(statement);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapGet("/documents/file/{documentId:int}", async (int documentId, bool preview, HttpContext httpContext, DocumentService documentService, IFileStorageService storage, ApplicationDbContext context) =>
{
    if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
        return Results.Unauthorized();

    var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (claim == null || !int.TryParse(claim.Value, out var userId))
        return Results.NotFound();

    var document = await documentService.GetForDeliveryAsync(documentId, userId);
    if (document == null)
        return Results.NotFound();

    var stream = await storage.OpenReadAsync(document.FilePath);
    if (stream == null)
        return Results.NotFound();

    context.DocumentActivities.Add(new ContosoDashboard.Models.DocumentActivity
    {
        DocumentId = document.DocumentId,
        ActorUserId = userId,
        Action = preview ? "preview" : "download"
    });
    await context.SaveChangesAsync();
    return Results.File(stream, document.FileType, preview ? null : document.OriginalFileName, enableRangeProcessing: true);
});
app.MapFallbackToPage("/_Host");

app.Run();
