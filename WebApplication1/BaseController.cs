namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using Parliament.Ontology.Serializer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.Http;
    using System.Web.Http.Results;
    using System.Web.OData;
    using VDS.RDF;
    using VDS.RDF.Storage;

    public class BaseController : ODataController
    {
        protected static IEnumerable<IOntologyInstance> Deserialize(string queryString)
        {
            var x = new Uri("http://id.ukpds.org/");

            using (var result = Query(queryString))
            {
                var instances = new Serializer().Deserialize(result, typeof(IPerson).Assembly);

                foreach (var item in instances)
                {
                    item.Id = x.MakeRelativeUri(new Uri(item.Id)).ToString();
                }

                return instances;
            }
        }

        protected static Uri GetUri(IEdmEntityType type)
        {
            var interfaceType = EntitysetController.GetType(type);
            var interfaceClassAttribute = interfaceType.GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;

            return interfaceClassAttribute.Uri;
        }

        protected static Type GetType(IEdmType type)
        {
            var mappingAssembly = typeof(IPerson).Assembly; // TODO: ???
            return mappingAssembly.GetType(type.FullTypeName());
        }

        protected static Dictionary<string, Uri> GetProperties(IEdmStructuredType type)
        {
            var mapping = new Dictionary<string, Uri>();

            foreach (var structuralProperty in type.StructuralProperties())
            {
                var propertyUri = BaseController.GetPropertyUri(structuralProperty);

                if (propertyUri != null)
                {
                    mapping.Add(structuralProperty.Name, propertyUri);
                }
            }

            return mapping;
        }

        protected static Uri GetPropertyUri(IEdmProperty structuralProperty)
        {
            var declaringType = BaseController.GetType(structuralProperty.DeclaringType);

            if (declaringType != null)
            {
                var property = declaringType.GetProperty(structuralProperty.Name);
                var propertyAttribute = property.GetCustomAttributes(typeof(PropertyAttribute), false).Single() as PropertyAttribute;

                return propertyAttribute.Uri;
            }

            return null;
        }

        protected static IEnumerable Cast(Type type, params object[] parameters)
        {
            var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
            var genericCast = castMethod.MakeGenericMethod(type);

            return genericCast.Invoke(null, parameters) as IEnumerable;
        }

        protected static SingleResult GetSingleResult(Type entityType, IEnumerable instances)
        {
            var createMethod = typeof(SingleResult).GetMethod(nameof(SingleResult.Create)).MakeGenericMethod(entityType);
            return createMethod.Invoke(null, new[] { instances.AsQueryable() }) as SingleResult;
        }

        protected IHttpActionResult Ok(object content)
        {
            var resultType = typeof(OkNegotiatedContentResult<>).MakeGenericType(content.GetType());
            return Activator.CreateInstance(resultType, content, this) as IHttpActionResult;
        }

        private static IGraph Query(string queryString)
        {
            var endpointUri = ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;

            using (var connector = new SparqlConnector(new Uri(endpointUri)))
            {
                return connector.Query(queryString) as IGraph;
            }
        }
    }
}
