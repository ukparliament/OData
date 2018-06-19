namespace Parliament.OData.Api
{
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Routing;

    public class CustomUrlHelper : UrlHelper
    {
        public CustomUrlHelper(HttpRequestMessage request) : base(request) { }

        public override string Link(string routeName, IDictionary<string, object> routeValues)
        {
            var link = base.Link(routeName, routeValues);

            if (routeName == Global.ODataRouteName)
            {
                string uriToReplace = null;
                //if (Request.RequestUri.Port == 80)
                    uriToReplace = $"https://{Request.RequestUri.Host}";
                //else
                //    uriToReplace = $"http://{Request.RequestUri.Host}:{Request.RequestUri.Port}";
                string ExternalAPIAddress = ConfigurationManager.AppSettings["ExternalAPIAddress"];
                if (ExternalAPIAddress.EndsWith("/"))
                    ExternalAPIAddress = ExternalAPIAddress.Substring(0, ExternalAPIAddress.Length - 1);  // Do not end with slash
                return link.Replace(uriToReplace, ExternalAPIAddress);
            }

            return link;
        }
    }

    internal class DefaultMetadataRoutingConvention : IODataRoutingConvention
    {
        private MetadataRoutingConvention _defaultRouting = new MetadataRoutingConvention();

        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            var helper = new CustomUrlHelper(controllerContext.Request);
            controllerContext.RequestContext.Url = helper;
            return _defaultRouting.SelectAction(odataPath, controllerContext, actionMap);
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            return _defaultRouting.SelectController(odataPath, request);
        }
    }
}
