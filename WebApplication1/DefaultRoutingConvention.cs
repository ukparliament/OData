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
            {
                switch (odataPath.PathTemplate)
                {
                    case "~/entityset": // http://example.com/Person
                        return "Entityset";

                    case "~/entityset/key":// http://example.com/Person('asdf1234')
                        return "EntitysetKey";

                    case "~/entityset/key/navigation":// http://example.com/Person('asdf1234')/PersonHasPersonImage
                        return "EntitysetKey";// "EntitysetKeyNavigation";

                    case "~":
                    case "~/$metadata":
                    case "~/entityset/$count":
                    case "~/entityset/action":
                    case "~/entityset/cast":
                    case "~/entityset/cast/$count":
                    case "~/entityset/cast/action":
                    case "~/entityset/cast/function":
                    case "~/entityset/cast/function/$count":
                    case "~/entityset/function":
                    case "~/entityset/function/$count":
                    case "~/entityset/key/action":
                    case "~/entityset/key/cast": // This might work if generated code is explicit interface implementations
                    case "~/entityset/key/cast/action":
                    case "~/entityset/key/cast/dynamicproperty":
                    case "~/entityset/key/cast/function":
                    case "~/entityset/key/cast/function/$count":
                    case "~/entityset/key/cast/navigation":
                    case "~/entityset/key/cast/navigation/$count":
                    case "~/entityset/key/cast/navigation/$ref":
                    case "~/entityset/key/cast/navigation/key/$ref":
                    case "~/entityset/key/cast/property":
                    case "~/entityset/key/cast/property/$count":
                    case "~/entityset/key/cast/property/$value":
                    case "~/entityset/key/cast/property/cast":
                    case "~/entityset/key/cast/property/dynamicproperty":
                    case "~/entityset/key/dynamicproperty":
                    case "~/entityset/key/function":
                    case "~/entityset/key/function/$count":
                    case "~/entityset/key/navigation/$count":
                    case "~/entityset/key/navigation/$ref":
                    case "~/entityset/key/navigation/key/$ref":
                    case "~/entityset/key/property":
                    case "~/entityset/key/property/$count":
                    case "~/entityset/key/property/$value":
                    case "~/entityset/key/property/cast":
                    case "~/entityset/key/property/dynamicproperty":
                    case "~/entityset/unresolved":
                    case "~/singleton":
                    case "~/singleton/action":
                    case "~/singleton/cast":
                    case "~/singleton/cast/action":
                    case "~/singleton/cast/dynamicproperty":
                    case "~/singleton/cast/function ":
                    case "~/singleton/cast/function/$count":
                    case "~/singleton/cast/navigation":
                    case "~/singleton/cast/navigation/$count":
                    case "~/singleton/cast/navigation/$ref":
                    case "~/singleton/cast/navigation/key/$ref":
                    case "~/singleton/cast/property":
                    case "~/singleton/cast/property/$count":
                    case "~/singleton/cast/property/$value":
                    case "~/singleton/cast/property/cast":
                    case "~/singleton/cast/property/dynamicproperty":
                    case "~/singleton/dynamicproperty":
                    case "~/singleton/function":
                    case "~/singleton/function/$count":
                    case "~/singleton/navigation":
                    case "~/singleton/navigation/$count":
                    case "~/singleton/navigation/$ref":
                    case "~/singleton/navigation/key/$ref":
                    case "~/singleton/property":
                    case "~/singleton/property/$count":
                    case "~/singleton/property/$value":
                    case "~/singleton/property/cast":
                    case "~/singleton/property/dynamicproperty":
                    default:
                        break;
                }
            }

            return null;
        }
    }
}