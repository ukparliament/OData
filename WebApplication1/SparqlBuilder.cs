namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.OData.Query;
    using VDS.RDF;
    using VDS.RDF.Parsing;
    using VDS.RDF.Query.Builder;
    using VDS.RDF.Query.Expressions;
    using VDS.RDF.Query.Expressions.Arithmetic;
    using VDS.RDF.Query.Expressions.Comparison;
    using VDS.RDF.Query.Expressions.Conditional;
    using VDS.RDF.Query.Expressions.Functions.Sparql.DateTime;
    using VDS.RDF.Query.Expressions.Functions.Sparql.Numeric;
    using VDS.RDF.Query.Expressions.Functions.Sparql.String;
    using VDS.RDF.Query.Expressions.Primary;
    using VDS.RDF.Query.Filters;
    using VDS.RDF.Query.Patterns;

    public class SparqlBuilder
    {
        private static ILiteralNode CreateLiteralNode(ConstantNode node)
        {
            NodeFactory nodeFactory = new NodeFactory();
            //if (node.LiteralText == "null")
            //    return LiteralNode LiteralExtensions.ToLiteral("null", nodeFactory);
            switch ((node.TypeReference.Definition as IEdmPrimitiveType).PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return LiteralExtensions.ToLiteralDate((node.Value as DateTimeOffset?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Date:
                    return LiteralExtensions.ToLiteralDate((node.Value as Date?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Int32:
                    return LiteralExtensions.ToLiteral((node.Value as Int32?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Double:
                    return LiteralExtensions.ToLiteral((node.Value as Double?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Decimal:
                    return LiteralExtensions.ToLiteral((node.Value as Decimal?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Single:
                    return LiteralExtensions.ToLiteral((node.Value as Single?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Int16:
                    return LiteralExtensions.ToLiteral((node.Value as Int16?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Int64:
                    return LiteralExtensions.ToLiteral((node.Value as Int64?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.Boolean:
                    return LiteralExtensions.ToLiteral((node.Value as Boolean?).GetValueOrDefault(), nodeFactory);
                case EdmPrimitiveTypeKind.String:
                    return nodeFactory.CreateLiteralNode(node.LiteralText.Replace("'", ""));
                default:
                    return nodeFactory.CreateLiteralNode(node.LiteralText.Replace("'", ""));
            }
        }

        private static ISparqlExpression BuildFunctionExpression(SingleValueFunctionCallNode functionOperator)
        {
            var funcName = functionOperator.Name;
            string[] funcNames = { "contains", "endswith", "startswith", "length", "tolower", "toupper",
                    "substring", "replace", "concat", "indexof", "trim", "year", "day", "month", "hour", "minute",
                    "second", "now", "ceiling", "floor", "round", };

            if (!funcNames.Contains(funcName))
                throw new NotImplementedException($"Function {funcName} not implemented.");

            if (funcName == "now") // NOW() in sparql does not work, will generate this now in .net instead.
            {
                ISparqlExpression exp = new VDS.RDF.Query.Expressions.Functions.Sparql.DateTime.NowFunction();
                return (new UnaryExpressionFilter(exp)).Expression;
            }

            var parameter = functionOperator.Parameters.ElementAt(0);
            ISparqlExpression valueTerm = null;
            if (parameter.GetType() == typeof(SingleValuePropertyAccessNode))
                valueTerm = new VariableTerm($"?{(parameter as SingleValuePropertyAccessNode).Property.Name}");
            else if (parameter.GetType() == typeof(SingleValueFunctionCallNode))
                valueTerm = BuildFunctionExpression(parameter as SingleValueFunctionCallNode);

            if (funcName == "length")
                return (new UnaryExpressionFilter(new StrLenFunction(valueTerm))).Expression;
            else if (funcName == "tolower")
                return (new UnaryExpressionFilter(new LCaseFunction(valueTerm))).Expression;
            else if (funcName == "toupper")
                return (new UnaryExpressionFilter(new UCaseFunction(valueTerm))).Expression;
            else if (funcName == "year")
                return (new UnaryExpressionFilter(new YearFunction(valueTerm))).Expression;
            else if (funcName == "day")
                return (new UnaryExpressionFilter(new DayFunction(valueTerm))).Expression;
            else if (funcName == "month")
                return (new UnaryExpressionFilter(new MonthFunction(valueTerm))).Expression;
            else if (funcName == "hour")
                return (new UnaryExpressionFilter(new HoursFunction(valueTerm))).Expression;
            else if (funcName == "minute")
                return (new UnaryExpressionFilter(new MinutesFunction(valueTerm))).Expression;
            else if (funcName == "second")
                return (new UnaryExpressionFilter(new SecondsFunction(valueTerm))).Expression;
            else if (funcName == "ceiling")
                return (new UnaryExpressionFilter(new CeilFunction(valueTerm))).Expression;
            else if (funcName == "floor")
                return (new UnaryExpressionFilter(new FloorFunction(valueTerm))).Expression;
            else if (funcName == "round")
                return (new UnaryExpressionFilter(new RoundFunction(valueTerm))).Expression;
            else
            {
                var constant = functionOperator.Parameters.ElementAt(1) as ConstantNode;
                ConstantTerm constantTerm = new ConstantTerm(CreateLiteralNode(constant));
                if (funcName == "contains")
                    return (new UnaryExpressionFilter(new ContainsFunction(valueTerm, constantTerm))).Expression;
                else if (funcName == "endswith")
                    return (new UnaryExpressionFilter(new StrEndsFunction(valueTerm, constantTerm))).Expression;
                else if (funcName == "startswith")
                    return (new UnaryExpressionFilter(new StrStartsFunction(valueTerm, constantTerm))).Expression;
                else if (funcName == "concat")
                    return (new UnaryExpressionFilter(new ConcatFunction(new List<ISparqlExpression> { valueTerm, constantTerm }))).Expression;
                else if (funcName == "indexof")
                    throw new NotImplementedException("Sparql does not have counterpart of indexof function.");
                else if (funcName == "trim")
                    throw new NotImplementedException("Sparql needs regex to do trim.");
                else if (funcName == "substring")
                {
                    if (functionOperator.Parameters.Count() > 2)
                    {
                        var constant2 = functionOperator.Parameters.ElementAt(2) as ConstantNode;
                        ConstantTerm constantTerm2 = new ConstantTerm(CreateLiteralNode(constant2));
                        return (new UnaryExpressionFilter(new SubStrFunction(valueTerm, constantTerm, constantTerm2))).Expression;
                    }
                    else
                        return (new UnaryExpressionFilter(new SubStrFunction(valueTerm, constantTerm))).Expression;
                }
                else if (funcName == "replace")
                {
                    var constant2 = functionOperator.Parameters.ElementAt(2) as ConstantNode;
                    ConstantTerm constantTerm2 = new ConstantTerm(CreateLiteralNode(constant2));
                    return (new UnaryExpressionFilter(new ReplaceFunction(valueTerm, constantTerm, constantTerm2))).Expression;
                }
            }
            return null;
        }

        private static ISparqlExpression BuildSparqlFilter(SingleValueNode filterNode)
        {
            NodeFactory nodeFactory = new NodeFactory();

            if (filterNode.GetType() == typeof(SingleValueFunctionCallNode))
                return BuildFunctionExpression(filterNode as SingleValueFunctionCallNode);
            else if (filterNode.GetType() == typeof(SingleValuePropertyAccessNode))
                return new VariableTerm($"?{(filterNode as SingleValuePropertyAccessNode).Property.Name}");
            else if (filterNode.GetType() == typeof(ConstantNode))
                return new ConstantTerm(CreateLiteralNode(filterNode as ConstantNode));
            else if (filterNode.GetType() == typeof(ConvertNode))
            {
                var convert = filterNode as ConvertNode;
                if (convert.Source is SingleValueFunctionCallNode)
                    return BuildSparqlFilter(convert.Source as SingleValueFunctionCallNode);
                else if (convert.Source is ConstantNode)
                    return new ConstantTerm(CreateLiteralNode(convert.Source as ConstantNode));
                else if (convert.Source is SingleValuePropertyAccessNode)
                    return new VariableTerm($"?{(convert.Source as SingleValuePropertyAccessNode).Property.Name}");
            }
            else if (filterNode.GetType() == typeof(BinaryOperatorNode))
            {
                var binaryOperator = filterNode as BinaryOperatorNode;
                var left = BuildSparqlFilter(binaryOperator.Left);
                var right = BuildSparqlFilter(binaryOperator.Right);

                if (binaryOperator.OperatorKind == BinaryOperatorKind.And)
                    return new AndExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Or)
                    return new OrExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Equal)
                    return new EqualsExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.NotEqual)
                    return new NotEqualsExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.GreaterThan)
                    return new GreaterThanExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.GreaterThanOrEqual)
                    return new GreaterThanOrEqualToExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.LessThan)
                    return new LessThanExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.LessThanOrEqual)
                    return new LessThanOrEqualToExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Add)
                    return new AdditionExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Subtract)
                    return new SubtractionExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Divide)
                    return new DivisionExpression(left, right);
                else if (binaryOperator.OperatorKind == BinaryOperatorKind.Multiply)
                    return new MultiplicationExpression(left, right);
            }
            else if (filterNode.GetType() == typeof(UnaryOperatorNode))
            {
                var unaryOperator = filterNode as UnaryOperatorNode;
                if (unaryOperator.OperatorKind == UnaryOperatorKind.Not)
                    return new NotEqualsExpression(BuildSparqlFilter(unaryOperator.Operand),
                        new ConstantTerm(LiteralExtensions.ToLiteral(true, nodeFactory)));
            }
            return null;
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

        private static Uri GetClassUri(IEdmStructuredType structuredType)
        {
            var declaringType = GetInterface(structuredType);

            if (declaringType != null)
            {
                ClassAttribute classAttribute = declaringType.GetCustomAttributes(typeof(ClassAttribute), false).SingleOrDefault() as ClassAttribute;
                return classAttribute.Uri;
            }

            return null;
        }

        protected static Uri NamespaceUri
        {
            get
            {
                var namespaceBase = ConfigurationManager.AppSettings["NamespaceBase"];
                return new Uri(namespaceBase);
            }
        }

        protected static Uri GetUri(IEdmEntityType type)
        {
            var interfaceType = GetInterface(type);
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

        protected static Uri GetPropertyUri(IEdmProperty structuralProperty)
        {
            var declaringType = GetType(structuralProperty.DeclaringType);
            Type[] interfaces;
            if (declaringType.IsInterface)
                interfaces = new Type[] { declaringType };
            else
                interfaces = declaringType.GetInterfaces();

            if (interfaces != null)
            {
                foreach (var inter in interfaces)
                {
                    var property = inter.GetProperty(structuralProperty.Name);
                    if (property != null)
                    {
                        var propertyAttribute = property.GetCustomAttributes(typeof(PropertyAttribute), false).Single() as PropertyAttribute;

                        return propertyAttribute.Uri;
                    }
                }
            }

            return null;
        }

        ODataQueryOptions queryOptions;
        List<ITriplePattern> classTriplePatterns;
        List<ITriplePattern> predicateTriplePatterns;
        List<ITriplePattern> navTriplePatterns;
        List<ITriplePattern> classNavTriplePatterns;
        List<ITriplePattern> classExpandTriplePatterns;
        List<IEdmStructuralProperty> selectPropList;
        List<IEdmNavigationProperty> expandPropList;
        ISparqlExpression filterExp;
        NavigationPropertySegment navProp;
        List<ITriplePattern> subQueryTriplePatterns;
        List<ITriplePattern> optSubQueryTriplePatterns;

        public SparqlBuilder(ODataQueryOptions options)
        {
            queryOptions = options;
            classTriplePatterns = new List<ITriplePattern>();
            predicateTriplePatterns = new List<ITriplePattern>();
            navTriplePatterns = new List<ITriplePattern>();
            classNavTriplePatterns = new List<ITriplePattern>();
            classExpandTriplePatterns = new List<ITriplePattern>();
            selectPropList = new List<IEdmStructuralProperty>();
            expandPropList = new List<IEdmNavigationProperty>();
            subQueryTriplePatterns = new List<ITriplePattern>();
            optSubQueryTriplePatterns = new List<ITriplePattern>();
        }

        public string BuildSparql()
        {
            var entityType = queryOptions.Context.Path.Segments[0].EdmType.AsElementType() as EdmEntityType;
            Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(entityType);
            Dictionary<string, Tuple<Type, Uri>> navProperties = null;
            NodeFactory nodeFactory = new NodeFactory();
            PatternItem root = new VariablePattern("Id");
            PatternItem navPropRoot = null;

            /* process entity key */
            if (queryOptions.Context.Path.Segments.Count > 1)
            {
                var keySegment = queryOptions.Context.Path.Segments[1] as KeySegment;
                if (keySegment != null)
                {
                    var keys = (queryOptions.Context.Path.Segments[1] as KeySegment).Keys.ToList();
                    if (keys.Count() > 0)
                    {
                        string entityId = keys[0].Value.ToString();
                        if (entityId.StartsWith(NamespaceUri.AbsoluteUri))
                            root = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(entityId)));
                        else
                            root = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(NamespaceUri, entityId)));
                    }
                }
            }

            classTriplePatterns.Add(new TriplePattern(root,
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties["Id"].Item2.AbsoluteUri)))));

            /* process navigation and structural property */
            if (queryOptions.Context.Path.Segments.Count > 2)
            {
                navProp = queryOptions.Context.Path.Segments[2] as NavigationPropertySegment;
                var prop = queryOptions.Context.Path.Segments[2] as PropertySegment;

                if (prop != null)
                    selectPropList.Add(prop.Property);
                else if (navProp != null)
                {
                    var navPropName = navProp.NavigationProperty.Name;
                    var navEntityType = navProp.EdmType.AsElementType() as EdmEntityType;
                    navProperties = GetAllProperties(navEntityType);

                    navPropRoot = new VariablePattern($"?{navPropName}");
                    if (queryOptions.Context.Path.Segments.Count > 3)
                    {
                        var navKeySegment = queryOptions.Context.Path.Segments[3] as KeySegment;
                        if (navKeySegment != null)
                        {
                            var keys = navKeySegment.Keys.ToList();
                            if (keys.Count() > 0)
                            {
                                string navEntityId = keys[0].Value.ToString();
                                if (navEntityId.StartsWith(NamespaceUri.AbsoluteUri))
                                    navPropRoot = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(navEntityId)));
                                else
                                    navPropRoot = new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(NamespaceUri, navEntityId)));
                            }
                        }
                    }

                    classNavTriplePatterns.Add(new TriplePattern(root,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[navPropName].Item2.AbsoluteUri))),
                        navPropRoot));

                    navTriplePatterns.Add(new TriplePattern(navPropRoot,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(navEntityType)))));
                }
            }

            /*Select and expand options*/
            if (queryOptions.SelectExpand != null && queryOptions.SelectExpand.SelectExpandClause != null)
            {
                foreach (var item in queryOptions.SelectExpand.SelectExpandClause.SelectedItems)
                {
                    var selectItem = item as PathSelectItem;
                    if (selectItem != null && selectItem.SelectedPath != null)
                    {
                        var segment = selectItem.SelectedPath.FirstSegment as PropertySegment;
                        if (segment != null)
                            selectPropList.Add(segment.Property);
                    }
                    var expandItem = item as ExpandedNavigationSelectItem;
                    if (expandItem != null)
                    {
                        var segment = expandItem.PathToNavigationProperty.FirstSegment as NavigationPropertySegment;
                        if (segment != null)
                            expandPropList.Add(segment.NavigationProperty);
                    }
                }
            }

            if (selectPropList.Count == 0)
            {
                if (navProp != null)
                    selectPropList = (navProp.EdmType.AsElementType() as EdmEntityType).StructuralProperties().ToList();
                else
                    selectPropList = entityType.StructuralProperties().ToList();
            }

            /*Loop through select property list*/
            foreach (var prop in selectPropList.Where(p => p.Name != "Id"))
            {
                if (navProp != null)
                    predicateTriplePatterns.Add(new TriplePattern(navPropRoot,
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(navProperties[prop.Name].Item2.AbsoluteUri))),
                    new VariablePattern($"?{prop.Name}")));
                else
                    predicateTriplePatterns.Add(new TriplePattern(root,
                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                    new VariablePattern($"?{prop.Name}")));
            }

            /*Loop through expand property list*/
            foreach (var expProp in expandPropList)
            {
                //if (navProp != null)
                //{

                //}
                //else
                //{
                var expEntityType = expProp.Type.Definition.AsElementType() as EdmEntityType;
                List<ITriplePattern> tripleList = new List<ITriplePattern>();

                tripleList.Add(new TriplePattern(new VariablePattern($"?{expProp.Name}"),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(expEntityType)))));

                tripleList.Add(new TriplePattern(root,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[expProp.Name].Item2.AbsoluteUri))),
                        new VariablePattern($"?{expProp.Name}")));
                classExpandTriplePatterns.AddRange(tripleList);

                List<ITriplePattern> predTripleList = new List<ITriplePattern>();
                Dictionary<string, Tuple<Type, Uri>> expandProperties = GetAllProperties(expEntityType);

                foreach (var prop in expEntityType.StructuralProperties().Where(p => p.Name != "Id"))
                    predTripleList.Add(new TriplePattern(new VariablePattern($"?{expProp.Name}"),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(expandProperties[prop.Name].Item2.AbsoluteUri))),
                        new VariablePattern($"?{prop.Name}Expand")));

                IQueryBuilder subqueryBuilder = null;
                subqueryBuilder = QueryBuilder.Select(new string[] { expProp.Name }).Where(tripleList.ToArray())
                    .Optional(gp => gp.Where(predTripleList.ToArray()));
                optSubQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
                //}
            }

            /*Filter options*/
            if (queryOptions.Filter != null && queryOptions.Filter.FilterClause != null)
                filterExp = BuildSparqlFilter(queryOptions.Filter.FilterClause.Expression);

            /*Skip and Top options*/
            if (queryOptions.Skip != null || queryOptions.Top != null)
            {
                IQueryBuilder subqueryBuilder = null;
                if (navProp != null)
                    subqueryBuilder = QueryBuilder.Select(new string[] { $"?{navProp.NavigationProperty.Name}" }).Where(navTriplePatterns.ToArray());
                else
                    subqueryBuilder = QueryBuilder.Select(new string[] { "Id" }).Where(classTriplePatterns.ToArray());
                if (queryOptions.Skip != null)
                    subqueryBuilder = subqueryBuilder.Offset(queryOptions.Skip.Value);
                if (queryOptions.Top != null)
                    subqueryBuilder = subqueryBuilder.Limit(queryOptions.Top.Value);
                subQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
            }

            return ConstructSparql();
        }

        private string ConstructSparql()
        {
            IQueryBuilder queryBuilder = null;
            if (navProp == null)
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(classTriplePatterns.Concat(predicateTriplePatterns).Concat(classExpandTriplePatterns).ToArray()));
            else
                queryBuilder = QueryBuilder
                    .Construct(q => q.Where(navTriplePatterns.Concat(predicateTriplePatterns).Concat(classExpandTriplePatterns).ToArray()));

            queryBuilder.Where(classTriplePatterns.Concat(navTriplePatterns).
                Concat(classNavTriplePatterns).Concat(subQueryTriplePatterns).ToArray());

            foreach (TriplePattern tp in predicateTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            foreach (var tp in optSubQueryTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            if (filterExp != null)
                queryBuilder.Filter(filterExp);

            /*OrderBy options*/
            if (queryOptions.OrderBy != null && queryOptions.OrderBy.OrderByClause != null)
            {
                foreach (var node in queryOptions.OrderBy.OrderByNodes)
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
    }
}