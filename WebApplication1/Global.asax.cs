namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;

    public class Global : HttpApplication
    {
        public static IEdmModel edmModel;
        protected void Application_Start(object sender, EventArgs e)
        {
            var config = GlobalConfiguration.Configuration;

            var builder = new Builder(); // custom
            edmModel = builder.GetEdmModel();

            foreach (var removedNavProp in builder.RemovedNavigationProperties)
            {
                var clrType = removedNavProp.RelatedClrType;

                var declareType = (EdmEntityType)edmModel.FindDeclaredType(removedNavProp.DeclaringType.FullName);

                declareType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
                {
                    TargetMultiplicity = removedNavProp.Multiplicity,
                    Target = (EdmEntityType)edmModel.FindDeclaredType($"{clrType.Namespace}.{clrType.Name.Substring(1)}"),
                    ContainsTarget = removedNavProp.ContainsTarget,
                    OnDelete = EdmOnDeleteAction.None,
                    Name = removedNavProp.Name,

                });
            }

            var handler = new DefaultODataPathHandler(); // built-in
            var conventions = new IODataRoutingConvention[] {
                new MetadataRoutingConvention(), // built-in
                new DefaultRoutingConvention() // custom
            };

            config.MapODataServiceRoute("ODataRoute", null, edmModel, handler, conventions);

            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings
                .ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            GlobalConfiguration.Configuration.Formatters
                .Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
        }
    }
}