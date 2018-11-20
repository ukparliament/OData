namespace OData
{
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Rewrite;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData.Edm;
    using Swashbuckle.AspNetCore.SwaggerUI;
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
            //services.Configure<RouteOptions>(Startup.ConfigureRouteOptions);
            services.AddOData();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseRewriter(new RewriteOptions().AddRewrite("^$", "swagger/index.html", false).AddRewrite("^(swagger|favicon)(.+)$", "swagger/$1$2", true));
            app.UseRewriter(new RewriteOptions().AddRewrite(@"^odata201710131103.azurewebsites.net", "api.parliament.uk/odata", true));
            //app.UseSwaggerUI(Startup.ConfigureSwaggerUI);

            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
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

            //foreach (var mapping in Configuration.OpenApiMappings)
            //{
            //    mvc.OutputFormatters.Insert(0, new OpenApiFormatter(mapping.MediaType, mapping.WriterType));
            //    mvc.FormatterMappings.SetMediaTypeMappingForFormat(mapping.Extension, mapping.MediaType);
            //    mvc.FormatterMappings.SetMediaTypeMappingForFormat(mapping.MediaType, mapping.MediaType);
            //}
        }

        //private static void ConfigureRouteOptions(RouteOptions routes)
        //{
        //    routes.ConstraintMap.Add("openapi", typeof(OpenApiExtensionConstraint));
        //}

        //private static void ConfigureSwaggerUI(SwaggerUIOptions swaggerUI)
        //{
        //    swaggerUI.DocumentTitle = "UK Parliament OData API Service";
        //    swaggerUI.SwaggerEndpoint("./openapi", "live");
        //}
    }
}
