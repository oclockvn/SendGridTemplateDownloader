using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
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

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", subscription);

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
            throw new NotImplementedException();
        }

        public async Task<string> ReportTemplateAsync(TemplateVersionInfo version)
        {
            throw new NotImplementedException();
        }
    }
}
