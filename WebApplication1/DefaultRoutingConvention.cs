namespace WebApplication1
{
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;

    internal class DefaultRoutingConvention : IODataRoutingConvention
    {
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            return "Default";
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (request.Method == HttpMethod.Get)
                return "Entityset";
            return null;
        }
    }
}
