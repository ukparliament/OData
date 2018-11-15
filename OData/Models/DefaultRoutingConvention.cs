namespace OData
{
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Routing;
    using System.Collections.Generic;
    using System.Reflection;

    internal class DefaultRoutingConvention : IODataRoutingConvention
    {
        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            if (routeContext.RouteData.Values["odataPath"] == null ||
            routeContext.RouteData.Values["odataPath"].ToString() == "$metadata")
                return new MetadataRoutingConvention().SelectAction(routeContext);

            ControllerActionDescriptor odataControllerDescriptor = new ControllerActionDescriptor
            {
                ControllerName = "Entityset",
                ActionName = "Get",
                Parameters = new List<ParameterDescriptor>(),
                FilterDescriptors = new List<FilterDescriptor>(),
                BoundProperties = new List<ParameterDescriptor>(),
                MethodInfo = typeof(EntitysetController).GetMethod("Get"),
                ControllerTypeInfo = typeof(EntitysetController).GetTypeInfo()
            };

            //ControllerActionDescriptor odataControllerDescriptor1 = new ControllerActionDescriptor
            //{
            //    ControllerName = "EntityMaintenance",
            //    ActionName = "Post,Put,Delete",
            //    Parameters = new List<ParameterDescriptor>(),
            //    FilterDescriptors = new List<FilterDescriptor>(),
            //    BoundProperties = new List<ParameterDescriptor>(),
            //    MethodInfo = typeof(EntitysetController).GetMethod("Default"),
            //    ControllerTypeInfo = typeof(EntitysetController).GetTypeInfo()
            //};

            return new List<ControllerActionDescriptor> { odataControllerDescriptor };
            //odataControllerDescriptor1};
        }
    }
}
