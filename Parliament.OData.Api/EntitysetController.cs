namespace Parliament.OData.Api
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Web.Http;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;

    public class CustomEnableQueryAttribute : EnableQueryAttribute
    {
        public override IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
        {
            var ignoreQueryOptions = AllowedQueryOptions.Skip | AllowedQueryOptions.Top;
            return queryOptions.ApplyTo(queryable, ignoreQueryOptions);
        }
    }
    public class EntitysetController : BaseController
    {
        [HttpGet]
        [CustomEnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
        public IHttpActionResult Default()
        {
            var result = BaseController.GenerateODataResult(this.Request);
            return this.Ok(result);
        }

        [HttpGet]
        public IHttpActionResult Default([FromUri]string sparql)
        {
            if (sparql == bool.TrueString.ToLowerInvariant())
            {
                // TODO: Refactor so these three can be  shared with BaseController.GenerateODataResult
                var options = BaseController.GetQueryOptions(this.Request);
                var NamespaceUri = new Uri(ConfigurationManager.AppSettings["IdNamespace"]);
                var queryString = new SparqlBuilder(options, NamespaceUri).BuildSparql();

                return this.Ok(queryString);
            }

            return this.BadRequest();
        }
    }
}
