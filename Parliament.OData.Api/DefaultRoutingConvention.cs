namespace Parliament.OData.Api
{
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;

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
