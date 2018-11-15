namespace OData
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    internal static class Program
    {
        internal static Configuration Configuration { get; set; }

        public static void Main(string[] args)
        {
            Program.CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
        }
    }
}
