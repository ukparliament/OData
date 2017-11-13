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

    public class EntitysetController : BaseController
    {
        //http://localhost:2933/House
        //http://localhost:2933/House?$select=HouseName
        //http://localhost:2933/House('1AFu55Hs')
        //http://localhost:2933/House('1AFu55Hs')?$select=HouseName
        //http://localhost:2933/Person('QdTpvoeQ')?$select=PersonGivenName
        //http://localhost:2933/House('1AFu55Hs')?$filter=HouseName%20eq%20%27House%20of%20Commons%27&$select=Id,HouseName
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseSeat?$top=2
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseIncumbency?$orderby=ParliamentaryIncumbencyStartDate%20desc&$top=2


        [HttpGet]
        //[EnableQuery(AllowedQueryOptions = System.Web.OData.Query.AllowedQueryOptions.Select |
        //    System.Web.OData.Query.AllowedQueryOptions.Filter)]
        public IHttpActionResult Default()
        {
            ODataQueryOptions option = GetQueryOptions(Request);
            object result = GenerateODataResult(option);

            return Ok(result);
        }

    }
}
