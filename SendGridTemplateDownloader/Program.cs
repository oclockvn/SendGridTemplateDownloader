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

            Console.Write("Enter subscription key: ");
            var subscription = Console.ReadLine().Trim();

            Console.Write("Enter subscription key to transfer to (Optional): ");
            var toApiKey = Console.ReadLine();

            Console.Write("Enter output folder: ");
            var output = Console.ReadLine().Trim();

            var downloadService = sp.GetRequiredService<ISendGridDownloadService>();
            var reportService = sp.GetRequiredService<ISendGridReportService>();

            var result = await downloadService.GetAllTemplatesAsync(subscription);

            if (result == null || result.Count == 0)
            {
                Console.WriteLine("No templates found");
                return;
            }

            Console.WriteLine($"Found {result.Count} templates:");
            for (var i = 0; i < result.Count; i++)
            {
                Console.WriteLine($"{i} - {result[i].Name}");
            }

            await reportService.ReportTemplatesAsync(result);

            Console.WriteLine("Use the pattern \"i:1,2,4\" to get template at index 1, 2 and 4. Without \"i:\", everything else is treated as normal template name");
            Console.Write("Enter template name or indexes to fetch ('-1' to fetch all): ");
            var command = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("Thank you!");
                return;
            }

            var names = GetTemplateNames(command, result);
            foreach (var name in names)
            {
                await DownloadAndReportAsync(output, name, subscription, result, reportService, downloadService, toApiKey);
            }

            Console.WriteLine("Done!");
        }

        static IEnumerable<string> GetTemplateNames(string command, List<TemplateInfo> templates)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            if (command == "-1") // get all
            {
                return templates.Select(x => x.Name);
            }

            if (command.StartsWith("i:")) // get by indexes
            {
                var indexes = command
                    .Substring(2) // i:
                    .Split(',') // 1,2,3
                    .Select(x => int.TryParse(x, out var i) ? i : -1)
                    .Where(x => x > -1) // remove invalid number
                    .ToList();

                var result = new List<string>();
                foreach (var i in indexes)
                {
                    if (i <= templates.Count)
                    {
                        result.Add(templates[i].Name);
                    }
                }

                return result;
            }

            // normal template name
            return templates.Where(t => t.Name == command).Select(x => x.Name);
        }

        static async Task DownloadAndReportAsync(
            string output,
            string templateName,
            string subscription,
            List<TemplateInfo> result,
            ISendGridReportService reportService,
            ISendGridDownloadService downloadService,
            string toApiKey = null)
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
                await reportService.ReportTemplateAsync(output, version);
            }
            else
            {
                var results = await downloadService.TransferTemplateAsync(subscription, toApiKey, templateId);
                if (string.IsNullOrEmpty(results.newTemplateId))
                {
                    Console.WriteLine($"Failed to transfer template {templateName}:{templateId} - {results.message}");
                }
                else
                {
                    Console.WriteLine($"Transferred template {templateName}:{templateId} to {results.newTemplateId}");
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
