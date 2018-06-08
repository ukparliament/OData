namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using Parliament.Model;
    using Parliament.Rdf;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        public Builder()
        {
            var iOntologyInstance = this.AddEntityType(typeof(IResource));
            iOntologyInstance.HasKey(typeof(IResource).GetProperty(nameof(IResource.LocalId)));
            iOntologyInstance.Abstract();
            iOntologyInstance.RemoveProperty(typeof(IResource).GetProperty(nameof(IResource.Id)));
            iOntologyInstance.RemoveProperty(typeof(IResource).GetProperty(nameof(IResource.BaseUri)));

            var assembly = typeof(Person).Assembly;
            this.Namespace = assembly.GetName().Name;

            foreach (var type in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(type);
                entityType.DerivesFrom(new EntityTypeConfiguration(this, typeof(IResource)));
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
