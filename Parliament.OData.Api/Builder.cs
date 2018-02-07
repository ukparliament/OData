﻿namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System.Linq;
    using System.Web.OData.Builder;

    public class Builder : ODataConventionModelBuilder
    {
        public Builder()
        {
            var iOntologyInstance = this.AddEntityType(typeof(IOntologyInstance));
            iOntologyInstance.HasKey(typeof(IOntologyInstance).GetProperty(nameof(IOntologyInstance.Id)));
            iOntologyInstance.Abstract();

            var assembly = typeof(Person).Assembly;
            this.Namespace = assembly.GetName().Name;

            foreach (var type in assembly.GetTypes().Where(x => !x.IsInterface))
            {
                var entityType = this.AddEntityType(type);
                entityType.DerivesFrom(new EntityTypeConfiguration(this, typeof(IOntologyInstance)));
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
