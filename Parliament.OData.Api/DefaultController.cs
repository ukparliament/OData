namespace WebApplication1
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;
    using System.Web.OData;
    using System.Web.OData.Routing;

    /// <summary>
    /// This controller handles all OData requests except $metadata
    /// </summary>
    public class DefaultController : ODataController
    {
        /// <summary>
        /// This action handles all OData requests
        /// </summary>
        /// <returns>
        /// A list of strings that helps debug OData paths
        /// </returns>
        [HttpGet]
        public IEnumerable<string> Default()
        {
            var path = this.Request.Properties["System.Web.OData.Path"] as ODataPath;
            return path.Segments.Select(
                segment => string.Format(
                    "type: {0}, id: {1}, edmType: {2}",
                    segment,
                    segment.Identifier,
                    segment.EdmType));
        }
    }
}
