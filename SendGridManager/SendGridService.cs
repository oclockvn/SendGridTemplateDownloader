using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

            var result = System.Text.Json.JsonSerializer.Deserialize<TemplateResult>(body);

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
            var result = System.Text.Json.JsonSerializer.Deserialize<TemplateInfo>(body);

            return result;
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
            var body = templates.Select(t => new []{ t.Id, t.Name, t.ActiveVersion?.Subject })
                .Select(s => s.ToCsvLine())
                .ToList();

            var csv = headers + Environment.NewLine + string.Join(Environment.NewLine, body);

            var fullPath = $"result-{DateTime.Now:yyyyMMddhhmmssffff}.csv";
            await File.WriteAllTextAsync(fullPath, csv);

            return fullPath;
        }

        public async Task<string> ReportTemplateAsync(TemplateInfo version)
        {
            if (version == null || version.ActiveVersion == null)
            {
                return string.Empty;
            }

            var folder = "Templates";

            // try to create a folder if not exist
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // not to care about the existing file, just make a unique name
            var unique = version.Name.NormalizeFolderName() + "-" + DateTime.Now.ToString("yyyyMMddThhmmssfff");
            var htmlPath = Path.Combine(folder, $"{unique}.html");
            var textPath = Path.Combine(folder, $"{unique}.txt");
            await File.WriteAllTextAsync(htmlPath, version.ActiveVersion.HtmlContent);
            await File.WriteAllTextAsync(textPath, version.ActiveVersion.PlainContent);

            return htmlPath;
        }
    }
}
