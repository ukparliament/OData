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
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;
    using System.Collections.Generic;

    public class CustomUrlHelper : UrlHelper
    {
        string _externalAPIAddress;

        public CustomUrlHelper(ActionContext actionContext, string ExternalAPIAddress) : base(actionContext)
        {
            _externalAPIAddress = ExternalAPIAddress;
        }

        public override string Link(string routeName, object values)
        {
            var link = base.Link(routeName, values);

            if (routeName == "odata")
            {
                string ExternalAPIAddress = _externalAPIAddress;
                if (_externalAPIAddress.EndsWith("/"))
                    ExternalAPIAddress = ExternalAPIAddress.Substring(0, ExternalAPIAddress.Length - 1);  // Do not end with slash
                var newLink = link.Replace($"https://{HttpContext.Request.Host}", ExternalAPIAddress);
                return newLink;
            }

            return link;
        }
    }

    internal class DefaultMetadataRoutingConvention : IODataRoutingConvention
    {
        string _externalAPIAddress;
        public DefaultMetadataRoutingConvention(string ExternalAPIAddress)
        {
            _externalAPIAddress = ExternalAPIAddress;
        }
        private MetadataRoutingConvention _defaultRouting = new MetadataRoutingConvention();

        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            var helper = new CustomUrlHelper(new ActionContext(routeContext.HttpContext, routeContext.RouteData, new ActionDescriptor()), _externalAPIAddress);
            routeContext.HttpContext.Request.ODataFeature().UrlHelper = helper;
            return _defaultRouting.SelectAction(routeContext);
        }
    }
}
