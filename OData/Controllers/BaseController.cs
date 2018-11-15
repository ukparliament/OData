namespace OData
{
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    //using Microsoft.IdentityModel.Protocols;
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    //using Microsoft.OData.UriParser;
    using Parliament.Model;
    using Parliament.Rdf.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    //using System.Net.Http;
    using System.Reflection;
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

        protected static ODataQueryOptions GetQueryOptions(HttpRequest request)
        {
            Microsoft.AspNet.OData.Routing.ODataPath path = request.ODataFeature().Path;
            //Microsoft.AspNet.OData.Routing.ODataPath path = request.Path.Properties["Microsoft.AspNet.OData.Path"] as Microsoft.AspNet.OData.Routing.ODataPath;
            EdmEntityType edmType = null;
            foreach (var seg in path.Segments.Reverse())
            {
                edmType = seg.EdmType.AsElementType() as EdmEntityType;
                if (edmType != null)
                    break;
            }
            Type entityType = GetType(edmType);
            ODataQueryContext context = new ODataQueryContext(Startup.edmModel, entityType, path);
            return new ODataQueryOptions(context, request);
        }

        public static object Execute(ODataQueryOptions options)
        {
            string sparqlEndpoint = "https://api.parliament.uk/sparql";
                //ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;
            Uri NamespaceUri = new Uri("https://id.parliament.uk/");
                //new Uri(ConfigurationManager.AppSettings["IdNamespace"]);
            string queryString = new SparqlBuilder(options, NamespaceUri).BuildSparql();
            IGraph graph = null;
            //string baseUri = "http://localhost:7201";
            //string storeId = "TestNew";
            //using (var connector = new SesameHttpProtocolVersion6Connector(baseUri, storeId))
            using (var connector = new SparqlConnector(new Uri(sparqlEndpoint)))
            //using (var connector = new SparqlConnector(new GraphDBSparqlEndpoint()))
            {
                graph = connector.Query(queryString) as IGraph;
            }

            RdfSerializer serializer = new RdfSerializer();
            IEnumerable<BaseResource> ontologyInstances = serializer.Deserialize(graph, typeof(Person).Assembly, NamespaceUri);

            return ontologyInstances;
        }

        protected static object GenerateODataResult(HttpRequest request)
        {
            ODataQueryOptions options = GetQueryOptions(request);
            if (options.SelectExpand != null)
                request.ODataFeature().SelectExpandClause = options.SelectExpand.SelectExpandClause;

            IEnumerable<BaseResource> results = Execute(options) as IEnumerable<BaseResource>;

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
                    var res = castMethod.Invoke(results.Where(x => x.GetType() == type), new object[] { results.Where(x => x.GetType() == type) });
                    return res;
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

        //public override OkObjectResult Ok(object content)
        //{
        //    //return base.Ok(content);
        //    var resultType = typeof(OkNegotiatedContentResult<>).MakeGenericType(content.GetType());
        //    return Activator.CreateInstance(resultType, content, this) as IActionResult;
        //}
    }
}
