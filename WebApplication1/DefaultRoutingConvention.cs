namespace WebApplication1
{
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;

    /// <summary>
    /// An OData routing convention that sends all requests to the defaul controller, which could get data based class and property uris extracted from attributes of mapping interfaces.
    /// </summary>
    internal class DefaultRoutingConvention : IODataRoutingConvention
    {
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            return "Default";
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return "Default";
        }
    }
}