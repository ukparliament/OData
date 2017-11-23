namespace WebApplication1
{
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        public List<NavigationPropertyConfiguration> RemovedNavigationProperties { get; set; }
        public Builder()
        {
            var assembly = typeof(IPerson).Assembly;

            var interfaces = assembly.GetTypes().Where(x => x.IsInterface);
            foreach (var @interface in interfaces)
                this.AddEntityType(@interface);

            var classes = assembly.GetTypes().Where(x => !x.IsInterface);
            foreach (var @class in classes)
            {
                var entityType = this.AddEntityType(@class);
                entityType.HasKey(@class.GetProperty("Id"));
                string[] propertyNames = @class.GetProperties().Select(p => p.Name).ToArray();
                entityType.QueryConfiguration.SetSelect(propertyNames, System.Web.OData.Query.SelectExpandType.Allowed);
                entityType.QueryConfiguration.SetFilter(propertyNames, true);
                entityType.QueryConfiguration.SetExpand(propertyNames, 2, System.Web.OData.Query.SelectExpandType.Allowed);
                entityType.QueryConfiguration.SetCount(true);
                //entityType.QueryConfiguration.SetMaxTop(100);
                //entityType.QueryConfiguration.SetPageSize(100);
                this.AddEntitySet(@class.Name, entityType);
            }

            this.OnModelCreating = builder =>
            {
                RemovedNavigationProperties = new List<NavigationPropertyConfiguration>();
                foreach (var item in builder.StructuralTypes.Where(x => !x.ClrType.IsInterface))
                {
                    foreach (var prop in item.NavigationProperties.Where(x => x.RelatedClrType.IsInterface).ToArray())
                    {
                        RemovedNavigationProperties.Add(prop);
                        item.RemoveProperty(prop.PropertyInfo);
                    }
                }
                foreach (var @interface in interfaces)
                {
                    builder.RemoveStructuralType(@interface);
                }
            };
        }
    }
}
