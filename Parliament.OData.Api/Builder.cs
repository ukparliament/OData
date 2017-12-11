namespace Parliament.OData.Api
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
            foreach (var @interface in interfaces)
                this.AddEntityType(@interface);

            foreach (var @class in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(@class);
                entityType.HasKey(@class.GetProperty("Id"));
                this.AddEntitySet(@class.Name, entityType);
             }

            this.OnModelCreating = builder =>
            {
                foreach (var @interface in interfaces)
                    builder.RemoveStructuralType(@interface);

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
                    TargetMultiplicity = EdmMultiplicity.Many,  //navProp.Multiplicity,
                    Target = targetType,
                    ContainsTarget = navProp.ContainsTarget,
                    OnDelete = navProp.OnDeleteAction,
                    Name = navProp.Name
                });

                var declaredSet = (EdmEntitySet)edmModel.EntityContainer.FindEntitySet(declareType.Name);
                var targetSet = (EdmEntitySet)edmModel.EntityContainer.FindEntitySet(targetType.Name);
                declaredSet.AddNavigationTarget(edmNavProp, targetSet);
            }
            this.ValidateModel(edmModel);
            return edmModel;
        }
    }
}
