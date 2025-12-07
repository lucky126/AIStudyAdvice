using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Study.Data;
using Study.Services;
using Study.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

builder.Services.AddHttpClient<CozeService>(client =>
{
    var baseUrl = builder.Configuration["Coze:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});
builder.Services.AddScoped<CozeService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:8099") });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    try
    {       
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" serial PRIMARY KEY,
                ""Username"" text NOT NULL,
                ""PasswordHash"" text NOT NULL,
                ""Nickname"" text,
                ""RegistrationTime"" timestamp with time zone NOT NULL,
                ""UsedInvitationCode"" text,
                ""LoginCount"" integer NOT NULL DEFAULT 0,
                ""LastLoginTime"" timestamp with time zone,
                ""IsActive"" boolean NOT NULL DEFAULT TRUE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");

            CREATE TABLE IF NOT EXISTS ""InvitationCodes"" (
                ""Id"" serial PRIMARY KEY,
                ""Code"" character varying(8) NOT NULL,
                ""IsUsed"" boolean NOT NULL DEFAULT FALSE,
                ""UsedByUsername"" text,
                ""UsedTime"" timestamp with time zone,
                ""CreatedTime"" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InvitationCodes_Code"" ON ""InvitationCodes"" (""Code"");

            CREATE TABLE IF NOT EXISTS ""AdviceHistories"" (
                ""Id"" serial PRIMARY KEY,
                ""UserId"" text NOT NULL,
                ""Grade"" text,
                ""Subject"" text,
                ""Textbook"" text,
                ""RequestHash"" text NOT NULL,
                ""ResponseContent"" text NOT NULL,
                ""CreateTime"" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_AdviceHistories_Lookup"" ON ""AdviceHistories"" (""UserId"", ""Grade"", ""Subject"", ""Textbook"", ""RequestHash"");
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration warning: {ex.Message}");
    }
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/config", (int? grade, string? subject) => Results.Ok(new { grade = grade ?? 0, subject = subject ?? "" }));

app.MapPost("/api/upload", async (AppDbContext db, CozeService coze, UploadDto dto) =>
{
    Console.WriteLine($"[UPLOAD] start grade={dto.Grade} subject={dto.Subject} publisher={dto.Publisher} base64_len={(dto.Base64Image?.Length ?? 0)}");
    var (parsed, error, fileId) = await coze.ParsePaperImageAsync(dto.Base64Image, dto.Grade, dto.Subject, dto.Publisher);
    if (parsed == null)
    {
        Console.WriteLine($"[UPLOAD] coze parse failed: {error}");
        return Results.Problem(detail: error, statusCode: 502);
    }

    var paperId = string.IsNullOrWhiteSpace(parsed.paperId) ? Guid.NewGuid().ToString() : parsed.paperId;
    Console.WriteLine($"[UPLOAD] parsed paperId={paperId} questions={(parsed.questions?.Count ?? 0)}");

    // Save paper info
    try
    {
        var paper = new Paper
        {
            PaperId = Guid.TryParse(paperId, out var pid) ? pid : Guid.NewGuid(),
            UserId = dto.UserId ?? "demo_user",
            Subject = dto.Subject,
            FileId = fileId,
            CreateTime = DateTime.UtcNow
        };
        // Ensure PaperId matches what we use for questions
        if (paper.PaperId.ToString() != paperId) paperId = paper.PaperId.ToString();

        await db.Papers.AddAsync(paper);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[UPLOAD] Save paper failed: {ex.Message}");
    }

    var toAdd = new List<Question>();
    foreach (var q in parsed.questions)
    {
        toAdd.Add(new Question
        {
            QuestionId = string.IsNullOrWhiteSpace(q.questionId) ? Guid.NewGuid().ToString() : q.questionId,
            UserId = dto.UserId ?? "demo_user",
            Content = q.content,
            UserAnswer = q.userAnswer,
            IsCorrect = q.isCorrect,
            CorrectAnswer = q.correctAnswer,
            KnowledgePoint = q.knowledgePoint,
            Subject = dto.Subject,
            Grade = dto.Grade,
            QuestionType = q.questionType,
            ErrorAnalysis = q.errorAnalysis,
            PaperId = paperId
        });
    }
    await db.Questions.AddRangeAsync(toAdd);
    await db.SaveChangesAsync();
    Console.WriteLine($"[UPLOAD] saved questions={toAdd.Count} paperId={paperId}");

    await UpdateKnowledgeStats(db, dto.UserId ?? "demo_user", dto.Grade, dto.Subject, toAdd);
    Console.WriteLine("[UPLOAD] knowledge stats updated");

    return Results.Ok(new
    {
        paperId,
        questions = toAdd.Select(q => new
        {
            q.QuestionId,
            q.Content,
            q.UserAnswer,
            q.IsCorrect,
            q.CorrectAnswer,
            q.KnowledgePoint,
            q.Subject,
            q.Grade,
            q.QuestionType,
            q.ErrorAnalysis
        })
    });
});

app.MapGet("/api/analytics", async (AppDbContext db, int grade, string subject, string? userId) =>
{
    userId ??= "demo_user";
    var stats = await db.KnowledgeStats
        .Where(k => k.UserId == userId && k.Grade == grade && k.Subject == subject)
        .OrderBy(k => k.Accuracy)
        .ToListAsync();

    return Results.Ok(new
    {
        userId = userId,
        grade,
        subject,
        report = stats.Select(s => new
        {
            knowledgePoint = s.KnowledgePoint,
            accuracy = s.Accuracy,
            mastery = s.MasteryLevel,
            correct = s.Correct,
            total = s.Total
        }).ToList()
    });
});

app.MapGet("/api/questions", async (AppDbContext db, int grade, string subject, string kp, string? userId) =>
{
    userId ??= "demo_user";
    var list = await db.Questions
        .Where(q => q.UserId == userId && q.Grade == grade && q.Subject == subject && q.KnowledgePoint == kp)
        .OrderBy(q => q.PaperId)
        .ToListAsync();

    return Results.Ok(list.Select(q => new
    {
        q.QuestionId,
        q.QuestionType,
        q.Content,
        q.IsCorrect,
        q.UserAnswer,
        q.CorrectAnswer,
        q.ErrorAnalysis,
        q.KnowledgePoint,
        q.Subject,
        q.Grade
    }).ToList());
});

app.MapPost("/api/practice/generate", async (AppDbContext db, CozeService coze, GeneratePracticeDto dto) =>
{
    var questions = await coze.GeneratePracticeQuestionsAsync(dto.UserId ?? "demo_user", dto.KnowledgePoints, dto.QuestionTypeSpecs, dto.Grade, dto.Subject, dto.Publisher);
    var paperId = Guid.NewGuid().ToString();
    foreach (var q in questions)
    {
        q.PaperId = paperId;
        if (string.IsNullOrWhiteSpace(q.Id))
            q.Id = Guid.NewGuid().ToString();
    }
    await db.PracticeQuestions.AddRangeAsync(questions);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        paperId,
        questions = questions.Select(q => new
        {
            QuestionId = q.Id,
            q.Content,
            q.CorrectAnswer,
            q.KnowledgePoint,
            q.Subject,
            q.Grade,
            q.QuestionType,
            q.Options
        }).ToList()
    });
});

app.MapPost("/api/practice/submit", async (AppDbContext db, SubmitPracticeDto dto) =>
{
    var toUpdate = new List<Question>();
    foreach (var ans in dto.Answers)
    {
        var q = await db.Questions.FirstOrDefaultAsync(x =>
            x.QuestionId == ans.QuestionId && x.Grade == dto.Grade && x.Subject == dto.Subject);
        if (q != null)
        {
            q.UserAnswer = ans.UserAnswer;
            q.IsCorrect = ans.IsCorrect;
            q.ErrorAnalysis = ans.ErrorAnalysis ?? "";
            toUpdate.Add(q);
        }
        else
        {
            var nq = new Question
            {
                QuestionId = ans.QuestionId,
                UserId = dto.UserId ?? "demo_user",
                Content = ans.Content,
                UserAnswer = ans.UserAnswer,
                IsCorrect = ans.IsCorrect,
                CorrectAnswer = ans.CorrectAnswer,
                KnowledgePoint = ans.KnowledgePoint,
                Subject = dto.Subject,
                Grade = dto.Grade,
                QuestionType = ans.QuestionType,
                ErrorAnalysis = ans.ErrorAnalysis ?? "",
                PaperId = dto.PaperId ?? Guid.NewGuid().ToString()
            };
            await db.Questions.AddAsync(nq);
            toUpdate.Add(nq);
        }
    }
    await db.SaveChangesAsync();

    await UpdateKnowledgeStats(db, dto.UserId ?? "demo_user", dto.Grade, dto.Subject, toUpdate);

    return Results.Ok(new { ok = true });
});

app.MapGet("/api/practice/history", async (AppDbContext db, string? userId) =>
{
    userId ??= "demo_user";
    var list = await db.PracticeQuestions
        .Where(q => q.UserId == userId)
        .GroupBy(q => new { q.PaperId, q.Subject, q.Grade })
        .Select(g => new
        {
            g.Key.PaperId,
            g.Key.Subject,
            g.Key.Grade,
            CreateTime = g.Max(q => q.CreateTime),
            Count = g.Count()
        })
        .OrderByDescending(x => x.CreateTime)
        .ToListAsync();
    return Results.Ok(list);
});

app.MapGet("/api/practice/paper/{paperId}", async (AppDbContext db, string paperId) =>
{
    var questions = await db.PracticeQuestions
        .Where(q => q.PaperId == paperId)
        .OrderBy(q => q.QuestionType) // Group by type roughly
        .ToListAsync();
        
    return Results.Ok(questions.Select(q => new
    {
        QuestionId = q.Id,
        q.Content,
        q.CorrectAnswer,
        q.KnowledgePoint,
        q.Subject,
        q.Grade,
        q.QuestionType,
        q.Options,
        q.UserAnswer
    }));
});

app.MapPost("/api/advice", async (AppDbContext db, CozeService coze, AdviceRequest dto) =>
{
    Console.WriteLine($"[API_ADVICE] Received request: grade={dto.Grade}, subject={dto.Subject}, user={dto.UserId}, pub={dto.Publisher}");
    var uid = dto.UserId ?? "demo_user";
    var stats = await db.KnowledgeStats
        .Where(k => k.UserId == uid && k.Grade == dto.Grade && k.Subject == dto.Subject)
        .OrderBy(k => k.Accuracy)
        .Take(10)
        .ToListAsync();

    var adviceStats = new List<Study.Services.CozeService.AdviceKnowledgeStat>();
    
    foreach (var stat in stats)
    {
        // Get error analyses for this KP (limit to recent/relevant ones to avoid token overflow)
        var errors = await db.Questions
            .Where(q => q.UserId == uid && q.Grade == dto.Grade && q.Subject == dto.Subject && q.KnowledgePoint == stat.KnowledgePoint && q.IsCorrect == false && !string.IsNullOrEmpty(q.ErrorAnalysis))
            .OrderByDescending(q => q.PaperId) // Assuming newer first, or could add CreatedTime
            .Select(q => q.ErrorAnalysis)
            .Take(5)
            .ToListAsync();

        adviceStats.Add(new Study.Services.CozeService.AdviceKnowledgeStat
        {
            knowledgePoint = stat.KnowledgePoint,
            accuracy = stat.Accuracy,
            proficiency = stat.MasteryLevel,
            errorAnalyses = errors
        });
    }

    var input = new Study.Services.CozeService.AdviceInput
    {
        userId = uid,
        grade = $"{dto.Grade}年级",
        subject = dto.Subject,
        textbook = dto.Publisher ?? "",
        knowledgeStats = adviceStats
    };

    // Calculate Hash
    var inputJson = JsonSerializer.Serialize(input);
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputJson));
    var hash = Convert.ToBase64String(hashBytes);

    Console.WriteLine($"[API_ADVICE] Request Hash: {hash}");

    // Check Cache
    var cached = await db.AdviceHistories
        .Where(h => h.UserId == uid && h.Grade == dto.Grade.ToString() && h.Subject == dto.Subject && h.Textbook == (dto.Publisher ?? "") && h.RequestHash == hash)
        .OrderByDescending(h => h.CreateTime)
        .FirstOrDefaultAsync();

    if (cached != null)
    {
        Console.WriteLine("[API_ADVICE] Cache hit, returning stored advice.");
        try 
        {
            var cachedResult = JsonSerializer.Deserialize<Study.Services.CozeService.AdviceResult>(cached.ResponseContent);
            if (cachedResult != null) return Results.Ok(cachedResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API_ADVICE] Cache deserialization failed: {ex.Message}");
        }
    }

    Console.WriteLine("[API_ADVICE] Cache miss, calling Coze...");
    var advice = await coze.GetLearningAdviceAsync(input);
    
    if (advice != null)
    {
        // Save to Cache
        try 
        {
            var history = new AdviceHistory
            {
                UserId = uid,
                Grade = dto.Grade.ToString(),
                Subject = dto.Subject,
                Textbook = dto.Publisher ?? "",
                RequestHash = hash,
                ResponseContent = JsonSerializer.Serialize(advice),
                CreateTime = DateTime.UtcNow
            };
            await db.AdviceHistories.AddAsync(history);
            await db.SaveChangesAsync();
            Console.WriteLine("[API_ADVICE] Advice saved to cache.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API_ADVICE] Failed to save cache: {ex.Message}");
        }
    }

    return Results.Ok(advice ?? new Study.Services.CozeService.AdviceResult());
});
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static string ToMastery(double accuracy)
{
    if (accuracy > 0.9) return "熟练掌握";
    if (accuracy > 0.75) return "掌握";
    if (accuracy > 0.6) return "了解";
    return "不明白";
}

static async Task UpdateKnowledgeStats(AppDbContext db, string userId, int grade, string subject, List<Question> questions)
{
    var grouped = questions
        .Where(q => !string.IsNullOrWhiteSpace(q.KnowledgePoint))
        .GroupBy(q => q.KnowledgePoint);

    foreach (var g in grouped)
    {
        var kp = g.Key;
        var total = g.Count();
        var correct = g.Count(q => q.IsCorrect == true);
        var accuracy = total == 0 ? 0 : (double)correct / total;

        var stat = await db.KnowledgeStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Grade == grade && s.Subject == subject && s.KnowledgePoint == kp);

        if (stat == null)
        {
            stat = new KnowledgeStat
            {
                UserId = userId,
                Grade = grade,
                Subject = subject,
                KnowledgePoint = kp,
                Total = total,
                Correct = correct,
                Accuracy = Math.Round(accuracy, 4),
                MasteryLevel = ToMastery(accuracy)
            };
            await db.KnowledgeStats.AddAsync(stat);
        }
        else
        {
            stat.Total += total;
            stat.Correct += correct;
            stat.Accuracy = Math.Round((double)stat.Correct / Math.Max(1, stat.Total), 4);
            stat.MasteryLevel = ToMastery(stat.Accuracy);
            db.KnowledgeStats.Update(stat);
        }
    }

    await db.SaveChangesAsync();
}

public record UploadDto(string Base64Image, int Grade, string Subject, string? Publisher, string? UserId = "demo_user");
public record GeneratePracticeDto(string UserId, List<string> KnowledgePoints, List<string> QuestionTypeSpecs, int Grade, string Subject, string? Publisher);
public record SubmitPracticeDto(string? PaperId, int Grade, string Subject, List<SubmitAnswer> Answers, string? UserId = "demo_user");
public record SubmitAnswer(string QuestionId, string Content, string? UserAnswer, bool IsCorrect, string? CorrectAnswer, string KnowledgePoint, string QuestionType, string? ErrorAnalysis);
public class AdviceRequest
{
    public int Grade { get; set; }
    public string Subject { get; set; } = "";
    public string? UserId { get; set; }
    public string? Publisher { get; set; }
}
