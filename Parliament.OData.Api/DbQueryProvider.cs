using Parliament.Ontology.Base;
using Parliament.Ontology.Code;
using Parliament.Ontology.Serializer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.OData.Query;
using VDS.RDF;
using VDS.RDF.Storage;

namespace WebApplication1
{
    public class DbQueryProvider //: IQueryProvider
    {
        private string translate(ODataQueryOptions options) //Expression expression)
        {
            return new QueryTranslator().Translate(options);// expression);
        }

        //public IQueryable CreateQuery(Expression expression)
        //{
        //    Type elementType = TypeSystem.GetElementType(expression.Type);
        //    try
        //    {
        //        return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
        //    }
        //    catch (TargetInvocationException tie)
        //    {
        //        throw tie.InnerException;
        //    }
        //}

        //public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        //{
        //    return new Query<TElement>(this, expression);
        //}

        public object Execute(ODataQueryOptions options) //Expression expression)
        {
            string sparqlEndpoint = ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;
            string queryString = translate(options); // expression);
            IGraph graph = null;
            using (var connector = new SparqlConnector(new Uri(sparqlEndpoint)))
            {
                graph = connector.Query(queryString) as IGraph;
            }

            //Type elementType = TypeSystem.GetElementType(expression.Type);
            Serializer serializer = new Serializer();
            IEnumerable<IOntologyInstance> ontologyInstances = serializer.Deserialize(graph, typeof(IPerson).Assembly);

            return ontologyInstances;
        }

        //TResult IQueryProvider.Execute<TResult>(Expression expression)
        //{
        //    return (TResult)Execute(expression);            
        //}        
    }
}