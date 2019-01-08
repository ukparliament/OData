namespace OData
{
    using System.Linq;
    using System.Reflection;
    using Microsoft.OpenApi.Exceptions;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Readers;

    public static class Resources
    {
        public static OpenApiDocument OpenApiDocument
        {
            get
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OData.Resources.OpenApiDocumentTemplate.json"))
                {
                    var reader = new OpenApiStreamReader();
                    var document = reader.Read(stream, out var diagnostic);

                    if (diagnostic.Errors.Any())
                    {
                        throw new OpenApiException(diagnostic.Errors.First().Message);
                    }
                    return document;
                }
            }
        }
    }
}
