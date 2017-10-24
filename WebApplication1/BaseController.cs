namespace WebApplication1
{
    using Microsoft.OData.Edm;
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using Parliament.Ontology.Serializer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Http.Results;
    using System.Web.OData;
    using System.Web.OData.Routing;
    using VDS.RDF;
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
            return mappingAssembly.GetType(type.FullTypeName()).GetInterface($"I{((EdmEntityType)type).Name}");
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

        //work only on primitive types and implement select option only
        protected static Expression GenerateExpression(ODataPath path, string requestQuery)
        {
            var edmType = path.EdmType.AsElementType() as EdmEntityType;

            Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(edmType);

            string[] queryStrings = requestQuery
                .Replace("?", string.Empty)
                .Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            List<Expression> expressions = new List<Expression>();
            Expression projection = null;
            foreach (string queryString in queryStrings)
            {
                if (queryString.StartsWith("$filter"))
                {
                }
                if (queryString.StartsWith("$select"))
                {
                    string[] selectQueries = queryString
                        .Substring(queryString.IndexOf('=') + 1)
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    projection = generateProjectionExpression(selectQueries, edmType);
                    expressions.Add(projection);
                }
            }
            if (projection == null)
            {
                projection = generateProjectionExpression(null, edmType);
                expressions.Add(projection);
            }

            Expression expression = Expression.Empty();
            if (expressions.Any())
                expression = Expression.Block(expressions);

            return expression;
        }

        private static Expression generateProjectionExpression(string[] selectQueries, EdmEntityType edmEntityType)
        {
            Type entityType = BaseController.GetType(edmEntityType);
            Dictionary<string, Tuple<Type, Uri>> properties = GetAllProperties(edmEntityType);

            List<Expression> expressions = new List<Expression>();
            List<ParameterExpression> selectParameterExpressions = new List<ParameterExpression>();
            if (selectQueries == null)
                selectQueries = properties.Select(p => p.Key).ToArray();
            foreach (string selectQuery in selectQueries)
            {
                if ((properties.ContainsKey(selectQuery)) && (selectQuery != "Id"))
                    selectParameterExpressions.Add(Expression.Parameter(properties[selectQuery].Item1, properties[selectQuery].Item2.AbsoluteUri));
            }
            return Expression.Lambda(Expression.Parameter(entityType, properties["Id"].Item2.AbsoluteUri), selectParameterExpressions);
        }

        protected static object GenerateODataResult(Expression expression)
        {
            DbQueryProvider dbQueryProvider = new DbQueryProvider();
            IEnumerable<IOntologyInstance> result = dbQueryProvider.Execute(expression) as IEnumerable<IOntologyInstance>;
            MethodInfo castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(result.First().GetType());

            return castMethod.Invoke(result, new object[] { result });
        }

        private static Type convertPrimitiveEdmTypeToType(IEdmPrimitiveType edmType, bool isNullable)
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
                        p.Type.Definition.TypeKind == EdmTypeKind.Primitive ? convertPrimitiveEdmTypeToType(p.Type.Definition as IEdmPrimitiveType, p.Type.IsNullable) : typeof(object),
                        p.Name == "Id" ? getClassUri(type) : GetPropertyUri(p)
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

        private static Uri getClassUri(IEdmStructuredType structuredType)
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
