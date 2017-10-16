namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Routing;
    using VDS.RDF.Query;
    using UP = Microsoft.OData.UriParser;

    public class EntitysetKeyController : BaseController
    {
        [HttpGet]
        [EnableQuery]
        public IHttpActionResult Default()
        {
            var path = this.Request.Properties["System.Web.OData.Path"] as ODataPath;
            var edmType = path.EdmType.AsElementType() as EdmEntityType;
            var keySegment = path.Segments[1] as UP.KeySegment;
            var key = keySegment.Keys.Single().Value as string;

            var queryString = EntitysetKeyController.BuildQueryString(edmType, key);
            var instances = BaseController.Deserialize(queryString);
            var entityType = BaseController.GetType(edmType);
            var typedInstances = BaseController.Cast(entityType, instances);
            var singleInstance = BaseController.GetSingleResult(entityType, typedInstances);

            return this.Ok(singleInstance);
        }

        private static string BuildQueryString(EdmEntityType type, string key)
        {
            var entityClassUri = BaseController.GetUri(type);
            var properties = BaseController.GetProperties(type);

            var queryString = EntitysetKeyController.BuildQueryTemplate(properties);
            var query = new SparqlParameterizedString(queryString);

            query.SetUri("instanceUri", new Uri($"http://id.ukpds.org/{key}"));
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

            queryBuilder.Append(@"CONSTRUCT { @instanceUri a @entityClassUri ; ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"@{property.Key} ?{property.Key} ; ");
            }

            queryBuilder.Append(@". } WHERE { @instanceUri a @entityClassUri . ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"OPTIONAL {{ @instanceUri @{property.Key} ?{property.Key} . }} ");
            }

            queryBuilder.Append(@"}");

            return queryBuilder.ToString();
        }
    }
}
