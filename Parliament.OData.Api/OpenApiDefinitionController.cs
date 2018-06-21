using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace Parliament.OData.Api
{
    public class OpenApiDefinitionController:ApiController
    {
        public HttpResponseMessage Get()
        {
            string json = null;
            using (Stream sparqlResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Parliament.OData.Api.OpenApiDefinition.json"))
            {
                using (StreamReader reader = new StreamReader(sparqlResourceStream))
                    json = reader.ReadToEnd();
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            return response;
        }
    }
}