namespace Parliament.OData.Api
{
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.OData.Edm;
    using Parliament.Model;
    using Parliament.Rdf.Serialization;
    using System.Linq;

    public class Builder : ODataConventionModelBuilder
    {
        public Builder()
        {
            var iOntologyInstance = this.AddEntityType(typeof(BaseResource));
            iOntologyInstance.HasKey(typeof(BaseResource).GetProperty(nameof(BaseResource.LocalId)));
            iOntologyInstance.Abstract();
            iOntologyInstance.RemoveProperty(typeof(BaseResource).GetProperty(nameof(BaseResource.Id)));
            iOntologyInstance.RemoveProperty(typeof(BaseResource).GetProperty(nameof(BaseResource.BaseUri)));

            var assembly = typeof(Person).Assembly;
            this.Namespace = assembly.GetName().Name;

            foreach (var type in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(type);
                entityType.DerivesFrom(new EntityTypeConfiguration(this, typeof(BaseResource)));
                this.AddEntitySet(type.Name, entityType);
            }
        }

        public override IEdmModel GetEdmModel()
        {
            IEdmModel edmModel = base.GetEdmModel();
            this.ValidateModel(edmModel);
            return edmModel;
        }
    }
}
