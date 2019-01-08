// MIT License
//
// Copyright (c) 2019 UK Parliament
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

            return new List<ControllerActionDescriptor> { odataControllerDescriptor };
        }
    }
}
