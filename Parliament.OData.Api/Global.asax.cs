namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;
    using System.Configuration;
    using System.Web.Http.ExceptionHandling;

    public class Global : HttpApplication
    {
        public static IEdmModel edmModel;
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
            config.Services.Add(typeof(IExceptionLogger), new AIExceptionLogger());
            Uri externalApiUri = new Uri(ConfigurationManager.AppSettings["ExternalAPIAddress"]);
            string routePrefix = externalApiUri.AbsolutePath;
            if (routePrefix.StartsWith("/"))
                routePrefix = routePrefix.Substring(1);
            if (routePrefix.EndsWith("/"))
                routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
            config.MapODataServiceRoute("ODataRoute", routePrefix, edmModel, handler, conventions);
            config.Select().Expand().Filter().OrderBy().Count().MaxTop(null);
            //config.Formatters.JsonFormatter.SerializerSettings
            //    .ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            //config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
