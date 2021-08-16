using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Bit.CryptoAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureLogging((hostingContext, logging) =>
                    {
                        var settings = new CryptoAgentSettings();
                        ConfigurationBinder.Bind(
                            hostingContext.Configuration.GetSection("CryptoAgentSettings"), settings);

                        var serilogConfig = new LoggerConfiguration()
                            .Enrich.FromLogContext();

                        if (!string.IsNullOrWhiteSpace(settings.LogFilePath))
                        {
                            serilogConfig.WriteTo.File(settings.LogFilePath, rollOnFileSizeLimit: true,
                                rollingInterval: RollingInterval.Day);
                        }

                        var serilog = serilogConfig.CreateLogger();
                        logging.AddSerilog(serilog);
                    });
                })
                .Build()
                .Run();
        }


    }
}
