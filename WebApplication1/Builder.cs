namespace WebApplication1
{
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        public Builder()
        {
            this.OnModelCreating = b =>
            {
                foreach (var item in b.StructuralTypes.Where(x => !x.ClrType.IsInterface))
                {
                    foreach (var p in item.Properties.ToArray())
                    {
                        item.RemoveProperty(p.PropertyInfo);
                    }
                }
            };

            var iOntologyInstance = this.AddEntityType(typeof(IOntologyInstance));
            iOntologyInstance.HasKey(typeof(IOntologyInstance).GetProperty(nameof(IOntologyInstance.Id)));
            iOntologyInstance.Abstract();

            var assembly = typeof(IPerson).Assembly;

            var interfaces = assembly.GetTypes().Where(x => x.IsInterface);
            foreach (var @interface in interfaces)
            {
                var entityType = this.AddEntityType(@interface);
                entityType.Abstract();

                var superInterfaces = @interface.GetInterfaces().AsEnumerable();
                superInterfaces = superInterfaces.Except(superInterfaces.SelectMany(x => x.GetInterfaces()));

                foreach (var superInterface in superInterfaces)
                {
                    entityType.DerivesFrom(new EntityTypeConfiguration(this, superInterface));
                }
            }

            var classes = assembly.GetTypes().Where(x => !x.IsInterface);
            foreach (var @class in classes)
            {
                var entityType = this.AddEntityType(@class);

                var superInterfaces = @class.GetInterfaces().AsEnumerable();
                superInterfaces = superInterfaces.Except(superInterfaces.SelectMany(x => x.GetInterfaces()));

                foreach (var superInterface in superInterfaces)
                {
                    entityType.DerivesFrom(new EntityTypeConfiguration(this, superInterface));
                }
                
                string[] propertyNames = @class.GetProperties().Select(p => p.Name).ToArray();
                entityType.QueryConfiguration.SetSelect(propertyNames, System.Web.OData.Query.SelectExpandType.Allowed);
                entityType.QueryConfiguration.SetFilter(propertyNames, true);
                this.AddEntitySet(@class.Name, entityType);
            }
        }
    }
}