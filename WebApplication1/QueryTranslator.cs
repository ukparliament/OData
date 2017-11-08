using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Web.OData.Query;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Patterns;

namespace WebApplication1
{
    internal class QueryTranslator //: ExpressionVisitor
    {
        List<Tuple<string, string[]>> nodes = new List<Tuple<string, string[]>>();
        //private MethodCallExpression innermostWhereExpression;

        //public MethodCallExpression GetInnermostWhere(Expression expression)
        //{
        //    Visit(expression);
        //    return innermostWhereExpression;
        //}

        internal string Translate(ODataQueryOptions options)//Expression expression)
        {
            //Visit(expression);
            return buildSparql(options);
        }

        //protected override Expression VisitLambda<T>(Expression<T> node)
        //{
        //    string[] selects = node.Parameters
        //        .Select(p => p.Name)
        //        .ToArray();
        //    nodes.Add(Tuple.Create<string, string[]>(node.Body.ToString(), selects));
        //    return node;
        //}

        //protected override Expression VisitMethodCall(MethodCallExpression expression)
        //{
        //    if (expression.Method.Name == "Where")
        //        innermostWhereExpression = expression;

        //    Visit(expression.Arguments[0]);

        //    return expression;
        //}
        //SingleValueNode filterExpressions;
        //    if (options.Filter != null)
        //        filterExpressions = options.Filter.FilterClause.Expression;
        //    var selectExpressions = options.SelectExpand;
        //int? top;
        //    if (options.Top != null)
        //        top = options.Top.Value;
        //    int? skip;
        //    if (options.Skip != null)
        //        skip = options.Skip.Value;
        private string buildSparql(ODataQueryOptions options)
        {
            var edmEntityType = options.Context.Path.EdmType.AsElementType() as EdmEntityType;
            //Type entityType = BaseController.GetType(edmEntityType);
            Dictionary<string, Tuple<Type, Uri>> properties = BaseController.GetAllProperties(edmEntityType);

            List<ITriplePattern> classTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> predicateTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> filterTriplePatterns = new List<ITriplePattern>();
            NodeFactory nodeFactory = new NodeFactory();
            int i = 0;

            classTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"),
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
                            //foreach (string predicate in node.Item2)
                            predicateTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"),
                                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[propName].Item2.AbsoluteUri))),
                                new VariablePattern($"?{propName}")));
                        }
                    }
                }
                //i++;
            }
            else
            {
                foreach (var propName in properties.Keys)
                {
                    predicateTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"),
                                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[propName].Item2.AbsoluteUri))),
                                new VariablePattern($"?{propName}")));
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
                        filterTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"),
                                new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(properties[property.Property.Name].Item2.AbsoluteUri))),
                                new NodeMatchPattern(nodeFactory.CreateLiteralNode(constant.LiteralText.Replace("'","")))));
                        //Debug.WriteLine("Property: " + property.Property.Name);
                        //Debug.WriteLine("Operator: " + binaryOperator.OperatorKind);
                        //Debug.WriteLine("Value: " + constant.LiteralText);
                    }
                }
            }

            IQueryBuilder queryBuilder = QueryBuilder
                .Construct(q => q.Where(classTriplePatterns.Concat(predicateTriplePatterns).ToArray()))
                .Where(classTriplePatterns.Concat(filterTriplePatterns).ToArray());

            foreach (TriplePattern tp in predicateTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            if (options.Top != null)
            {
                Debug.WriteLine("Top: " + options.Top.Value);
            }

            if (options.Skip != null)
            {
                Debug.WriteLine("Skip: " + options.Skip.Value);
            }

            if (options.OrderBy != null && options.OrderBy.OrderByClause != null)
            {
                foreach (var node in options.OrderBy.OrderByNodes)
                {
                    var typedNode = node as OrderByPropertyNode;
                    Debug.WriteLine("Property: " + typedNode.Property.Name);
                    Debug.WriteLine("Direction: " + typedNode.OrderByClause.Direction);
                }
            }

            
            //foreach (Tuple<string, string[]> node in nodes)
            //{
            //    classTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"), 
            //        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), 
            //        new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(node.Item1)))));
            //    foreach (string predicate in node.Item2)
            //        predicateTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"), 
            //            new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(predicate))), 
            //            new VariablePattern($"?{new Uri(predicate).Segments.LastOrDefault()}")));
            //    i++;
            //}

            return queryBuilder.BuildQuery().ToString();
        }

    }
}