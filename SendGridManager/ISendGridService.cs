using System.Collections.Generic;
using System.Threading.Tasks;

namespace SendGridManager
{
    public interface ISendGridDownloadService
    {
        Task<List<TemplateInfo>> GetAllTemplatesAsync(string subscription);

        Task<TemplateInfo> GetTemplateAsync(string subscription, string templateId);

        Task<(string newTemplateId, string message)> TransferTemplateAsync(string fromApiKey, string toApiKey, string templateId);
    }

    public interface ISendGridReportService
    {
        Task<string> ReportTemplatesAsync(List<TemplateInfo> templates);

        Task<string> ReportTemplateAsync(string dir, TemplateInfo version);
    }
}
