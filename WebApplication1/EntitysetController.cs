namespace WebApplication1
{
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Query;
    using System.Web.OData.Routing;

    public static class ODataQueryOptionsExtensions
    {
        public static Expression ToExpression<TElement>(this FilterQueryOption filter)
        {
            IQueryable queryable = Enumerable.Empty<TElement>().AsQueryable();
            queryable = filter.ApplyTo(queryable, new ODataQuerySettings());
            return queryable.Expression;
        }
    }

    public class EntitysetController : BaseController
    {
        //http://localhost:2933/House
        //http://localhost:2933/House?$select=HouseName
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = System.Web.OData.Query.AllowedQueryOptions.Select |
            System.Web.OData.Query.AllowedQueryOptions.Filter)]
        public IHttpActionResult Default()
        {
            ODataQueryOptions option = GetQueryOptions(Request);
            object result = GenerateODataResult(option);

            return Ok(result);
        }

    }
}
