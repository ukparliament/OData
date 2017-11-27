namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Code;
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.OData.Extensions;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;
    using System.Linq;
    using System.Web.OData.Builder;
    using System.Collections.Generic;

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

                var prop = declareType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
                {
                    TargetMultiplicity = removedNavProp.Multiplicity,
                    Target = (EdmEntityType)edmModel.FindDeclaredType($"{clrType.Namespace}.{clrType.Name.Substring(1)}"),
                    ContainsTarget = removedNavProp.ContainsTarget,
                    OnDelete = EdmOnDeleteAction.None,
                    Name = removedNavProp.Name,

                });
            }

            builder.ValidateModel(edmModel);

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

//var assembly = typeof(IPerson).Assembly;
//var classes = assembly.GetTypes().Where(x => !x.IsInterface);
//            foreach (var @class in classes)
//            {
//                var entityType = builder.GetTypeConfigurationOrNull(@class) as EntityTypeConfiguration;
//var properties = @class.GetProperties();
////    var entityType = builder.GetTypeConfigurationOrNull(@class) as EntityTypeConfiguration;
//List<string> navProps = new List<string>();
//                foreach (var prop in properties)
//                {
//                    if (prop.PropertyType.IsInterface)
//                        navProps.Add(prop.Name);
//                }
//                //string[] structProps = @class.GetProperties().Select(p => p.Name).Where(p => !navProps.Contains(p)).ToArray();
//                //    entityType.QueryConfiguration.SetSelect(structProps, System.Web.OData.Query.SelectExpandType.Allowed);
//                //    entityType.QueryConfiguration.SetFilter(structProps, true);
//                entityType.QueryConfiguration.SetExpand(navProps, 0, System.Web.OData.Query.SelectExpandType.Allowed);
//                //    entityType.QueryConfiguration.SetCount(true);
//                    entityType.QueryConfiguration.SetMaxTop(100);
//                    entityType.QueryConfiguration.SetPageSize(100);
//                //    builder.AddEntitySet(@class.Name, entityType);
//                var y = @class.Name;
//                if (y == "Member")
//                { }
//            }