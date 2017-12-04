namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Code;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        private List<NavigationPropertyConfiguration> RemovedNavigationProperties = 
            new List<NavigationPropertyConfiguration>();
        public Builder()
        {
            var assembly = typeof(IPerson).Assembly;
            this.Namespace = assembly.GetName().Name;
            var interfaces = assembly.GetTypes().Where(x => x.IsInterface);

            //var container = new EdmEntityContainer(assembly.GetName().Name, "Default");
            //this.ContainerName = assembly.GetName().Name;

            foreach (var @interface in interfaces)
                this.AddEntityType(@interface);

            foreach (var @class in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(@class);
                entityType.HasKey(@class.GetProperty("Id"));
                //var properties = @class.GetProperties();
                //var navProps = @class.GetProperties().Where(cls => cls.PropertyType.IsInterface)
                //    .Select(cls1 => cls1.Name);

                //var structProps = properties.Select(p => p.Name).Where(p => !navProps.Contains(p));
                //entityType.QueryConfiguration.SetSelect(structProps, System.Web.OData.Query.SelectExpandType.Allowed);
                //entityType.QueryConfiguration.SetFilter(structProps, true);
                //entityType.QueryConfiguration.SetExpand(navProps, 1, System.Web.OData.Query.SelectExpandType.Allowed);
                //entityType.QueryConfiguration.SetCount(true);
                //entityType.QueryConfiguration.SetMaxTop(100);
                //entityType.QueryConfiguration.SetPageSize(100);
                var conf = this.AddEntitySet(@class.Name, entityType);
             }

            this.OnModelCreating = builder =>
            {
                foreach (var @interface in interfaces)
                {
                    builder.RemoveStructuralType(@interface);
                }
                foreach (var item in builder.StructuralTypes.Where(x => !x.ClrType.IsInterface))
                {
                    foreach (var prop in item.NavigationProperties.Where(x => x.RelatedClrType.IsInterface).ToArray())
                    {
                        RemovedNavigationProperties.Add(prop);
                        item.RemoveProperty(prop.PropertyInfo);
                    }
                }
            };
        }

        public override IEdmModel GetEdmModel()
        {
            IEdmModel edmModel = base.GetEdmModel();

            foreach (var navProp in RemovedNavigationProperties)
            {
                var clrType = navProp.RelatedClrType;
                var declareType = (EdmEntityType)edmModel.FindDeclaredType(navProp.DeclaringType.FullName);
                var targetType = (IEdmEntityType)edmModel.FindDeclaredType($"{clrType.Namespace}.{clrType.Name.Substring(1)}");

                var edmNavProp = declareType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
                {
                    TargetMultiplicity = EdmMultiplicity.Many, //= navProp.Multiplicity,
                    Target = targetType,
                    ContainsTarget = navProp.ContainsTarget,
                    OnDelete = navProp.OnDeleteAction,
                    Name = navProp.Name
                    
                });

                var cars = (EdmEntitySet)edmModel.EntityContainer.FindEntitySet(declareType.Name);
                var parts = (EdmEntitySet)edmModel.EntityContainer.FindEntitySet(targetType.Name);
                cars.AddNavigationTarget(edmNavProp, parts);
            }
            this.ValidateModel(edmModel);
            return edmModel;
        }
    }
}
