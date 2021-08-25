using Microsoft.Extensions.DependencyInjection;
using SendGridManager;
using System;
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

            var downloadManager = sp.GetRequiredService<ISendGridDownloadService>();
            var result = await downloadManager.GetAllTemplatesAsync(subscription);

            if (result == null || result.Count == 0)
            {
                Console.WriteLine("No templtes found");
                return;
            }

            Console.WriteLine($"Found {result.Count} templates:");
            Console.WriteLine(string.Join(Environment.NewLine, result.Select(t => t.Name)));

            Console.Write("Enter template name to fetch: ");
            var templateName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                Console.WriteLine("Thank you!");
                return;
            }

            var templateId = result.FirstOrDefault(t => t.Name == templateName.Trim())?.Id;
            if (string.IsNullOrWhiteSpace(templateId))
            {
                Console.WriteLine("No template id found");
                return;
            }

            var version = await downloadManager.GetTemplateAsync(subscription, templateId);
            var reportService = sp.GetRequiredService<ISendGridReportService>();

            await reportService.ReportTemplateAsync(version.ActiveVersion);

            Console.WriteLine("Done!");
        }

        static IServiceProvider Setup()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSendGridManager();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
