namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using Parliament.Ontology.Serializer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Http.Results;
    using System.Web.OData;
    using System.Web.OData.Builder;
    using System.Web.OData.Query;
    using System.Web.OData.Routing;
    using VDS.RDF;
    using VDS.RDF.Parsing;
    using VDS.RDF.Query.Builder;
    using VDS.RDF.Query.Filters;
    using VDS.RDF.Query.Builder.Expressions;
    using VDS.RDF.Query.Expressions;
    using VDS.RDF.Query.Expressions.Functions.Arq;
    using VDS.RDF.Query.Patterns;
    using VDS.RDF.Storage;
    using VDS.RDF.Query.Expressions.Functions.Sparql.Boolean;
    using VDS.RDF.Query.Expressions.Functions.Sparql.DateTime;
    using VDS.RDF.Query.Expressions.Functions.Sparql;
    using VDS.RDF.Query.Expressions.Functions.Sparql.Constructor;
    using VDS.RDF.Query.Expressions.Functions.Sparql.Numeric;
    using VDS.RDF.Query.Expressions.Functions.Sparql.Set;
    using VDS.RDF.Query.Expressions.Functions.Sparql.String;
    using VDS.RDF.Query.Expressions.Comparison;
    using VDS.RDF.Query.Expressions.Primary;
    using VDS.RDF.Query.Expressions.Conditional;
    using VDS.RDF.Query.Expressions.Arithmetic;
    using VDS.RDF.Query.Algebra;

    public class BaseController : ODataController
    {
        protected static Type GetType(IEdmType type)
        {
            var mappingAssembly = typeof(IPerson).Assembly; // TODO: ???
            return mappingAssembly.GetType(type.FullTypeName());
        }

        protected static ODataQueryOptions GetQueryOptions(HttpRequestMessage request, System.Web.OData.Routing.ODataPath odataPath)
        {
            System.Web.OData.Routing.ODataPath path = request.Properties["System.Web.OData.Path"] as System.Web.OData.Routing.ODataPath;
            var edmType = path.Segments.Last().EdmType.AsElementType() as EdmEntityType;
            Type entityType = GetType(edmType);
            ODataQueryContext context = new ODataQueryContext(Global.edmModel, entityType, path);
            return new ODataQueryOptions(context, request);
        }

        public static object Execute(ODataQueryOptions options, System.Web.OData.Routing.ODataPath odataPath)
        {
            string sparqlEndpoint = ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;
            //string queryString = new SparqlBuilder(options, odataPath).BuildSparql();
            string queryString = new SparqlBuilder(options, odataPath).BuildSparqlNew();
            IGraph graph = null;
            using (var connector = new SparqlConnector(new Uri(sparqlEndpoint)))
            {
                graph = connector.Query(queryString) as IGraph;
            }

            Serializer serializer = new Serializer();
            IEnumerable<IOntologyInstance> ontologyInstances = serializer.Deserialize(graph, typeof(IPerson).Assembly);

            return ontologyInstances;
        }

        private static bool IsTypeEnumerable(Type type)
        {
            return (type != typeof(string)) && ((type.IsArray) ||
                ((type.GetInterfaces().Any()) && (type.GetInterfaces().Any(i => i == typeof(IEnumerable)))));
        }

        protected static Type GetClass(Type type)
        {
            var mappingAssembly = typeof(IPerson).Assembly; // TODO: ???
            var t = mappingAssembly.GetType(type.FullName);
            if (!t.IsInterface)
                return t;
            else
            {
                var name = $"{type.Namespace}.{type.Name.Substring(1)}";
                return mappingAssembly.GetType(name);
            }
        }

        private static void RemoveIDPrefix(IEnumerable<IOntologyInstance> results)
        {
            foreach (var result in results)
            {
                result.Id = result.Id.Split('/').Last();
                foreach (var prop in result.GetType().GetProperties().Where(p => IsTypeEnumerable(p.PropertyType)))
                {
                    var propValue = prop.GetValue(result);
                    var instanceType = prop.PropertyType.GetGenericArguments()[0];
                    if (propValue != null && instanceType.GetInterface("IOntologyInstance") != null)
                    {
                        var newValue = ((IEnumerable<IOntologyInstance>)propValue).ToList();
                        newValue.ForEach(instance => instance.Id = instance.Id.Split('/').Last());

                        var cls = GetClass(instanceType);
                        var castMethodValues = typeof(Enumerable).GetMethod("Cast")
                            .MakeGenericMethod(cls);
                        prop.SetValue(result, castMethodValues.Invoke(newValue, new object[] { newValue }));
                    }
                }
            }
        }

        protected static object GenerateODataResult(ODataQueryOptions options, System.Web.OData.Routing.ODataPath odataPath)
        {
            IEnumerable<IOntologyInstance> results = Execute(options, odataPath) as IEnumerable<IOntologyInstance>;

            RemoveIDPrefix(results);

            if (results.Count() > 0)
            {
                var type = results.First().GetType();

                MethodInfo castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type);

                return castMethod.Invoke(results.Where(x=>x.GetType() == type), new object[] { results.Where(x => x.GetType() == type) });
            }
            return results;
        }

        protected IHttpActionResult Ok(object content)
        {
            var resultType = typeof(OkNegotiatedContentResult<>).MakeGenericType(content.GetType());
            return Activator.CreateInstance(resultType, content, this) as IHttpActionResult;
        }
    }
}
