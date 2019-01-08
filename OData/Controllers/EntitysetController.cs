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
    using System;
    using System.Linq;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    public class EntitysetController : BaseController
    {
        IConfiguration _configuration;
        public EntitysetController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, MaxExpansionDepth = 4)]
        [ODataRoute]
        public IActionResult Get([FromODataUri]string sparql)
        {
            string sparqlEndpoint = _configuration["SparqlEndpoint"];
            string nameSpace = _configuration["Namespace"];
            if (this.Request.Query["sparql"].FirstOrDefault() != null &&
                this.Request.Query["sparql"].FirstOrDefault().ToLowerInvariant() == "true")
                // TODO: Refactor so these three can be  shared with BaseController.GenerateODataResult
                return Content(new SparqlBuilder(BaseController.GetQueryOptions(this.Request), new Uri(nameSpace)).BuildSparql());
            else
                return Ok(BaseController.GenerateODataResult(this.Request, sparqlEndpoint, nameSpace));
        }
    }
}