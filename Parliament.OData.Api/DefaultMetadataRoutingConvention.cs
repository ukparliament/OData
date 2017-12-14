namespace Parliament.OData.Api
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;
    using System.Web.OData.Routing;
    using System.Web.OData.Routing.Conventions;

    internal class DefaultMetadataRoutingConvention : IODataRoutingConvention
    {
        private MetadataRoutingConvention _defaultRouting = new MetadataRoutingConvention();
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            return _defaultRouting.SelectAction(odataPath, controllerContext, actionMap);
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            var externalAddress = ConfigurationManager.AppSettings["ExternalAPIAddress"];
            Uri uriToReplace = null;
            //if (request.RequestUri.Port == 80)
            uriToReplace = new Uri($"http://{request.RequestUri.Host}");
            //else
            //    uriToReplace = new Uri($"http://{request.RequestUri.Host}:{request.RequestUri.Port}");
            request.RequestUri = new Uri(request.RequestUri.AbsoluteUri.Replace(uriToReplace.AbsoluteUri, externalAddress));

            return _defaultRouting.SelectController(odataPath, request);
        }
    }
}
