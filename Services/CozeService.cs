using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Study.Models;

namespace Study.Services
{
    public class CozeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CozeService> _logger;
        private readonly IConfiguration _configuration;

        public CozeService(HttpClient httpClient, ILogger<CozeService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public class CozeParseResponse
        {
            public string paperId { get; set; } = string.Empty;
            public List<QuestionItem> questions { get; set; } = new();
            public class QuestionItem
            {
                public string questionId { get; set; } = string.Empty;
                public string content { get; set; } = string.Empty;
                public string? userAnswer { get; set; }
                public bool? isCorrect { get; set; }
                public string? correctAnswer { get; set; }
                public string knowledgePoint { get; set; } = string.Empty;
                public string subject { get; set; } = string.Empty;
                public string questionType { get; set; } = string.Empty;
                public List<string>? options { get; set; }
                public string errorAnalysis { get; set; } = string.Empty;
            }
        }

        public async Task<(CozeParseResponse? Result, string Error, string FileId)> ParsePaperImageAsync(string base64Image, int grade, string subject, string? publisher)
        {
            var baseUrl = _configuration["Coze:BaseUrl"] ?? "";
            var workflowId = _configuration["Coze:WorkflowIdParse"] ?? "";
            var apiKey = _configuration["Coze:ApiKey"] ?? "";
            
            if (string.IsNullOrEmpty(baseUrl)) return (null, "Coze:BaseUrl is missing", "");
            if (string.IsNullOrEmpty(workflowId)) return (null, "Coze:WorkflowIdParse is missing", "");
            if (string.IsNullOrEmpty(apiKey)) return (null, "Coze:ApiKey is missing", "");
            // 1) 将前端base64图片转为字节并上传到 Coze，获取 file_id
            string fileId;
            try
            {
                var imageBytes = Convert.FromBase64String(base64Image);
                var uploadUrl = $"{baseUrl.TrimEnd('/')}/files/upload";
                Console.WriteLine($"[COZE] Upload start: url={uploadUrl}, bytes={imageBytes.Length}");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                content.Add(fileContent, "file", "upload.png");

                using var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = content };
                uploadReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var uploadResp = await _httpClient.SendAsync(uploadReq);
                Console.WriteLine($"[COZE] Upload resp: status={(int)uploadResp.StatusCode} {uploadResp.StatusCode}");
                if (!uploadResp.IsSuccessStatusCode)
                {
                    var err = await uploadResp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[COZE] Upload error body: {err}");
                    return (null, $"Coze upload failed: {uploadResp.StatusCode} - {err}", "");
                }

                var uploadJson = await uploadResp.Content.ReadAsStringAsync();
                Console.WriteLine($"[COZE] Upload ok body: {uploadJson}");
                using var doc = JsonDocument.Parse(uploadJson);
                // 兼容返回格式：{"code":0,"data":{"id":"xxxx"}}
                if (!doc.RootElement.TryGetProperty("data", out var dataElem) || !dataElem.TryGetProperty("id", out var idElem))
                {
                    Console.WriteLine("[COZE] Upload parse error: missing data.id");
                    return (null, "Coze upload response missing data.id", "");
                }
                fileId = idElem.GetString() ?? string.Empty;
                Console.WriteLine($"[COZE] file_id={fileId}");
                if (string.IsNullOrEmpty(fileId)) return (null, "Coze upload returned empty file_id", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coze file upload error");
                Console.WriteLine($"[COZE] Upload exception: {ex.Message}");
                return (null, $"Upload exception: {ex.Message}", "");
            }

            // 2) 使用 file_id 调用工作流
            var workflowUrl = $"{baseUrl.TrimEnd('/')}/workflow/stream_run";
            var imageParam = JsonSerializer.Serialize(new { file_id = fileId });
            Console.WriteLine($"[COZE] Workflow upload start: url={workflowUrl}, workflow_id={workflowId}, imageParam={imageParam}, grade={grade}, subject={subject}, publisher={publisher}");
            var payload = new
            {
                workflow_id = workflowId,
                parameters = new
                {
                    grade = $"{grade}年级",
                    subject = subject,
                    publisher = publisher ?? "",
                    image = imageParam
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, workflowUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var resp = await _httpClient.SendAsync(req);
                Console.WriteLine($"[COZE] Workflow resp: status={(int)resp.StatusCode} {resp.StatusCode}");
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    return (null, $"Workflow run failed: {resp.StatusCode} - {err}", fileId);
                }

                var innerJson = await ParseCozeStreamResponseAsync(resp);

                if (string.IsNullOrEmpty(innerJson))
                {
                    return (null, "Workflow data.data is empty", fileId);
                }

                // Manual parsing to handle Coze output format (data might be stringified JSON with "output" array)
                var parsed = new CozeParseResponse { paperId = Guid.NewGuid().ToString() };
                try
                {
                    using var dataDoc = JsonDocument.Parse(innerJson);
                    var dataRoot = dataDoc.RootElement;
                    
                    // Check for "output" array which is the common pattern
                    if (dataRoot.TryGetProperty("output", out var outputElem) && outputElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in outputElem.EnumerateArray())
                        {
                            parsed.questions.Add(new CozeParseResponse.QuestionItem
                            {
                                questionId = item.TryGetProperty("questionId", out var qid) ? (qid.GetString() ?? "") : "",
                                content = item.TryGetProperty("content", out var content) ? (content.GetString() ?? "") : "",
                                userAnswer = item.TryGetProperty("userAnswer", out var ua) ? ua.GetString() : null,
                                isCorrect = item.TryGetProperty("isCorrect", out var ic) ? (ic.ValueKind == JsonValueKind.Null ? (bool?)null : ic.GetBoolean()) : null,
                                correctAnswer = item.TryGetProperty("correctAnswer", out var ca) ? ca.GetString() : null,
                                knowledgePoint = item.TryGetProperty("knowledgePoint", out var kp) ? (kp.GetString() ?? "") : "",
                                subject = item.TryGetProperty("subject", out var subj) ? (subj.GetString() ?? "") : "",
                                questionType = item.TryGetProperty("questionType", out var qt) ? (qt.GetString() ?? "") : "",
                                errorAnalysis = item.TryGetProperty("errorAnalysis", out var ea) ? (ea.GetString() ?? "") : ""
                            });
                        }
                    }
                    else
                    {
                        // Try direct deserialization if "output" wrapper is missing
                        var directParsed = JsonSerializer.Deserialize<CozeParseResponse>(innerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (directParsed != null)
                        {
                            if (directParsed.questions != null && directParsed.questions.Count > 0)
                            {
                                parsed.questions = directParsed.questions;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[COZE] Data parse exception: {ex.Message}");
                }

                return (parsed, string.Empty, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coze workflow failed");
                return (null, $"Workflow exception: {ex.Message}", fileId);
            }
        }

        public async Task<List<PracticeQuestion>> GeneratePracticeQuestionsAsync(string userId, List<string> knowledgePoints, List<string> questionTypeSpecs, int grade, string subject, string? publisher)
        {
            var baseUrl = _configuration["Coze:BaseUrl"] ?? "";
            var workflowId = _configuration["Coze:WorkflowIdGenerate"] ?? "";
            var apiKey = _configuration["Coze:ApiKey"] ?? "";
            var url = $"{baseUrl.TrimEnd('/')}/workflow/stream_run";

            var payload = new
            {
                workflow_id = workflowId,
                parameters = new
                {
                    grade = $"{grade}年级",
                    knowledgePoints = knowledgePoints,
                    questionTypeSpecs = questionTypeSpecs,
                    subject = subject,
                    publisher = publisher ?? ""
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                Console.WriteLine($"[COZE_GEN] Calling workflow_id={workflowId} grade={grade} subject={subject} publisher={publisher} kps={knowledgePoints.Count} specs={string.Join(",", questionTypeSpecs)}");
                var resp = await _httpClient.SendAsync(req);
                Console.WriteLine($"[COZE_GEN] Response status: {resp.StatusCode}");
                resp.EnsureSuccessStatusCode();
                
                var innerJson = await ParseCozeStreamResponseAsync(resp);
                
                if (string.IsNullOrEmpty(innerJson))
                {
                    _logger.LogError("Coze generate workflow returned empty result (stream)");
                    return new List<PracticeQuestion>();
                }

                var parsed = new CozeParseResponse();
                try 
                {
                    // innerJson might be a direct JSON array or object
                    // Try to parse it to see if it matches our structure or if it needs further unwrapping
                    using var innerDoc = JsonDocument.Parse(innerJson);
                    if (innerDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        // Direct array of questions
                        parsed.questions = JsonSerializer.Deserialize<List<CozeParseResponse.QuestionItem>>(innerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    }
                    else if (innerDoc.RootElement.TryGetProperty("output", out var outputElem))
                    {
                        // Wrapped in "output" property
                        if (outputElem.ValueKind == JsonValueKind.Array)
                        {
                             parsed.questions = JsonSerializer.Deserialize<List<CozeParseResponse.QuestionItem>>(outputElem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                        }
                        else if (outputElem.ValueKind == JsonValueKind.String)
                        {
                             // Double encoded?
                             parsed.questions = JsonSerializer.Deserialize<List<CozeParseResponse.QuestionItem>>(outputElem.GetString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                        }
                    }
                    else
                    {
                         // Try object deserialization
                         var temp = JsonSerializer.Deserialize<CozeParseResponse>(innerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                         if (temp?.questions != null) parsed = temp;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[COZE] Inner parse error: {ex.Message}");
                }

                var result = new List<PracticeQuestion>();
                foreach (var q in parsed.questions)
                {
                    result.Add(new PracticeQuestion
                    {
                        Id = !string.IsNullOrWhiteSpace(q.questionId) ? q.questionId : Guid.NewGuid().ToString(),
                        Content = q.content,
                        CorrectAnswer = q.correctAnswer,
                        KnowledgePoint = q.knowledgePoint,
                        Subject = !string.IsNullOrWhiteSpace(q.subject) ? q.subject : subject,
                        Grade = grade,
                        QuestionType = !string.IsNullOrWhiteSpace(q.questionType) ? q.questionType : "练习题",
                        Options = q.options != null ? JsonSerializer.Serialize(q.options) : null,
                        UserId = userId
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coze generate workflow failed");
                return new List<PracticeQuestion>();
            }
        }

        private async Task<string?> ParseCozeStreamResponseAsync(HttpResponseMessage resp)
        {
            var content = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[COZE] Stream raw len: {content.Length}");
            
            var lines = content.Split('\n');
            string currentEvent = "";
            
            foreach (var line in lines)
            {
                var trim = line.Trim();
                if (string.IsNullOrEmpty(trim)) continue;

                if (trim.StartsWith("event:"))
                {
                    currentEvent = trim.Substring(6).Trim();
                }
                else if (trim.StartsWith("data:"))
                {
                    var data = trim.Substring(5).Trim();
                    if (currentEvent == "workflow_finished")
                    {
                        try 
                        {
                            Console.WriteLine($"[COZE] Found workflow_finished: {data}");
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("data", out var dataElem))
                            {
                                return dataElem.GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[COZE] Error parsing workflow_finished data: {ex.Message}");
                        }
                    }
                    else if (currentEvent == "Message")
                    {
                        try
                        {
                            // Console.WriteLine($"[COZE] Found Message event data: {data}");
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("content", out var contentElem))
                            {
                                var contentStr = contentElem.GetString();
                                if (!string.IsNullOrEmpty(contentStr) && contentStr.Contains("\"output\""))
                                {
                                    Console.WriteLine($"[COZE] Found output in Message content: {contentStr.Substring(0, Math.Min(50, contentStr.Length))}...");
                                    return contentStr;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                             // Ignore parsing errors for non-JSON messages
                        }
                    }
                }
            }
            return null;
        }

        public class AdviceResult
        {
            public string summary { get; set; } = string.Empty;
            public string tone { get; set; } = string.Empty;
            public List<string> suggestions { get; set; } = new();
        }

        public async Task<AdviceResult?> GetLearningAdviceAsync(string userId, int grade, string subject, List<string> weakPoints)
        {
            var baseUrl = _configuration["Coze:BaseUrl"] ?? "";
            var workflowId = _configuration["Coze:WorkflowIdAdvice"] ?? "";
            var apiKey = _configuration["Coze:ApiKey"] ?? "";
            var url = $"{baseUrl.TrimEnd('/')}/workflows/{workflowId}/run";

            var prompt = $"为用户:{userId}提供学习建议。年级:{grade}，学科:{subject}，薄弱知识点:{string.Join(";", weakPoints)}。请返回JSON包含summary、tone、suggestions。";
            var payload = new
            {
                input_type = "text",
                prompt
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var resp = await _httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var result = await resp.Content.ReadFromJsonAsync<AdviceResult>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
