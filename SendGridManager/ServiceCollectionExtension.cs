using Microsoft.Extensions.DependencyInjection;
using System;

namespace SendGridManager
{
    public static class ServiceCollectionExtension
    {
        public static void AddSendGridManager(this ServiceCollection services)
        {
            services.AddScoped<ISendGridDownloadService, SendGridDownloadService>();
            services.AddScoped<ISendGridReportService, SendGridReportService>();

            services.AddHttpClient("sendgrid", c =>
            {
                c.BaseAddress = new Uri("https://api.sendgrid.com/v3/");
            });
        }
    }
}
