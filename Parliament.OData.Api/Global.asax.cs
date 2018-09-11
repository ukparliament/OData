namespace Parliament.OData.Api
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.OData.Edm;
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Web;
    using System.Web.Http;

    public class Global : HttpApplication
    {
        public static IEdmModel edmModel;
        public static string ODataRouteName = "ODataRouteNew";

        protected void Application_Start(object sender, EventArgs e)
        {
            TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["ApplicationInsightsInstrumentationKey"];
            var builder = new Builder(); // custom
            edmModel = builder.GetEdmModel();

            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
                new DefaultMetadataRoutingConvention(), // custom
                new DefaultRoutingConvention() // custom
            };

            var config = GlobalConfiguration.Configuration;
            config.Routes.MapHttpRoute("OpenApiDefinition", "openapi.json", new { controller = "OpenApiDefinition" });
            config.MapODataServiceRoute(ODataRouteName, null, edmModel, handler, conventions);
            config.Select().Expand().Filter().OrderBy().Count().MaxTop(null);

            config.Formatters.Add(new ODataSparqlRequestFormatter());

            //config.Formatters.JsonFormatter.SerializerSettings
            //    .ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            //config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
