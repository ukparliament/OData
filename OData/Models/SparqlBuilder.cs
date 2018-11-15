namespace OData
{
    using Microsoft.OData.Edm;
    using Microsoft.OData.UriParser;
    using Parliament.Model;
    using Parliament.Rdf.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using Microsoft.AspNet.OData.Query;

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

        private ISparqlExpression BuildSparqlFilter(SingleValueNode filterNode, string suffix = "")
        {
            NodeFactory nodeFactory = new NodeFactory();

            if (filterNode.GetType() == typeof(SingleValueFunctionCallNode))
                return BuildFunctionExpression(filterNode as SingleValueFunctionCallNode);
            else if (filterNode.GetType() == typeof(SingleValuePropertyAccessNode))
                return new VariableTerm($"?{(filterNode as SingleValuePropertyAccessNode).Property.Name}{suffix}");
            else if (filterNode.GetType() == typeof(ConstantNode))
                return new ConstantTerm(CreateLiteralNode(filterNode as ConstantNode));
            else if (filterNode.GetType() == typeof(ConvertNode))
            {
                var convert = filterNode as ConvertNode;
                if (convert.Source is SingleValueFunctionCallNode)
                    return BuildSparqlFilter(convert.Source as SingleValueFunctionCallNode, suffix);
                else if (convert.Source is ConstantNode)
                    return new ConstantTerm(CreateLiteralNode(convert.Source as ConstantNode));
                else if (convert.Source is SingleValuePropertyAccessNode)
                {
                    var varName = (convert.Source as SingleValuePropertyAccessNode).Property.Name;
                    if (varName.ToLower() == "localid")
                    {
                        var node = LiteralExtensions.ToLiteral(NamespaceUri.ToString().Length + 1, nodeFactory);
                        varName = EdmNodeList[EdmNodeList.Count - 1].Name;
                        return (new UnaryExpressionFilter(
                            new SubStrFunction(new StrFunction(new VariableTerm($"?{varName}{suffix}")),
                            (new ConstantTerm(node))))).Expression;
                    }
                    else
                        return new VariableTerm($"?{varName}{suffix}");
                }
            }
            else if (filterNode.GetType() == typeof(BinaryOperatorNode))
            {
                var binaryOperator = filterNode as BinaryOperatorNode;
                var left = BuildSparqlFilter(binaryOperator.Left, suffix);
                var right = BuildSparqlFilter(binaryOperator.Right, suffix);

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
                    return new NotEqualsExpression(BuildSparqlFilter(unaryOperator.Operand, suffix),
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
                        p.Name == "LocalId" ? GetClassUri(type) : GetPropertyUri(p)
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
            var classAttribute = GetType(structuredType).GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;
            return classAttribute.Uri;
        }

        protected static Uri GetUri(IEdmEntityType type)
        {
            var classAttribute = GetType(type).GetCustomAttributes(typeof(ClassAttribute), false).Single() as ClassAttribute;

            return classAttribute.Uri;
        }

        protected static Type GetType(IEdmType type)
        {
            var mappingAssembly = typeof(Person).Assembly; // TODO: ???
            return mappingAssembly.GetType(type.FullTypeName());
        }

        protected static Uri GetPropertyUri(IEdmProperty structuralProperty)
        {
            var property = GetType(structuralProperty.DeclaringType).GetProperty(structuralProperty.Name);
            if (property != null)
                return (property.GetCustomAttributes(typeof(PropertyAttribute), false).Single() as PropertyAttribute).Uri;
            else
                return null;
        }

        private class EdmNode
        {
            public string Name { get; set; }
            public EdmEntityType ItemEdmType { get; set; }
            public PatternItem RdfNode { get; set; }
            public List<IEdmStructuralProperty> StructProperties { get; set; }
            public List<CustomNavigationProperty> NavProperties { get; set; }
            public string IdKey { get; set; }
        }

        private class CustomNavigationProperty
        {
            public IEdmNavigationProperty NavigationProperty;
            public List<IEdmStructuralProperty> StructProperties { get; set; }
            public SingleValueNode Filters { get; set; }
            public long? Top { get; set; }
            public long? Skip { get; set; }
            public OrderByClause OrderBy { get; set; }
            public List<EdmNode> NestedEdmNodes;
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
                        NavProperties = new List<CustomNavigationProperty>()
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
                    CustomNavigationProperty navProp = new CustomNavigationProperty();
                    if (propSeg != null)
                    {
                        entityType = propSeg.EdmType.AsElementType() as EdmEntityType;
                        navProp.NavigationProperty = propSeg.NavigationProperty;
                    }
                    else
                    {
                        entityType = propSegLink.EdmType.AsElementType() as EdmEntityType;
                        navProp.NavigationProperty = propSegLink.NavigationProperty;
                    }
                    var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                    edmNode.NavProperties.Add(navProp);

                    EdmNodeList.Add(new EdmNode()
                    {
                        Name = navProp.NavigationProperty.Name,
                        ItemEdmType = entityType,
                        RdfNode = new VariablePattern($"{navProp.NavigationProperty.Name}"),
                        StructProperties = entityType.StructuralProperties().ToList(),
                        NavProperties = new List<CustomNavigationProperty>()
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
                else
                {
                    throw new Exception($"Error with {seg.ToString()}");
                }
            }
        }

        private void ProcessSelectExpandItem(SelectExpandClause clause, EdmNode edmNode, CustomNavigationProperty parentCustomNavProp, NavigationPropertySegment parentSegment)
        {
            if (edmNode == null)
            {
                var entityType = parentSegment.EdmType.AsElementType() as EdmEntityType;
                var RdfNode = new VariablePattern($"{parentSegment.NavigationProperty.Name}");
                edmNode = new EdmNode()
                {
                    Name = parentSegment.NavigationProperty.Name,
                    ItemEdmType = entityType,
                    RdfNode = RdfNode,
                    //StructProperties = entityType.StructuralProperties().ToList(),
                    NavProperties = new List<CustomNavigationProperty>()
                };
                parentCustomNavProp.NestedEdmNodes.Add(edmNode);
            }

            List<IEdmStructuralProperty> structPropList = new List<IEdmStructuralProperty>();
            foreach (var item in clause.SelectedItems)
            {
                var selectItem = item as PathSelectItem;
                if (selectItem != null && selectItem.SelectedPath != null)
                {
                    var segment = selectItem.SelectedPath.FirstSegment as PropertySegment;
                    if (segment != null)
                        structPropList.Add(segment.Property);
                }

                var expandItem = item as ExpandedNavigationSelectItem;
                if (expandItem != null)
                {
                    var segment = expandItem.PathToNavigationProperty.FirstSegment as NavigationPropertySegment;
                    if (segment != null && expandItem.SelectAndExpand != null)
                    {
                        CustomNavigationProperty customNavProp = new CustomNavigationProperty();
                        edmNode.NavProperties.Add(customNavProp);
                        customNavProp.NestedEdmNodes = new List<EdmNode>();
                        customNavProp.NavigationProperty = segment.NavigationProperty;
                        customNavProp.Filters = expandItem.FilterOption != null ? expandItem.FilterOption.Expression : null;
                        customNavProp.Top = expandItem.TopOption.GetValueOrDefault();
                        customNavProp.Skip = expandItem.SkipOption.GetValueOrDefault();
                        customNavProp.OrderBy = expandItem.OrderByOption;
                        ProcessSelectExpandItem(expandItem.SelectAndExpand, null, customNavProp, segment);
                    }
                }
            }
            if (structPropList.Count > 0)
            {
                //edmNode.StructProperties = structPropList;
                if (parentCustomNavProp != null)
                    parentCustomNavProp.StructProperties = structPropList;
            }
        }

        private void ProcessSelectExpand()
        {
            var edmNode = EdmNodeList.Last();
            if (QueryOptions.SelectExpand != null && QueryOptions.SelectExpand.SelectExpandClause != null)
                ProcessSelectExpandItem(QueryOptions.SelectExpand.SelectExpandClause, edmNode, null, null);
        }

        private void ConstructFromNavProperties(EdmNode edmNode, List<ITriplePattern> constructList, List<ITriplePattern> optSubQueryTriplePatterns)
        {
            NodeFactory nodeFactory = new NodeFactory();
            Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(edmNode.ItemEdmType);
            if (edmNode.NavProperties.Count > 0)
            {
                foreach (var expProp in edmNode.NavProperties)
                {
                    var expEntityType = expProp.NavigationProperty.Type.Definition.AsElementType() as EdmEntityType;
                    List<ITriplePattern> tripleList = new List<ITriplePattern>();

                    tripleList.Add(new TriplePattern(new VariablePattern($"?{expProp.NavigationProperty.Name}"),
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                            new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(expEntityType)))));

                    tripleList.Add(new TriplePattern(edmNode.RdfNode,
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[expProp.NavigationProperty.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"?{expProp.NavigationProperty.Name}")));
                    constructList.AddRange(tripleList);

                    List<ITriplePattern> predTripleList = new List<ITriplePattern>();
                    Dictionary<string, Tuple<Type, Uri>> expandProperties = GetAllProperties(expEntityType);

                    List<IEdmStructuralProperty> structProps = expProp.StructProperties;
                    if (structProps == null)
                        structProps = expEntityType.StructuralProperties().ToList();
                    foreach (var prop in structProps.Where(p => p.Name != "LocalId"))
                    {
                        var expPropTriple = new TriplePattern(new VariablePattern($"?{expProp.NavigationProperty.Name}"),
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(expandProperties[prop.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"?{prop.Name}Expand"));
                        predTripleList.Add(expPropTriple);
                        constructList.Add(expPropTriple);
                    }

                    IQueryBuilder subqueryBuilder = null;
                    var variableList = structProps.Where(p => p.Name != "LocalId")
                        .Select(p => $"?{p.Name}Expand").ToList();
                    variableList.Add(expProp.NavigationProperty.Name);
                    if (edmNode.RdfNode is VariablePattern)
                        variableList.Add(edmNode.Name);
                    subqueryBuilder = QueryBuilder.Select(variableList.ToArray()).Where(tripleList.ToArray());
                    foreach (var tp in predTripleList)
                        subqueryBuilder.Optional(gp => gp.Where(tp));
                    if (expProp.Filters != null)
                    {
                        ISparqlExpression FilterExp = BuildSparqlFilter(expProp.Filters, "Expand");
                        subqueryBuilder.Filter(FilterExp);
                    }
                    if (expProp.Top != null & expProp.Top != 0)
                    {
                        subqueryBuilder.Limit(Convert.ToInt32(expProp.Top));
                    }
                    if (expProp.Skip != null & expProp.Skip != 0)
                    {
                        subqueryBuilder.Offset(Convert.ToInt32(expProp.Skip));
                    }
                    if (expProp.OrderBy != null)
                    {
                        foreach (var node in expProp.OrderBy.AsEnumerable())
                        {
                            //var typedNode = node as OrderByPropertyNode;
                            var ordName = (node.Expression as SingleValuePropertyAccessNode).Property.Name;
                            if (ordName.ToLower() == "localid")
                                ordName = expProp.NavigationProperty.Name;
                            else
                                ordName = $"?{ordName}Expand";
                            if (node.Direction == OrderByDirection.Ascending)
                                subqueryBuilder.OrderBy(ordName);
                            else
                                subqueryBuilder.OrderByDescending(ordName);
                        }
                    }
                    optSubQueryTriplePatterns.Add(new SubQueryPattern(subqueryBuilder.BuildQuery()));
                    if (expProp.NestedEdmNodes != null && expProp.NestedEdmNodes.Count > 0)
                    {
                        foreach (var nestedEdmNode in expProp.NestedEdmNodes)
                            ConstructFromNavProperties(nestedEdmNode, constructList, optSubQueryTriplePatterns);
                    }
                }
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
                    foreach (var prop in edmNode.StructProperties.Where(p => p.Name != "LocalId"))
                    {
                        var propTriple = new TriplePattern(edmNode.RdfNode,
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"{prop.Name}"));
                        constructList.Add(propTriple);
                        optionList.Add(propTriple);
                    }
                    ConstructFromNavProperties(edmNode, constructList, optSubQueryTriplePatterns);
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

            ///*OrderBy options*/
            //if (QueryOptions.OrderBy != null && QueryOptions.OrderBy.OrderByClause != null)
            //{
            //    var edmNode = EdmNodeList[EdmNodeList.Count - 1];
            //    foreach (var node in QueryOptions.OrderBy.OrderByNodes)
            //    {
            //        var typedNode = node as OrderByPropertyNode;
            //        var ordName = typedNode.Property.Name;
            //        if (ordName.ToLower() == "localid")
            //            ordName = edmNode.Name;
            //        if (typedNode.OrderByClause.Direction == OrderByDirection.Ascending)
            //            queryBuilder.OrderBy(ordName);
            //        else
            //            queryBuilder.OrderByDescending(ordName);
            //    }
            //}

            return queryBuilder.BuildQuery().ToString();
        }

        private void ProcessSkipTopFilterOrderBy()
        {
            if (QueryOptions.Skip != null || QueryOptions.Top != null ||
                (QueryOptions.OrderBy != null && QueryOptions.OrderBy.OrderByClause != null))
            {
                NodeFactory nodeFactory = new NodeFactory();
                var edmNode = EdmNodeList[EdmNodeList.Count - 1];
                ITriplePattern[] tps;
                List<ITriplePattern> optionList = new List<ITriplePattern>();
                if (EdmNodeList.Count > 1 && (EdmNodeList[EdmNodeList.Count - 2].RdfNode is NodeMatchPattern))
                {
                    var prevEdmNode = EdmNodeList[EdmNodeList.Count - 2];
                    Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(prevEdmNode.ItemEdmType);
                    tps = new ITriplePattern[] {
                        new TriplePattern(prevEdmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[edmNode.RdfNode.VariableName].Item2.AbsoluteUri))),
                        edmNode.RdfNode),
                        new TriplePattern(edmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(edmNode.ItemEdmType))))};
                    foreach (var prop in edmNode.StructProperties.Where(p => p.Name != "LocalId"))
                    {
                        var propTriple = new TriplePattern(edmNode.RdfNode,
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"{prop.Name}"));
                        optionList.Add(propTriple);
                    }
                }
                else
                {
                    tps = new ITriplePattern[] { new TriplePattern(edmNode.RdfNode,
                        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))),
                        new NodeMatchPattern(nodeFactory.CreateUriNode(GetUri(edmNode.ItemEdmType))))};
                    Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(edmNode.ItemEdmType);
                    foreach (var prop in edmNode.StructProperties.Where(p => p.Name != "LocalId"))
                    {
                        var propTriple = new TriplePattern(edmNode.RdfNode,
                            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[prop.Name].Item2.AbsoluteUri))),
                            new VariablePattern($"{prop.Name}"));
                        optionList.Add(propTriple);
                    }
                }

                IQueryBuilder subqueryBuilder = QueryBuilder.Select(new string[] { edmNode.Name }).
                    Where(tps);

                foreach (var tp in optionList)
                    subqueryBuilder.Optional(gp => gp.Where(tp));

                if (QueryOptions.Skip != null)
                    subqueryBuilder = subqueryBuilder.Offset(QueryOptions.Skip.Value);
                if (QueryOptions.Top != null)
                    subqueryBuilder = subqueryBuilder.Limit(QueryOptions.Top.Value);
                if (FilterExp != null)
                {
                    subqueryBuilder = subqueryBuilder.Filter(FilterExp);
                    FilterExp = null;
                }
                if (QueryOptions.OrderBy != null && QueryOptions.OrderBy.OrderByClause != null)
                {
                    foreach (var node in QueryOptions.OrderBy.OrderByNodes)
                    {
                        var typedNode = node as OrderByPropertyNode;
                        var ordName = typedNode.Property.Name;
                        if (ordName.ToLower() == "localid")
                            ordName = edmNode.Name;
                        if (typedNode.OrderByClause.Direction == OrderByDirection.Ascending)
                            subqueryBuilder.OrderBy(ordName);
                        else
                            subqueryBuilder.OrderByDescending(ordName);
                    }
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
            ProcessSkipTopFilterOrderBy();

            return ConstructSparql();
        }
    }
}
