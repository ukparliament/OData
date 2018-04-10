using System;
using System.Configuration;
using System.Net;
using VDS.RDF.Query;

namespace Parliament.OData.Api
{
    public class GraphDBSparqlEndpoint : SparqlRemoteEndpoint
    {
        private static readonly string sparqlEndpoint = ConfigurationManager.ConnectionStrings["SparqlEndpoint"].ConnectionString;
        private static readonly string apiVersion = ConfigurationManager.AppSettings["ApiVersion"];
        private static readonly string subscriptionKey = ConfigurationManager.AppSettings["SubscriptionKey"];
        private static readonly Uri endpoint = new Uri(sparqlEndpoint);

        public GraphDBSparqlEndpoint() : base(endpoint)
        {
        }

        protected override void ApplyCustomRequestOptions(HttpWebRequest httpRequest)
        {
            base.ApplyCustomRequestOptions(httpRequest);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            httpRequest.Headers.Add("Api-Version", apiVersion);
        }
    }
}