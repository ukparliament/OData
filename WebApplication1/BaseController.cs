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
            var model = modelBuilder.GetEdmModel();
            ODataQueryContext context = new ODataQueryContext(model, entityType, path);
            return new ODataQueryOptions(context, request);
        }

        private static ILiteralNode CreateLiteralNode(ConstantNode node)
        {
            NodeFactory nodeFactory = new NodeFactory();
            switch ((node.TypeReference.Definition as IEdmPrimitiveType).PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return LiteralExtensions.ToLiteralDate((node.Value as DateTimeOffset?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Int32:
                    return LiteralExtensions.ToLiteral((node.Value as Int32?).GetValueOrDefault(), nodeFactory);
                default:
                    return nodeFactory.CreateLiteralNode(node.LiteralText.Replace("'", ""));
            }
        }

        private static ISparqlExpression BuildSparqlFilter(SingleValueNode filterNode)
        {
            NodeFactory nodeFactory = new NodeFactory();

            if (filterNode.GetType() == typeof(SingleValueFunctionCallNode))
            {
                var functionOperator = filterNode as SingleValueFunctionCallNode;
                ISparqlExpression exp = null;
                var funcName = functionOperator.Name;
                string[] funcNames = { "contains", "endswith", "startswith", "length", "tolower", "toupper",
                    "substring", "replace", "concat", "indexof", "trim", "year", "day", "month", "hour", "minute",
                    "second", "now" };

                if (!funcNames.Contains(funcName))
                    throw new NotImplementedException($"Function {funcName} not implemented.");

                if (funcName == "now") // NOW() in sparql does not work, will generate this now in .net instead.
                {
                    exp = new VDS.RDF.Query.Expressions.Functions.Sparql.DateTime.NowFunction();
                    return (new UnaryExpressionFilter(exp)).Expression;
                }
                var property = functionOperator.Parameters.ElementAt(0) as SingleValuePropertyAccessNode;
                var func = functionOperator.Parameters.ElementAt(0) as SingleValueFunctionCallNode;
                if ((property != null && property.Property != null) || func != null)
                {
                    ISparqlExpression valueTerm = null;
                    if (property != null && property.Property != null)
                        valueTerm = new VariableTerm($"?{property.Property.Name}");
                    else
                        valueTerm = BuildSparqlFilter(func);

                    if (funcName == "length")
                        exp = new StrLenFunction(valueTerm);
                    else if (funcName == "tolower")
                        exp = new LCaseFunction(valueTerm);
                    else if (funcName == "toupper")
                        exp = new UCaseFunction(valueTerm);
                    else if (funcName == "year")
                        exp = new YearFunction(valueTerm);
                    else if (funcName == "day")
                        exp = new DayFunction(valueTerm);
                    else if (funcName == "month")
                        exp = new MonthFunction(valueTerm);
                    else if (funcName == "hour")
                        exp = new HoursFunction(valueTerm);
                    else if (funcName == "minute")
                        exp = new MinutesFunction(valueTerm);
                    else if (funcName == "second")
                        exp = new SecondsFunction(valueTerm);
                    else
                    {
                        var constant = functionOperator.Parameters.ElementAt(1) as ConstantNode;
                        ConstantTerm constantTerm = new ConstantTerm(CreateLiteralNode(constant));
                        if (funcName == "contains")
                            exp = new ContainsFunction(valueTerm, constantTerm);
                        else if (funcName == "endswith")
                            exp = new StrEndsFunction(valueTerm, constantTerm);
                        else if (funcName == "startswith")
                            exp = new StrStartsFunction(valueTerm, constantTerm);
                        else if (funcName == "indexof")
                            throw new NotImplementedException("Sparql does not have counterpart of indexof function.");
                        else if (funcName == "trim")
                            throw new NotImplementedException("Sparql needs regex to do trim.");
                        else if (funcName == "concat")
                            exp = new ConcatFunction(new List<ISparqlExpression> { valueTerm, constantTerm });
                        else if (funcName == "substring")
                        {
                            if (functionOperator.Parameters.Count() > 2)
                            {
                                var constant2 = functionOperator.Parameters.ElementAt(2) as ConstantNode;
                                ConstantTerm constantTerm2 = new ConstantTerm(CreateLiteralNode(constant2));
                                exp = new SubStrFunction(valueTerm, constantTerm, constantTerm2);
                            }
                            else
                                exp = new SubStrFunction(valueTerm, constantTerm);
                        }
                        else if (funcName == "replace")
                        {
                            var constant2 = functionOperator.Parameters.ElementAt(2) as ConstantNode;
                            ConstantTerm constantTerm2 = new ConstantTerm(CreateLiteralNode(constant2));
                            exp = new ReplaceFunction(valueTerm, constantTerm, constantTerm2);
                        }
                    }
                }
                if (exp != null)
                    return (new UnaryExpressionFilter(exp)).Expression;
                return null;
            }

            else if (filterNode.GetType() == typeof(BinaryOperatorNode))
            {
                var binaryOperator = filterNode as BinaryOperatorNode;
                if (binaryOperator.OperatorKind == BinaryOperatorKind.And)
                {
                    return new AndExpression(BuildSparqlFilter(binaryOperator.Left), BuildSparqlFilter(binaryOperator.Right));
                }
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Or)
                {
                    return new OrExpression(BuildSparqlFilter(binaryOperator.Left), BuildSparqlFilter(binaryOperator.Right));
                }

                var property = binaryOperator.Left as SingleValuePropertyAccessNode ?? binaryOperator.Right as SingleValuePropertyAccessNode;
                var constant = binaryOperator.Left as ConstantNode ?? binaryOperator.Right as ConstantNode;
                var convert = binaryOperator.Left as ConvertNode ?? binaryOperator.Right as ConvertNode;
                var function = binaryOperator.Left as SingleValueFunctionCallNode;

                if ((property != null && property.Property != null) || function != null)
                {
                    ISparqlExpression exp = null;
                    ISparqlExpression valueTerm;
                    ISparqlExpression constantTerm = null;
                    ILiteralNode literalNode = null;

                    if (convert != null)
                    {
                        if (convert.Source is SingleValueFunctionCallNode)
                            constantTerm = BuildSparqlFilter(convert.Source as SingleValueFunctionCallNode);
                        else if (convert.Source is ConstantNode)
                            literalNode = CreateLiteralNode(convert.Source as ConstantNode);
                    }
                    else if (constant != null && constant.Value != null)
                        literalNode = CreateLiteralNode(constant);

                    if ((literalNode != null) || (constantTerm != null))
                    {
                        if (property != null && property.Property != null)
                            valueTerm = new VariableTerm($"?{property.Property.Name}");
                        else
                            valueTerm = BuildSparqlFilter(function);
                        if (constantTerm == null)
                            constantTerm = new ConstantTerm(literalNode);
                        if (binaryOperator.OperatorKind == BinaryOperatorKind.Equal)
                            exp = new EqualsExpression(valueTerm, constantTerm);
                        else if (binaryOperator.OperatorKind == BinaryOperatorKind.NotEqual)
                            exp = new NotEqualsExpression(valueTerm, constantTerm);
                        else if (binaryOperator.OperatorKind == BinaryOperatorKind.GreaterThan)
                            exp = new GreaterThanExpression(valueTerm, constantTerm);
                        else if (binaryOperator.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual)
                            exp = new GreaterThanOrEqualToExpression(valueTerm, constantTerm);
                        else if (binaryOperator.OperatorKind == BinaryOperatorKind.LessThan)
                            exp = new LessThanExpression(valueTerm, constantTerm);
                        else if (binaryOperator.OperatorKind == BinaryOperatorKind.LessThanOrEqual)
                            exp = new LessThanOrEqualToExpression(valueTerm, constantTerm);

                        return (new UnaryExpressionFilter(exp)).Expression;
                    }
                }
            }
            else if (filterNode.GetType() == typeof(UnaryOperatorNode))
            {
                var unaryOperator = filterNode as UnaryOperatorNode;
                if (unaryOperator.OperatorKind == UnaryOperatorKind.Not)
                {
                    return new NotEqualsExpression(BuildSparqlFilter(unaryOperator.Operand), 
                        new ConstantTerm(LiteralExtensions.ToLiteral(true, nodeFactory)));
                }
            }
            return null;
        }

        private static string BuildSparql(ODataQueryOptions options)
        {
            var entityType = options.Context.Path.Segments[0].EdmType.AsElementType() as EdmEntityType;
            string entityId = null;
            string navProp = null;
            EdmEntityType navEntityType = null;
            Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(entityType);
            Dictionary<string, Tuple<Type, Uri>> navProperties = null;
            NodeFactory nodeFactory = new NodeFactory();
            PatternItem root = new VariablePattern("Id");
            List<ITriplePattern> classTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> predicateTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> navTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> classNavTriplePatterns = new List<ITriplePattern>();

            if (options.Context.Path.Segments.Count > 1)
            {
                var keys = (options.Context.Path.Segments[1] as KeySegment).Keys.ToList();
                if (keys.Count() > 0)
                {
                    entityId = keys[0].Value.ToString();
                    root = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(NamespaceUri, entityId)));
                }
            }

            classTriplePatterns.Add(new TriplePattern(root,
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties["Id"].Item2.AbsoluteUri)))));

            if (options.Context.Path.Segments.Count > 2)
            {
                navProp = (options.Context.Path.Segments[2] as NavigationPropertySegment).NavigationProperty.Name;
                navEntityType = (options.Context.Path.Segments[2] as NavigationPropertySegment).EdmType.AsElementType() as EdmEntityType;
                navProperties = GetAllProperties(navEntityType);

                classNavTriplePatterns.Add(new TriplePattern(root,
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[navProp].Item2.AbsoluteUri))),
                    new VariablePattern($"?{navProp}")));

                navTriplePatterns.Add(new TriplePattern(new VariablePattern($"?{navProp}"),
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                    new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(navEntityType)))));
            }

            /*Select options*/
            List<IEdmStructuralProperty> propList = new List<IEdmStructuralProperty>();
            if (options.SelectExpand != null && options.SelectExpand.SelectExpandClause != null)
            {
                foreach (var item in options.SelectExpand.SelectExpandClause.SelectedItems)
                {
                    var selectItem = item as PathSelectItem;
                    if (selectItem != null && selectItem.SelectedPath != null)
                    {
                        var segment = selectItem.SelectedPath.FirstSegment as PropertySegment;
                        if (segment != null)
                            propList.Add (segment.Property);
                    }
                }
            }
            else
            {
                if (navProp != null)
                    propList = navEntityType.StructuralProperties().ToList();
                else
                    propList = entityType.StructuralProperties().ToList();
            }

            foreach (var prop in propList)
            {
                if (prop.Name == "Id")
                    continue;

                if (navProp != null)
                {
                    predicateTriplePatterns.Add(new TriplePattern(new VariablePattern($"?{navProp}"),
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(navProperties[prop.Name].Item2.AbsoluteUri))),
                    new VariablePattern($"?{prop.Name}")));
                }
                else
                    predicateTriplePatterns.Add(new TriplePattern(root,
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                    new VariablePattern($"?{prop.Name}")));
            }

            /*Filter options*/
            ISparqlExpression filterExp = null;
            if (options.Filter != null && options.Filter.FilterClause != null)
                filterExp = BuildSparqlFilter(options.Filter.FilterClause.Expression);

            /*Skip and Top options*/
            List<ITriplePattern> subQueryTriplePatterns = new List<ITriplePattern>();
            if (options.Skip != null || options.Top != null)
            {
                IQueryBuilder subqueryBuilder = null;
                if (navProp != null)
                    subqueryBuilder = QueryBuilder.Select(new string[] { $"?{navProp}" }).Where(navTriplePatterns.ToArray());
                else
                    subqueryBuilder = QueryBuilder.Select(new string[] { "Id" }).Where(classTriplePatterns.ToArray());
                if (options.Skip != null)
                    subqueryBuilder = subqueryBuilder.Offset(options.Skip.Value);
                if (options.Top != null)
                    subqueryBuilder = subqueryBuilder.Limit(options.Top.Value);
                subQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
            }

            IQueryBuilder queryBuilder = null;
            if (navProp == null)
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(classTriplePatterns.Concat(predicateTriplePatterns).ToArray()));
            else
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(navTriplePatterns.Concat(predicateTriplePatterns).ToArray()));

            queryBuilder.Where(classTriplePatterns.Concat(navTriplePatterns).
                Concat(classNavTriplePatterns).Concat(subQueryTriplePatterns).ToArray());

            foreach (TriplePattern tp in predicateTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            if (filterExp != null)
                queryBuilder.Filter(filterExp);

            /*OrderBy options*/
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
