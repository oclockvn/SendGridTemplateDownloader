using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace SendGridManager
{
    public class SendGridDownloadService : ISendGridDownloadService
    {
        private readonly HttpClient _httpClient;
        private bool _initialized;

        public SendGridDownloadService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("sendgrid");
        }

        private void EnsureClient(string subscription)
        {
            if (_initialized)
            {
                return;
            }

            if (_httpClient == null)
            {
                throw new ArgumentNullException(nameof(_httpClient));
            }

            if (string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.AbsoluteUri))
            {
                throw new ArgumentException("Base address has not been set");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", subscription.Trim());

            _initialized = true;
        }

        public async Task<List<TemplateInfo>> GetAllTemplatesAsync(string subscription)
        {
            EnsureClient(subscription);

            var response = await _httpClient.GetAsync("templates?generations=dynamic");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<TemplateResult>(body);

            return result?.Templates;
        }

        public async Task<TemplateInfo> GetTemplateAsync(string subscription, string templateId)
        {
            EnsureClient(subscription);
            var response = await _httpClient.GetAsync("templates/" + templateId);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TemplateInfo>(body);

            return result;
        }


        public async Task<(string newTemplateId, string message)> TransferTemplateAsync(string fromApiKey, string toApiKey, string templateId)
        {
            TemplateInfo templateInfo = await GetTemplateAsync(fromApiKey, templateId).ConfigureAwait(false);
            if (templateInfo == null)
            {
                return new(null, $"Template was not found");
            }

            _initialized = false;
            EnsureClient(toApiKey);
            _initialized = false;

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var createTemplate = new CreateTemplate
            {
                Name = templateInfo.Name,
            };

            // create the template
            var jsonString = JsonSerializer.Serialize(createTemplate, serializeOptions);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("templates", content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new(null, "Error occurred creating the template");
            }
            var template = await response.Content.ReadAsStringAsync();
            var newTemplateInfo = JsonSerializer.Deserialize<TemplateInfo>(template);

            var createVersion = new CreateVersion
            {
                Name = templateInfo.ActiveVersion.Name,
                HtmlContent = templateInfo.ActiveVersion.HtmlContent,
                PlainContent = templateInfo.ActiveVersion.PlainContent,
                Subject = templateInfo.ActiveVersion.Subject,
            };

            // add the version
            jsonString = JsonSerializer.Serialize(createVersion, serializeOptions);
            content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync($"templates/{newTemplateInfo.Id}/versions", content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new(null, "Error occurred adding a version");
            }

            // activate the template version
            var version = await response.Content.ReadAsStringAsync();
            var templateVersionInfo = JsonSerializer.Deserialize<TemplateVersionInfo>(version);
            response = await _httpClient.PostAsync($"templates/{newTemplateInfo.Id}/versions/{templateVersionInfo.Id}/activate", content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new(null, "Error occurred activating the template version");
            }

            return new(newTemplateInfo.Id, null);
        }
    }

    public class SendGridReportService : ISendGridReportService
    {
        public async Task<string> ReportTemplatesAsync(List<TemplateInfo> templates)
        {
            if (templates == null || templates.Count == 0)
            {
                return string.Empty;
            }

            var headers = new[] { "Template Id", "Template Name", "Subject" }.ToCsvLine();
            var body = templates.Select(t => new[] { t.Id, t.Name, t.ActiveVersion?.Subject })
                .Select(s => s.ToCsvLine())
                .ToList();

            var csv = headers + Environment.NewLine + string.Join(Environment.NewLine, body);

            var fullPath = $"result-{DateTime.Now:yyyyMMddhhmmssffff}.csv";
            await File.WriteAllTextAsync(fullPath, csv);

            return fullPath;
        }

        public async Task<string> ReportTemplateAsync(string dir, TemplateInfo version)
        {
            if (version == null || version.ActiveVersion == null)
            {
                return string.Empty;
            }

            var folder = Path.Combine("Templates", dir ?? string.Empty);

            // try to create a folder if not exist
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // attempt to use name as file name
            return await WriteFileAsync(folder, version);
        }

        private async Task<string> WriteFileAsync(string folder, TemplateInfo template)
        {
            var templateVersion = template.ActiveVersion;
            var filename = template.Name;

            try
            {
                var name = filename.IsValidFileName() ? filename : filename.CorrectFileName();
                var path = Path.Combine(folder, name);
                var dir = Directory.CreateDirectory(path);
                path = dir.FullName;

                string createPath(string file) => Path.Combine(path, file);

                await File.WriteAllTextAsync(createPath("html_content.html"), templateVersion.HtmlContent);
                var metadata = new Metadata
                {
                    Active = 1,
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    HtmlContentHashed = templateVersion.HtmlContent.ComputeSha256Hash(),
                    Subject = templateVersion.Subject,
                    TestDataHashed = templateVersion.TestData.ComputeSha256Hash(),
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    VersionId = templateVersion.Id,
                    VersionName = templateVersion.Name,
                };

                await File.WriteAllTextAsync(createPath("metadata.json"), JsonSerializer.Serialize(metadata, typeof(Metadata), new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }));
                await File.WriteAllTextAsync(createPath("test_data.json"), templateVersion.TestData ?? string.Empty);

                return path;
            }
            catch (Exception e) // something went wrong when writing file
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
