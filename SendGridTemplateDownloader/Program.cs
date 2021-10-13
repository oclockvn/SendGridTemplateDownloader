using Microsoft.Extensions.DependencyInjection;
using SendGridManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SendGridTemplateDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sp = Setup();

            Console.Write("Enter subscription: ");
            var subscription = Console.ReadLine().Trim();

            var downloadService = sp.GetRequiredService<ISendGridDownloadService>();
            var reportService = sp.GetRequiredService<ISendGridReportService>();

            var result = await downloadService.GetAllTemplatesAsync(subscription);

            if (result == null || result.Count == 0)
            {
                Console.WriteLine("No templates found");
                return;
            }

            Console.WriteLine($"Found {result.Count} templates:");
            Console.WriteLine(string.Join(Environment.NewLine, result.Select(t => t.Name)));

            await reportService.ReportTemplatesAsync(result);

            Console.Write("Enter template name to fetch ('all' to get all): ");
            var templateName = Console.ReadLine();

            Console.Write("Enter account API key to transfer to (Optional): ");
            var toApiKey = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                Console.WriteLine("Thank you!");
                return;
            }

            if (templateName == "all")
            {
                foreach (var name in result.Select(r => r.Name))
                {
                    await DownloadAndReportAsync(name, subscription, result, reportService, downloadService, toApiKey);
                }
            }
            else
            {
                await DownloadAndReportAsync(templateName, subscription, result, reportService, downloadService, toApiKey);
            }

            Console.WriteLine("Done!");
        }

        static async Task DownloadAndReportAsync(string templateName, string subscription, List<TemplateInfo> result, ISendGridReportService reportService, ISendGridDownloadService downloadService, string toApiKey = null)
        {
            var templateId = result.FirstOrDefault(t => t.Name == templateName.Trim())?.Id;
            if (string.IsNullOrWhiteSpace(templateId))
            {
                Console.WriteLine("No template id found");
                return;
            }

            if (string.IsNullOrEmpty(toApiKey))
            {
                Console.WriteLine($"Downloading template {templateName}...");
                var version = await downloadService.GetTemplateAsync(subscription, templateId);
                await reportService.ReportTemplateAsync(version);
            }
            else
            {
                var newTemplateId = await downloadService.TransferTemplate(subscription, toApiKey, templateId);
                if (string.IsNullOrEmpty(newTemplateId))
                {
                    Console.WriteLine($"Failed to transfer template {templateName}:{templateId}");
                }
                else
                {
                    Console.WriteLine($"Transferred template {templateName}:{templateId} to {newTemplateId}");
                }
            }
        }

        static IServiceProvider Setup()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSendGridManager();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
