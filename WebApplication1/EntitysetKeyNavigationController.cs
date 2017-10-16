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

    public class EntitysetKeyNavigationController : BaseController
    {
        [HttpGet]
        [EnableQuery]
        public IHttpActionResult Default()
        {
            var path = this.Request.Properties["System.Web.OData.Path"] as ODataPath;
            var entitysetSegment = path.Segments[0] as UP.EntitySetSegment;
            var edmType = entitysetSegment.EdmType.AsElementType() as EdmEntityType;
            var keySegment = path.Segments[1] as UP.KeySegment;
            var key = keySegment.Keys.Single().Value as string;
            var navigationSegment = path.Segments[2] as UP.NavigationPropertySegment;
            var navigationType = navigationSegment.EdmType.AsElementType() as EdmEntityType;
            var navigationProperty = navigationSegment.NavigationProperty;

            var queryString = EntitysetKeyNavigationController.BuildQueryString(edmType, key, navigationType, navigationProperty);
            var instances = BaseController.Deserialize(queryString);
            var entityType = BaseController.GetType(navigationType);
            var typedInstances = BaseController.Cast(entityType, instances);

            return this.Ok(typedInstances);
        }

        private static string BuildQueryString(EdmEntityType type, string key, IEdmEntityType navigationType, IEdmNavigationProperty navigationProperty)
        {
            var entityClassUri = BaseController.GetUri(type.BaseEntityType());
            var navigationClassUri = BaseController.GetUri(navigationType);
            var navigationPropertyUri = BaseController.GetPropertyUri(navigationProperty);
            var properties = BaseController.GetProperties(type);

            var queryString = EntitysetKeyNavigationController.BuildQueryTemplate(properties);
            var query = new SparqlParameterizedString(queryString);

            query.SetUri("instanceUri", new Uri(BaseController.NamespaceUri, key));
            query.SetUri("entityClassUri", entityClassUri);
            query.SetUri("navigationClassUri", navigationClassUri);
            query.SetUri("navigationPropertyUri", navigationPropertyUri);

            foreach (var property in properties)
            {
                query.SetUri(property.Key, property.Value);
            }

            return query.ToString();
        }

        private static string BuildQueryTemplate(Dictionary<string, Uri> properties)
        {
            var queryBuilder = new StringBuilder();

            queryBuilder.Append(@"CONSTRUCT { ?s a @navigationClassUri ; ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"@{property.Key} ?{property.Key} ; ");
            }

            queryBuilder.Append(@". } WHERE { @instanceUri a @entityClassUri ; @navigationPropertyUri ?s . ?s a @navigationClassUri . ");

            foreach (var property in properties)
            {
                queryBuilder.Append($"OPTIONAL {{ ?s @{property.Key} ?{property.Key} . }} ");
            }

            queryBuilder.Append(@"}");

            return queryBuilder.ToString();
        }
    }
}
