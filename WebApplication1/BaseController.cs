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
    using VDS.RDF.Query.Patterns;
    using VDS.RDF.Storage;

    public class BaseController : ODataController
    {
        protected static Uri NamespaceUri
        {
            get
            {
                var namespaceBase = ConfigurationManager.AppSettings["NamespaceBase"];
                return new Uri(namespaceBase);
            }
        }

        protected static IEnumerable<IOntologyInstance> Deserialize(string queryString)
        {

            using (var result = Query(queryString))
            {
                var instances = new Serializer().Deserialize(result, typeof(IPerson).Assembly);

                foreach (var item in instances)
                {
                    item.Id = BaseController.NamespaceUri.MakeRelativeUri(new Uri(item.Id)).ToString();
                }

                return instances;
            }
        }

        protected static Uri GetUri(IEdmEntityType type)
        {
            var interfaceType = EntitysetController.GetInterface(type);
            var interfaceClassAttribute = interfaceType.GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;

            return interfaceClassAttribute.Uri;
        }

        protected static Type GetInterface(IEdmType type)
        {
            var mappingAssembly = typeof(IPerson).Assembly; // TODO: ???
            var t = mappingAssembly.GetType(type.FullTypeName());
            if (t.IsInterface)
                return t;
            else
                return t.GetInterface($"I{((EdmEntityType)type).Name}");
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

        protected static ODataQueryOptions GetQueryOptions(HttpRequestMessage request)
        {
            System.Web.OData.Routing.ODataPath path = request.Properties["System.Web.OData.Path"] as System.Web.OData.Routing.ODataPath;
            var edmType = path.EdmType.AsElementType() as EdmEntityType;
            Type entityType = BaseController.GetType(edmType);
            ODataModelBuilder modelBuilder = new Builder();
            ODataQueryContext context = new ODataQueryContext(modelBuilder.GetEdmModel(), entityType, path);
            return new ODataQueryOptions(context, request);
        }

        private static string BuildSparql(ODataQueryOptions options)
        {
            var edmEntityType = options.Context.Path.Segments[0].EdmType.AsElementType() as EdmEntityType;
            string idKey = null;
            string navProp = null;
            EdmEntityType navPropType = null;
            if (options.Context.Path.Segments.Count > 1)
            {
                var keys = (options.Context.Path.Segments[1] as KeySegment).Keys.ToList();
                if (keys.Count() > 0)
                {
                    idKey = keys[0].Value.ToString();
                }
            }
            if (options.Context.Path.Segments.Count > 2)
            {
                navProp = (options.Context.Path.Segments[2] as NavigationPropertySegment).NavigationProperty.Name;
                navPropType = (options.Context.Path.Segments[2] as NavigationPropertySegment).EdmType.AsElementType() as EdmEntityType;
            }

            NodeFactory nodeFactory = new NodeFactory();
            //int i = 0;
            PatternItem root = new VariablePattern("Id"); //new VariablePattern($"?s{i}");
            if (idKey != null)
                root = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(NamespaceUri, idKey)));
            //Type entityType = BaseController.GetType(edmEntityType);
            Dictionary <string, Tuple<Type, Uri>> properties = GetAllProperties(edmEntityType);

            List<ITriplePattern> classTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> predicateTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> filterTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> navTriplePatterns = new List<ITriplePattern>();

            classTriplePatterns.Add(new TriplePattern(root,
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties["Id"].Item2.AbsoluteUri)))));

            if (options.SelectExpand != null && options.SelectExpand.SelectExpandClause != null)
            {
                foreach (var item in options.SelectExpand.SelectExpandClause.SelectedItems)
                {
                    var selectItem = item as PathSelectItem;
                    if (selectItem != null && selectItem.SelectedPath != null)
                    {
                        var segment = selectItem.SelectedPath.FirstSegment as PropertySegment;
                        if (segment != null)
                        {
                            var propName = segment.Property.Name;
                            if (propName == "Id")
                                continue;
                            //foreach (string predicate in node.Item2)
                            predicateTriplePatterns.Add(new TriplePattern(root,
                                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[propName].Item2.AbsoluteUri))),
                                new VariablePattern($"?{propName}")));
                        }
                    }
                }
                //i++;
            }
            else
            {
                var structProperties = edmEntityType.StructuralProperties();
                foreach (var propName in structProperties)
                {
                    if (propName.Name != "Id")
                    {
                        var propertyUri = BaseController.GetPropertyUri(propName);
                        predicateTriplePatterns.Add(new TriplePattern(root,
                                    new NodeMatchPattern(nodeFactory.CreateUriNode(propertyUri)),
                                    new VariablePattern($"?{propName.Name}")));
                    }
                }
            }

            if (options.Filter != null && options.Filter.FilterClause != null)
            {
                var binaryOperator = options.Filter.FilterClause.Expression as BinaryOperatorNode;
                if (binaryOperator != null)
                {
                    var property = binaryOperator.Left as SingleValuePropertyAccessNode ?? binaryOperator.Right as SingleValuePropertyAccessNode;
                    var constant = binaryOperator.Left as ConstantNode ?? binaryOperator.Right as ConstantNode;

                    if (property != null && property.Property != null && constant != null && constant.Value != null)
                    {
                        filterTriplePatterns.Add(new TriplePattern(root,
                                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[property.Property.Name].Item2.AbsoluteUri))),
                                new NodeMatchPattern(nodeFactory.CreateLiteralNode(constant.LiteralText.Replace("'", "")))));
                    }
                }
            }

            if (navProp != null)
            {
                var navNode = new VariablePattern($"?{navProp}");
                var navPropUri = BaseController.GetUri(navPropType);
                predicateTriplePatterns.Add(new TriplePattern(root,
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[navProp].Item2.AbsoluteUri))),
                    navNode));

                navTriplePatterns.Add(new TriplePattern(navNode,
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                new NodeMatchPattern(nodeFactory.CreateUriNode(navPropUri))));
            }

            List<ITriplePattern> subQueryTriplePatterns = new List<ITriplePattern>();
            if (options.Top != null)
            {
                IQueryBuilder subqueryBuilder = null;
                if (navProp != null)
                    subqueryBuilder = QueryBuilder.Select(new string[] { $"?{navProp}" }).Where(navTriplePatterns.ToArray()).Limit(options.Top.Value);
                else
                    subqueryBuilder = QueryBuilder.Select(new string[] { "Id" }).Where(classTriplePatterns.ToArray()).Limit(options.Top.Value);

                subQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
            }

            if (options.Skip != null)
            {
                IQueryBuilder subqueryBuilder = null;
                if (navProp != null)
                    subqueryBuilder = QueryBuilder.Select(new string[] { $"?{navProp}" }).Where(navTriplePatterns.ToArray()).Offset(options.Skip.Value);
                else
                    subqueryBuilder = QueryBuilder.Select(new string[] { "Id" }).Where(classTriplePatterns.ToArray()).Offset(options.Skip.Value);

                subQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
            }

            IQueryBuilder queryBuilder = null;
            if (navProp == null)
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(classTriplePatterns.Concat(predicateTriplePatterns).ToArray()))
                    .Where(classTriplePatterns.Concat(filterTriplePatterns).Concat(subQueryTriplePatterns).ToArray());
            else
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(navTriplePatterns.Concat(predicateTriplePatterns).ToArray()))
                    .Where(classTriplePatterns.Concat(filterTriplePatterns).Concat(navTriplePatterns).Concat(subQueryTriplePatterns).ToArray());

            foreach (TriplePattern tp in predicateTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            if (options.OrderBy != null && options.OrderBy.OrderByClause != null)
            {
                foreach (var node in options.OrderBy.OrderByNodes)
                {
                    var typedNode = node as OrderByPropertyNode;
                    if (typedNode.OrderByClause.Direction == OrderByDirection.Ascending)
                        queryBuilder.OrderBy(typedNode.Property.Name);
                    else
                        queryBuilder.OrderByDescending(typedNode.Property.Name);
                }
            }

            return queryBuilder.BuildQuery().ToString();
        }

        public static object Execute(ODataQueryOptions options) //Expression expression)
        {
            string sparqlEndpoint = ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;
            string queryString = BuildSparql(options); // expression);
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

        protected static object GenerateODataResult(ODataQueryOptions options)//Expression expression)
        {
            //DbQueryProvider dbQueryProvider = new DbQueryProvider();
            IEnumerable<IOntologyInstance> result = Execute(options) as IEnumerable<IOntologyInstance>;
            if (result.Count() > 0)
            {
                MethodInfo castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(result.First().GetType());

                return castMethod.Invoke(result, new object[] { result });
            }
            return result;
        }

        private static Type ConvertPrimitiveEdmTypeToType(IEdmPrimitiveType edmType, bool isNullable)
        {
            switch (edmType.PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return isNullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
                case EdmPrimitiveTypeKind.Decimal:
                    return isNullable ? typeof(decimal?) : typeof(decimal);
                case EdmPrimitiveTypeKind.Double:
                    return isNullable ? typeof(double?) : typeof(double);
                case EdmPrimitiveTypeKind.Int16:
                    return isNullable ? typeof(Int16?) : typeof(Int16);
                case EdmPrimitiveTypeKind.Int32:
                    return isNullable ? typeof(Int32?) : typeof(Int32);
                case EdmPrimitiveTypeKind.Int64:
                    return isNullable ? typeof(Int64?) : typeof(Int64);
                case EdmPrimitiveTypeKind.String:
                    return typeof(string);
                default:
                    return null;
            }
        }

        protected static Dictionary<string, Tuple<Type, Uri>> GetAllProperties(IEdmStructuredType type)
        {
            return type.Properties()
                .ToDictionary(p => p.Name, p =>
                    Tuple.Create<Type, Uri>(
                        p.Type.Definition.TypeKind == EdmTypeKind.Primitive ? ConvertPrimitiveEdmTypeToType(p.Type.Definition as IEdmPrimitiveType, p.Type.IsNullable) : typeof(object),
                        p.Name == "Id" ? GetClassUri(type) : GetPropertyUri(p)
                    )
                );
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

        private static Uri GetClassUri(IEdmStructuredType structuredType)
        {
            var declaringType = BaseController.GetInterface(structuredType);

            if (declaringType != null)
            {
                ClassAttribute classAttribute = declaringType.GetCustomAttributes(typeof(ClassAttribute), false).SingleOrDefault() as ClassAttribute;
                return classAttribute.Uri;
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
