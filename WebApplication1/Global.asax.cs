namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;
    using System.Linq;

    public class Global : HttpApplication
    {
        public static IEdmModel edmModel;
        protected void Application_Start(object sender, EventArgs e)
        {
            var builder = new Builder(); // custom
            edmModel = builder.GetEdmModel();

            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
                new MetadataRoutingConvention(), // built-in
                new DefaultRoutingConvention() // custom
            };

            var config = GlobalConfiguration.Configuration;
            config.MapODataServiceRoute("ODataRoute", null, edmModel, handler, conventions);
            config.Select().Expand().Filter().OrderBy().MaxTop(100).Count();
            config.Formatters.JsonFormatter.SerializerSettings
                .ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
