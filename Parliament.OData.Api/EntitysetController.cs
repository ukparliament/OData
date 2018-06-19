namespace Parliament.OData.Api
{
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using System.Web.Http;

    public class EntitysetController : BaseController
    {
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
        public IHttpActionResult Default()
        {
            var result = GenerateODataResult(Request);
            return Ok(result);
        }
    }
}
