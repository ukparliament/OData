namespace Parliament.OData.Api
{
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using System.Web.OData.Routing;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System;
    using System.Collections.Generic;
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
                ISparqlExpression exp = new NowFunction();
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

        private ISparqlExpression BuildSparqlFilter(SingleValueNode filterNode)
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
                {
                    var varName = (convert.Source as SingleValuePropertyAccessNode).Property.Name;
                    if (varName.ToLower() == "id")
                    {
                        var node = LiteralExtensions.ToLiteral(NamespaceUri.ToString().Length + 1, nodeFactory);
                        varName = EdmNodeList[EdmNodeList.Count - 1].Name;
                        return (new UnaryExpressionFilter(
                            new SubStrFunction(new StrFunction(new VariableTerm($"?{varName}")), 
                            (new ConstantTerm(node))))).Expression;
                    }
                    else
                        return new VariableTerm($"?{varName}");
                }
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
                var classAttribute = GetInterface(structuredType).GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;
                return classAttribute.Uri;
        }

        protected static Uri GetUri(IEdmEntityType type)
        {
            var classAttribute = GetInterface(type).GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;

            return classAttribute.Uri;
        }

        protected static Type GetType(IEdmType type)
        {
            var mappingAssembly = typeof(Person).Assembly; // TODO: ???
            return mappingAssembly.GetType(type.FullTypeName());
        }

        protected static Type GetInterface(IEdmType type)
        {
            Type clr_type = GetType(type);
            if (clr_type.IsInterface)
                return clr_type;
            else
                return typeof(Person).Assembly.GetType($"{clr_type.Namespace}.I{clr_type.Name}");
        }

        protected static Uri GetPropertyUri(IEdmProperty structuralProperty)
        {
            var clr_type = GetType(structuralProperty.DeclaringType);
            Type [] interfaces = null;
            if (clr_type.IsInterface)
                interfaces = new Type [] { clr_type };
            else
                interfaces = clr_type.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                var property = @interface.GetProperty(structuralProperty.Name);
                if (property != null)
                {
                    var propertyAttribute = property.GetCustomAttributes(typeof(PropertyAttribute), false).Single() as PropertyAttribute;
                    return propertyAttribute.Uri;
                }
            }
            return null;
        }

        private class EdmNode
        {
            public string Name { get; set; }
            public EdmEntityType ItemEdmType { get; set; }
            public PatternItem RdfNode { get; set; }
            public List<IEdmStructuralProperty> StructProperties { get; set; }
            public List<IEdmNavigationProperty> NavProperties { get; set; }
            public string IdKey { get; set; }
        }

        private static Uri NamespaceUri { get; set; }
        private ODataQueryOptions QueryOptions { get; set; }
        private ISparqlExpression FilterExp { get; set; }
        private List<ITriplePattern> SubQueryTriplePatterns { get; set; }

        private List<EdmNode> EdmNodeList { get; set; }

        public SparqlBuilder(ODataQueryOptions queryOptions, Uri namespaceUri)
        {
            NamespaceUri = namespaceUri;
            QueryOptions = queryOptions;
            SubQueryTriplePatterns = new List<ITriplePattern>();
            EdmNodeList = new List<EdmNode>();
        }

        private void ProcessOdataPath()
        {
            foreach (var seg in QueryOptions.Context.Path.Segments)
            {
                if (seg is EntitySetSegment)
                {
                    var setSeg = seg as EntitySetSegment;
                    var entityType = setSeg.EdmType.AsElementType() as EdmEntityType;
                    var rdfNode = new VariablePattern($"{setSeg.EntitySet.Name}");
                    EdmNodeList.Add(new EdmNode()
                    {
                        Name = setSeg.EntitySet.Name,
                        ItemEdmType = entityType,
                        RdfNode = rdfNode,
                        StructProperties = entityType.StructuralProperties().ToList(),
                        NavProperties = new List<IEdmNavigationProperty>()
                    });
                }
                else if (seg is KeySegment)
                {
                    var keys = (seg as KeySegment).Keys.ToList();
                    Uri keyUri = null;
                    var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                    if (keys.Count() > 0)
                    {
                        edmNode.IdKey = keys[0].Value.ToString();
                        if (keys[0].Value.ToString().StartsWith(NamespaceUri.AbsoluteUri))
                            keyUri = new Uri(keys[0].Value.ToString());
                        else
                            keyUri = new Uri(NamespaceUri, keys[0].Value.ToString());
                    }
                    NodeFactory nodeFactory = new NodeFactory();
                    edmNode.RdfNode = new NodeMatchPattern(nodeFactory.CreateUriNode(keyUri));
                }

                else if (seg is NavigationPropertySegment || seg is NavigationPropertyLinkSegment)
                {
                    var propSeg = seg as NavigationPropertySegment;
                    var propSegLink = seg as NavigationPropertyLinkSegment;
                    EdmEntityType entityType;
                    IEdmNavigationProperty navProp;
                    if (propSeg != null)
                    {
                        entityType = propSeg.EdmType.AsElementType() as EdmEntityType;
                        navProp = propSeg.NavigationProperty;
                    }
                    else
                    {
                        entityType = propSegLink.EdmType.AsElementType() as EdmEntityType;
                        navProp = propSegLink.NavigationProperty;
                    }
                    var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                    edmNode.NavProperties.Add(navProp);

                    EdmNodeList.Add(new EdmNode()
                    {
                        Name = navProp.Name,
                        ItemEdmType = entityType,
                        RdfNode = new VariablePattern($"{navProp.Name}"),
                        StructProperties = entityType.StructuralProperties().ToList(),
                        NavProperties = new List<IEdmNavigationProperty>()
                    });
                }
                else if (seg is PropertySegment)
                {
                    var propSeg = seg as PropertySegment;
                    var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                    edmNode.StructProperties.Clear();
                    edmNode.StructProperties.Add(propSeg.Property);

                }
                else if (seg is CountSegment)
                {

                }
                else if (seg is ValueSegment)
                {

                }
                else if (seg is UnresolvedPathSegment)
                {
                    throw new Exception($"{(seg as UnresolvedPathSegment).SegmentKind}: {(seg as UnresolvedPathSegment).SegmentValue}");
                }
            }
        }

        private void ProcessSelectExpand()
        {
            if (QueryOptions.SelectExpand != null && QueryOptions.SelectExpand.SelectExpandClause != null)
            {
                List<IEdmStructuralProperty> strucPropList = new List<IEdmStructuralProperty>();
                List<IEdmNavigationProperty> navPropList = new List<IEdmNavigationProperty>();
                foreach (var item in QueryOptions.SelectExpand.SelectExpandClause.SelectedItems)
                {
                    var selectItem = item as PathSelectItem;
                    if (selectItem != null && selectItem.SelectedPath != null)
                    {
                        var segment = selectItem.SelectedPath.FirstSegment as PropertySegment;
                        if (segment != null)
                            strucPropList.Add(segment.Property);
                    }
                    var expandItem = item as ExpandedNavigationSelectItem;
                    if (expandItem != null)
                    {
                        var segment = expandItem.PathToNavigationProperty.FirstSegment as NavigationPropertySegment;
                        if (segment != null)
                            navPropList.Add(segment.NavigationProperty);
                    }
                }
                var edmNode = EdmNodeList.Last();
                if (navPropList.Count > 0)
                    edmNode.NavProperties = navPropList;
                if (strucPropList.Count > 0)
                    edmNode.StructProperties = strucPropList;
            }
        }

        private string ConstructSparql()
        {
            NodeFactory nodeFactory = new NodeFactory();
            List<ITriplePattern> constructList = new List<ITriplePattern>();
            List<ITriplePattern> whereList = new List<ITriplePattern>();
            List<ITriplePattern> optionList = new List<ITriplePattern>();
            List<ITriplePattern> optSubQueryTriplePatterns = new List<ITriplePattern>();
            
            EdmNode previousEdmNode = null;
            EdmNode endEdmNode = EdmNodeList.Last();
            foreach (var edmNode in EdmNodeList)
            {
                var isaTriple = new TriplePattern(edmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(edmNode.ItemEdmType))));
                if (edmNode == endEdmNode)
                    constructList.Add(isaTriple);
                whereList.Add(isaTriple);

                if (previousEdmNode != null)
                {
                    Dictionary<string, Tuple<Type, Uri>> preProperties = GetAllProperties(previousEdmNode.ItemEdmType);
                    var relationTriple = new TriplePattern(previousEdmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(preProperties[edmNode.Name].Item2.AbsoluteUri))),
                        edmNode.RdfNode);
                    if (edmNode == endEdmNode)
                        constructList.Add(relationTriple);
                    whereList.Add(relationTriple);
                }
                previousEdmNode = edmNode;

                if (edmNode == endEdmNode)
                {
                    Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(edmNode.ItemEdmType);
                    foreach (var prop in edmNode.StructProperties.Where(p => p.Name != "Id"))
                    {
                        var propTriple = new TriplePattern(edmNode.RdfNode,
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"{prop.Name}"));
                        constructList.Add(propTriple);
                        optionList.Add(propTriple);
                    }

                    if (edmNode.NavProperties.Count > 0)
                    {
                        foreach (var expProp in edmNode.NavProperties)
                        {
                            var expEntityType = expProp.Type.Definition.AsElementType() as EdmEntityType;
                            List<ITriplePattern> tripleList = new List<ITriplePattern>();

                            tripleList.Add(new TriplePattern(new VariablePattern($"?{expProp.Name}"),
                                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                                    new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(expEntityType)))));

                            tripleList.Add(new TriplePattern(edmNode.RdfNode,
                                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[expProp.Name].Item2.AbsoluteUri))),
                                    new VariablePattern($"?{expProp.Name}")));
                            constructList.AddRange(tripleList);

                            List<ITriplePattern> predTripleList = new List<ITriplePattern>();
                            Dictionary<string, Tuple<Type, Uri>> expandProperties = GetAllProperties(expEntityType);

                            foreach (var prop in expEntityType.StructuralProperties().Where(p => p.Name != "Id"))
                            {
                                var expPropTriple = new TriplePattern(new VariablePattern($"?{expProp.Name}"),
                                    new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(expandProperties[prop.Name].Item2.AbsoluteUri))),
                                    new VariablePattern($"?{prop.Name}Expand"));
                                predTripleList.Add(expPropTriple);
                                constructList.Add(expPropTriple);
                            }

                            IQueryBuilder subqueryBuilder = null;
                            var variableList = expEntityType.StructuralProperties().Where(p => p.Name != "Id")
                                .Select(p => $"?{p.Name}Expand").ToList();
                            variableList.Add(expProp.Name);
                            if (edmNode.RdfNode is VariablePattern)
                                variableList.Add(edmNode.Name);
                            subqueryBuilder = QueryBuilder.Select(variableList.ToArray()).Where(tripleList.ToArray());
                            foreach (var tp in predTripleList)
                                subqueryBuilder.Optional(gp => gp.Where(tp));
                            optSubQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
                        }
                    }
                }
            }

            IQueryBuilder queryBuilder = QueryBuilder.Construct(q => q.Where(constructList.ToArray()))
                .Where(whereList.Concat(SubQueryTriplePatterns).ToArray());

            foreach (var tp in optionList)
                queryBuilder.Optional(gp => gp.Where(tp));
            foreach (var tp in optSubQueryTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            if (FilterExp != null)
                queryBuilder.Filter(FilterExp);

            /*OrderBy options*/
            if (QueryOptions.OrderBy != null && QueryOptions.OrderBy.OrderByClause != null)
            {
                var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                foreach (var node in QueryOptions.OrderBy.OrderByNodes)
                {
                    var typedNode = node as OrderByPropertyNode;
                    var ordName = typedNode.Property.Name;
                    if (ordName.ToLower() == "id")
                        ordName = edmNode.Name;
                    if (typedNode.OrderByClause.Direction == OrderByDirection.Ascending)
                        queryBuilder.OrderBy(ordName);
                    else
                        queryBuilder.OrderByDescending(ordName);
                }
            }

            return queryBuilder.BuildQuery().ToString(); 
        }

        private void ProcessSkipTop()
        {
            if (QueryOptions.Skip != null || QueryOptions.Top != null)
            {
                NodeFactory nodeFactory = new NodeFactory();
                var edmNode = EdmNodeList[EdmNodeList.Count - 1];

                IQueryBuilder subqueryBuilder = QueryBuilder.Select(new string[] { edmNode.Name }).
                    Where(new ITriplePattern[] { new TriplePattern(edmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(edmNode.ItemEdmType))))});

                if (QueryOptions.Skip != null)
                    subqueryBuilder = subqueryBuilder.Offset(QueryOptions.Skip.Value);
                if (QueryOptions.Top != null)
                    subqueryBuilder = subqueryBuilder.Limit(QueryOptions.Top.Value);
                if (FilterExp != null)
                {
                    subqueryBuilder = subqueryBuilder.Filter(FilterExp);
                    FilterExp = null;
                }
                SubQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
            }
        }

        public string BuildSparql()
        {
            /*Convert odata path segments to an EDM node list*/
            ProcessOdataPath();

            /*Select and expand options*/
            ProcessSelectExpand();

            /*Filter options*/
            if (QueryOptions.Filter != null && QueryOptions.Filter.FilterClause != null)
                FilterExp = BuildSparqlFilter(QueryOptions.Filter.FilterClause.Expression);

            /*Skip and top options*/
            ProcessSkipTop();

            return ConstructSparql();
        }
    }
}