namespace OData
{
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using System.Linq;

    public class Startup
    {
        public static IEdmModel edmModel;
        private static IEdmModel GetEdmModel()
        {
            var builder = new Builder(); // custom
            edmModel = builder.GetEdmModel();
            return edmModel;
        }

        public Startup(IConfiguration configuration)
        {
            Program.Configuration = configuration.Get<Configuration>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(Startup.SetupMvc);
            services.AddOData();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
                new DefaultMetadataRoutingConvention(),
                new DefaultRoutingConvention() // custom
            };

            app.UseMvc(routeBuilder =>
            {
                routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(100).Count().EnableContinueOnErrorHeader();
                routeBuilder.MapRoute("OpenApiDefinition", "openapi.json", new { controller = "OpenApi" });
                routeBuilder.MapODataServiceRoute("odata", null, GetEdmModel(), handler, conventions);
            });
        }

        private static void SetupMvc(MvcOptions mvc)
        {
            mvc.RespectBrowserAcceptHeader = true;
            mvc.ReturnHttpNotAcceptable = true;
        }
    }
}
