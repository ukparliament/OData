namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Routing;
    using VDS.RDF.Query;

    public class EntitysetController : BaseController
    {
        [HttpGet]
        [EnableQuery]
        public IHttpActionResult Default()
        {
            var path = this.Request.Properties["System.Web.OData.Path"] as ODataPath;
            var edmType = path.EdmType.AsElementType() as EdmEntityType;

            var queryString = EntitysetController.BuildQueryString(edmType);
            var instances = BaseController.Deserialize(queryString);
            var entityType = BaseController.GetType(edmType);
            var typedInstances = BaseController.Cast(entityType, instances);

            return this.Ok(typedInstances);
        }

        private static string BuildQueryString(EdmEntityType type)
        {
            var entityClassUri = BaseController.GetUri(type.BaseEntityType());
            var properties = BaseController.GetProperties(type);

            var queryString = BuildQueryTemplate(properties);
            var query = new SparqlParameterizedString(queryString);

            query.SetUri("entityClassUri", entityClassUri);

            foreach (var property in properties)
            {
                query.SetUri(property.Key, property.Value);
            }

            return query.ToString();
        }

        private static string BuildQueryTemplate(Dictionary<string, Uri> properties)
        {
            var queryBuilder = new StringBuilder();

            queryBuilder.Append(@"CONSTRUCT { ?s a @entityClassUri ; ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"@{property.Key} ?{property.Key} ; ");
            }

            queryBuilder.Append(@". } WHERE { ?s a @entityClassUri . ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"OPTIONAL {{ ?s @{property.Key} ?{property.Key} . }} ");
            }

            queryBuilder.Append(@"}");

            return queryBuilder.ToString();
        }
    }
}
