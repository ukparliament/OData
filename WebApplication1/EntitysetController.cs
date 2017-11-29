namespace WebApplication1
{
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
        //http://localhost:2933/House('WkUWUBMx')/HouseHasHouseIncumbency?$orderby=ParliamentaryIncumbencyStartDate%20desc&$top=2
        //http://localhost:2933/Person?$top=100&$orderby=PersonGivenName,PersonFamilyName%20desc
        //http://localhost:2933/Incumbency?$filter=IncumbencyStartDate%20ge%202017-11-09T00:00:00Z
        //http://localhost:2933/House('WkUWUBMx')/HouseHasHouseIncumbency?$filter=ParliamentaryIncumbencyStartDate%20gt%201983-11-17T00:00:00Z
        //http://localhost:2933/Person?$filter=contains(PersonGivenName,%27David%27)
        //http://localhost:2933/House('1AFu55Hs')?$filter=contains(HouseName,%20%27Ho%27)
        //http://localhost:2933/House('WkUWUBMx')/HouseHasHouseIncumbency?$filter=ParliamentaryIncumbencyStartDate%20ge%201983-11-17T00:00:00Z&$orderby=ParliamentaryIncumbencyStartDate%20desc
        //http://localhost:2933/House('WkUWUBMx')/HouseHasHouseIncumbency?$filter=ParliamentaryIncumbencyStartDate%20ge%201983-11-17T00:00:00Z%20and%20ParliamentaryIncumbencyStartDate%20lt%201984-11-17T00:00:00Z&$orderby=ParliamentaryIncumbencyStartDate
        //http://localhost:2933/House('WkUWUBMx')/HouseHasHouseIncumbency?$filter=ParliamentaryIncumbencyStartDate%20ge%201983-11-17T00:00:00Z%20and%20ParliamentaryIncumbencyStartDate%20lt%201984-01-17T00:00:00Z%20or%20ParliamentaryIncumbencyStartDate%20gt%202016-11-17T00:00:00Z&$orderby=ParliamentaryIncumbencyStartDate
        //http://localhost:2933/House?$filter=length(HouseName)%20gt%2014
        //http://localhost:2933/House?$filter=contains(tolower(HouseName),%20%27h%27)
        //http://localhost:2933/House?$filter=contains(tolower(HouseName),%20%27H%27)
        //http://localhost:2933/House?$filter=contains(toupper(tolower(substring(HouseName,%206))),%20%27L%27)
        //http://localhost:2933/House?$filter=contains(toupper(tolower(replace(HouseName,%27o%27,%20%271%27))),%20%27O%27)
        //http://localhost:2933/House?$filter=contains(concat(HouseName,%20%2712345%27),%20%27ds12%27)
        //http://localhost:2933/House?$filter=contains(substring(HouseName,%201,%202),%27Hou%27)
        //http://localhost:2933/Incumbency?$filter=month(IncumbencyStartDate)%20gt%2010%20and%20year(IncumbencyStartDate)%20gt%201998
        //http://localhost:2933/Incumbency?$filter=not(year(IncumbencyStartDate)%20lt%201998)
        //http://localhost:2933/House?$filter=not(length(HouseName)%20lt%208)
        //http://localhost:2933/House?$filter=not%20endswith(HouseName,%20%27ords%27)
        //http://localhost:2933/Person?$filter=length(substring(PersonGivenName,%202))%20sub%20length(PersonFamilyName)%20eq%202
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseSeat/$count
        //http://localhost:2933/House('1AFu55Hs')?$expand=HouseHasFormalBody,HouseHasHouseIncumbency
        //http://localhost:2933/House('1AFu55Hs')/HouseHasFormalBody('tz34m7Vt')
        //http://localhost:2933/House/$count
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseSeat('TakJEinu')/$ref
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseSeat/$ref
        //http://localhost:2933/House('1AFu55Hs')/HouseName/$value
        //http://localhost:2933/House('1AFu55Hs')/HouseName
        //http://localhost:2933/House('1AFu55Hs')/HouseHasHouseSeat/$count

        //combination of expand and select, need to debug.
        //http://localhost:2933/House('1AFu55Hs')?$expand=HouseHasHouseSeat&$select=HouseName,HouseHasHouseSeat/HouseSeatName


        [HttpGet]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Select |
            AllowedQueryOptions.Filter |
            AllowedQueryOptions.Expand |
            AllowedQueryOptions.All, MaxTop =100)]
        public IHttpActionResult Default(ODataPath odataPath)
        {
            ODataQueryOptions option = GetQueryOptions(Request, odataPath);
            object result = GenerateODataResult(option, odataPath);
            /*Format options*/
            if (option.RawValues.Format != null)
            {
                string format = option.RawValues.Format.ToLower(); //atom, xml, json
            }
            var response = Ok(result);
            return response;
        }
    }
}
