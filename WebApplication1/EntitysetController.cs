namespace WebApplication1
{
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Query;

    public class EntitysetController : BaseController
    {
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public IHttpActionResult Default()
        {
            var result = GenerateODataResult(Request);
            return Ok(result);
        }
    }
}
