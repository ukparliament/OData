namespace Parliament.OData.Api
{
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Query;

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
