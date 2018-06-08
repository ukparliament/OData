namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using Parliament.Model;
    using Parliament.Rdf;
    using Parliament.Rdf.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Http.Results;
    using System.Web.OData;
    using System.Web.OData.Query;
    using VDS.RDF;
    using VDS.RDF.Query.Builder;
    using VDS.RDF.Storage;

    public class BaseController : ODataController
    {
        protected static Type GetType(IEdmType type)
        {
            var mappingAssembly = typeof(Person).Assembly; // TODO: ???
            return mappingAssembly.GetType(type.FullTypeName());
        }

        protected static Type GetClass(Type type)
        {
            var mappingAssembly = typeof(Person).Assembly; // TODO: ???
            var t = mappingAssembly.GetType(type.FullName);
            if (!t.IsInterface)
                return t;
            else
            {
                var name = $"{type.Namespace}.{type.Name.Substring(1)}";
                return mappingAssembly.GetType(name);
            }
        }

        private static bool IsTypeEnumerable(Type type)
        {
            return (type != typeof(string)) && ((type.IsArray) ||
                ((type.GetInterfaces().Any()) && (type.GetInterfaces().Any(i => i == typeof(IEnumerable)))));
        }

        protected static ODataQueryOptions GetQueryOptions(HttpRequestMessage request)
        {
            System.Web.OData.Routing.ODataPath path = request.Properties["System.Web.OData.Path"] as System.Web.OData.Routing.ODataPath;
            EdmEntityType edmType = null;
            foreach (var seg in path.Segments.Reverse())
            {
                edmType = seg.EdmType.AsElementType() as EdmEntityType;
                if (edmType != null)
                    break;
            }
            Type entityType = GetType(edmType);
            ODataQueryContext context = new ODataQueryContext(Global.edmModel, entityType, path);
            return new ODataQueryOptions(context, request);
        }

        public static object Execute(ODataQueryOptions options)
        {
            Uri NamespaceUri = new Uri(ConfigurationManager.AppSettings["IdNamespace"]);
            string queryString = new SparqlBuilder(options, NamespaceUri).BuildSparql();
            IGraph graph = null;
            using (var connector = new SparqlConnector(new GraphDBSparqlEndpoint()))
            {
                graph = connector.Query(queryString) as IGraph;
            }

            RdfSerializer serializer = new RdfSerializer();
            IEnumerable<IResource> ontologyInstances = serializer.Deserialize(graph, typeof(Person).Assembly, NamespaceUri);

            return ontologyInstances;
        }

        protected static object GenerateODataResult(HttpRequestMessage request)
        {
            ODataQueryOptions options = GetQueryOptions(request);

            IEnumerable<IResource> results = Execute(options) as IEnumerable<IResource>;

            bool returnList = true;
            var lastSeg = options.Context.Path.Segments.Last() as NavigationPropertySegment;
            if (lastSeg != null && lastSeg.NavigationProperty.TargetMultiplicity() != EdmMultiplicity.Many)
                returnList = false;

            if (results.Count() > 0)
            {
                var type = results.First().GetType();

                if (returnList)
                {
                    MethodInfo castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type);
                    return castMethod.Invoke(results.Where(x => x.GetType() == type), new object[] { results.Where(x => x.GetType() == type) });
                }
                else
                {
                    return results.First(x => x.GetType() == type);
                }
            }

            /*Format options*/
            if (options.RawValues.Format != null)
            {
                string format = options.RawValues.Format.ToLower(); //atom, xml, json
            }

            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(options.Context.ElementClrType));
        }

        protected IHttpActionResult Ok(object content)
        {
            var resultType = typeof(OkNegotiatedContentResult<>).MakeGenericType(content.GetType());
            return Activator.CreateInstance(resultType, content, this) as IHttpActionResult;
        }
    }
}
