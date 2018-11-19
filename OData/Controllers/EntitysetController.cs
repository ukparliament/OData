namespace OData
{
    using System;
    using System.Linq;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNetCore.Mvc;

    public class EntitysetController : BaseController
    {
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
        [ODataRoute]
        public IActionResult Get([FromODataUri]string sparql)
        {
            if (this.Request.Query["sparql"].FirstOrDefault() != null &&
                this.Request.Query["sparql"].FirstOrDefault().ToLowerInvariant() == "true")
                // TODO: Refactor so these three can be  shared with BaseController.GenerateODataResult
                return Content(new SparqlBuilder(BaseController.GetQueryOptions(this.Request), new Uri("https://id.parliament.uk/")).BuildSparql());
            else
                return Ok(BaseController.GenerateODataResult(this.Request));
        }
    }
}