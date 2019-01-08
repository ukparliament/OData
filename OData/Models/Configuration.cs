namespace OData
{
    using System;
    using Microsoft.OpenApi.Writers;

    internal class Configuration
    {
        internal static readonly (string MediaType, string Extension, Type WriterType)[] OpenApiMappings = new[] {
            ("application/json", "json", typeof(OpenApiJsonWriter)),
            ("text/vnd.yaml", "yaml", typeof(OpenApiYamlWriter))
        };

        public QueryConfiguration Query { get; set; }

        internal class QueryConfiguration
        {
            public string Endpoint { get; set; }

            public string ApiVersion { get; set; }

            public string SubscriptionKey { get; set; }
        }
    }
}
