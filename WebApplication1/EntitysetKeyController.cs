namespace WebApplication1
{
    using Parliament.Ontology.Base;
    using Parliament.Ontology.Code;
    using System.Linq.Expressions;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Query;
    using System.Web.OData.Routing;

    public class EntitysetKeyController : BaseController
    {
        //http://localhost:2933/House('1AFu55Hs')
        //http://localhost:2933/House('1AFu55Hs')?$select=HouseName
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = System.Web.OData.Query.AllowedQueryOptions.Select |
            System.Web.OData.Query.AllowedQueryOptions.Filter)]
        public IHttpActionResult Default()
        //ODataQueryOptions<IOntologyInstance> options
        {
            ODataQueryOptions option = GetQueryOptions(Request);
            object result = GenerateODataResult(option);
            return Ok(result);
        }
        
    }
}
