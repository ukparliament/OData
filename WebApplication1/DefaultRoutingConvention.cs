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

                //switch (odataPath.PathTemplate)
                //{
                //    case "~/entityset": // http://example.com/Person
                //        return "Entityset";

                //    case "~/entityset/key":// http://example.com/Person('asdf1234')
                //        return "Entityset";

                //    case "~/entityset/key/navigation":// http://example.com/Person('asdf1234')/PersonHasPersonImage
                //        return "Entityset";// "EntitysetKeyNavigation";

                //    case "~/entityset/$count":
                //        return "Entityset";

                //    case "~/entityset/key/navigation/$count":
                //        return "Entityset";

                //    case "~/entityset/key/property":
                //        return "Entityset";

                //    case "~/entityset/key/property/$value":
                //        return "Entityset";

                //    case "~/entityset/key/navigation/$ref":
                //        return "Entityset";
                //    // does this exist? case "~/entityset/key/navigation/key":

                //    case "~/entityset/key/navigation/key":
                //        return "Entityset";

                //    case "~/entityset/key/navigation/key/navigation":
                //        return "Entityset";

                //    case "~/entityset/key/navigation/key/navigation/key":
                //        return "Entityset";

                //    case "~/entityset/key/navigation/key/$ref":
                //        return "Entityset";

                //    case "~/singleton":
                //        //return "Entityset";
                //    //case "~/entityset/key/property/$count": this does not work
                //    //    return "Entityset";


                //    case "~":
                //    case "~/$metadata":
                //    case "~/entityset/action":
                //    case "~/entityset/cast":
                //    case "~/entityset/cast/$count":
                //    case "~/entityset/cast/action":
                //    case "~/entityset/cast/function":
                //    case "~/entityset/cast/function/$count":
                //    case "~/entityset/function":
                //    case "~/entityset/function/$count":
                //    case "~/entityset/key/action":
                //    case "~/entityset/key/cast": // This might work if generated code is explicit interface implementations
                //    case "~/entityset/key/cast/action":
                //    case "~/entityset/key/cast/dynamicproperty":
                //    case "~/entityset/key/cast/function":
                //    case "~/entityset/key/cast/function/$count":
                //    case "~/entityset/key/cast/navigation":
                //    case "~/entityset/key/cast/navigation/$count":
                //    case "~/entityset/key/cast/navigation/$ref":
                //    case "~/entityset/key/cast/navigation/key/$ref":
                //    case "~/entityset/key/cast/property":
                //    case "~/entityset/key/cast/property/$count":
                //    case "~/entityset/key/cast/property/$value":
                //    case "~/entityset/key/cast/property/cast":
                //    case "~/entityset/key/cast/property/dynamicproperty":
                //    case "~/entityset/key/dynamicproperty":
                //    case "~/entityset/key/function":
                //    case "~/entityset/key/function/$count":
                //    case "~/entityset/key/property/cast":
                //    case "~/entityset/key/property/dynamicproperty":
                //    case "~/entityset/unresolved":
                //    case "~/singleton/action":
                //    case "~/singleton/cast":
                //    case "~/singleton/cast/action":
                //    case "~/singleton/cast/dynamicproperty":
                //    case "~/singleton/cast/function ":
                //    case "~/singleton/cast/function/$count":
                //    case "~/singleton/cast/navigation":
                //    case "~/singleton/cast/navigation/$count":
                //    case "~/singleton/cast/navigation/$ref":
                //    case "~/singleton/cast/navigation/key/$ref":
                //    case "~/singleton/cast/property":
                //    case "~/singleton/cast/property/$count":
                //    case "~/singleton/cast/property/$value":
                //    case "~/singleton/cast/property/cast":
                //    case "~/singleton/cast/property/dynamicproperty":
                //    case "~/singleton/dynamicproperty":
                //    case "~/singleton/function":
                //    case "~/singleton/function/$count":
                //    case "~/singleton/navigation":
                //    case "~/singleton/navigation/$count":
                //    case "~/singleton/navigation/$ref":
                //    case "~/singleton/navigation/key/$ref":
                //    case "~/singleton/property":
                //    case "~/singleton/property/$count":
                //    case "~/singleton/property/$value":
                //    case "~/singleton/property/cast":
                //    case "~/singleton/property/dynamicproperty":
                //    default:
                //        break;
                //}
