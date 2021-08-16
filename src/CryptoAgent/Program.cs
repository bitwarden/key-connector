using Microsoft.AspNetCore.Hosting;
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
                        var serilogConfig = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.File("/etc/bitwarden/logs/log.txt",
                                rollOnFileSizeLimit: true,
                                rollingInterval: RollingInterval.Day);

                        var serilog = serilogConfig.CreateLogger();
                        logging.AddSerilog(serilog);
                    });
                })
                .Build()
                .Run();
        }


    }
}
