using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Patterns;

namespace WebApplication1
{
    internal class QueryTranslator : ExpressionVisitor
    {
        List<Tuple<string, string[]>> nodes = new List<Tuple<string, string[]>>();

        internal string Translate(Expression expression)
        {
            Visit(expression);
            return buildSparql();
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            string[] selects = node.Parameters
                .Select(p => p.Name)
                .ToArray();
            nodes.Add(Tuple.Create<string, string[]>(node.Body.ToString(), selects));
            return node;
        }

        private string buildSparql()
        {
            List<ITriplePattern> classTriplePatterns = new List<ITriplePattern>();
            List<ITriplePattern> predicateTriplePatterns = new List<ITriplePattern>();
            NodeFactory nodeFactory = new NodeFactory();
            int i = 0;
            foreach (Tuple<string, string[]> node in nodes)
            {
                classTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"), new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(node.Item1)))));
                foreach (string predicate in node.Item2)
                    predicateTriplePatterns.Add(new TriplePattern(new VariablePattern($"?s{i}"), new NodeMatchPattern(nodeFactory.CreateUriNode(new Uri(predicate))), new VariablePattern($"?{new Uri(predicate).Segments.LastOrDefault()}")));
                i++;
            }
            IQueryBuilder queryBuilder = QueryBuilder
                .Construct(q => q.Where(classTriplePatterns.Concat(predicateTriplePatterns).ToArray()))
                .Where(classTriplePatterns.ToArray());

            foreach (TriplePattern tp in predicateTriplePatterns)
                queryBuilder.Optional(gp => gp.Where(tp));

            return queryBuilder.BuildQuery().ToString();
        }

    }
}