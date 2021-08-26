using System.Collections.Generic;
using System.Threading.Tasks;

namespace SendGridManager
{
    public interface ISendGridDownloadService
    {
        Task<List<TemplateInfo>> GetAllTemplatesAsync(string subscription);

        Task<TemplateInfo> GetTemplateAsync(string subscription, string templateId);
    }

    public interface ISendGridReportService
    {
        Task<string> ReportTemplatesAsync(List<TemplateInfo> templates);

        Task<string> ReportTemplateAsync(TemplateInfo version);
    }
}
