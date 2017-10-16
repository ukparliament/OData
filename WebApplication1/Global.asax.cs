namespace WebApplication1
{
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;

    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            var config = GlobalConfiguration.Configuration;

            var builder = new Builder(); // custom
            var model = builder.GetEdmModel();
            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
                new MetadataRoutingConvention(), // built-in
                new DefaultRoutingConvention() // custom
            };

            config.MapODataServiceRoute("ODataRoute", null, model, handler, conventions);
        }
    }
}