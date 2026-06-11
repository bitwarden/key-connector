using Bit.KeyConnector.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Bit.KeyConnector
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Host
                    .CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    })
                    .Build()
                    .Run();
                return 0;
            }
            catch (UnsupportedDatabaseProviderException e)
            {
                Log.Fatal(e.Message);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
