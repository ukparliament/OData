// MIT License
//
// Copyright (c) 2019 UK Parliament
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace OData
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
