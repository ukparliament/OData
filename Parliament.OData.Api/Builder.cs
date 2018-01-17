namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Code;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        public Builder()
        {
            var assembly = typeof(Person).Assembly;
            this.Namespace = assembly.GetName().Name;

            foreach (var @class in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(@class);
                entityType.HasKey(@class.GetProperty("Id"));
                this.AddEntitySet(@class.Name, entityType);
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
