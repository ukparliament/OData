namespace OData
{
    using System;
    using System.Linq;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    public class EntitysetController : BaseController
    {
        IConfiguration _configuration;
        public EntitysetController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
        [ODataRoute]
        public IActionResult Get([FromODataUri]string sparql)
        {
            string sparqlEndpoint = _configuration["SparqlEndpoint"];
            string nameSpace = _configuration["Namespace"];
            if (this.Request.Query["sparql"].FirstOrDefault() != null &&
                this.Request.Query["sparql"].FirstOrDefault().ToLowerInvariant() == "true")
                // TODO: Refactor so these three can be  shared with BaseController.GenerateODataResult
                return Content(new SparqlBuilder(BaseController.GetQueryOptions(this.Request), new Uri(nameSpace)).BuildSparql());
            else
                return Ok(BaseController.GenerateODataResult(this.Request, sparqlEndpoint, nameSpace));
        }
    }
}