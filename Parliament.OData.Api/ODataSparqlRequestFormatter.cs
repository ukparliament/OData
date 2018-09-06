namespace Parliament.OData.Api
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Text;

    internal class ODataSparqlRequestFormatter : BufferedMediaTypeFormatter
    {
        public ODataSparqlRequestFormatter()
        {
            this.AddQueryStringMapping("sparql", bool.TrueString.ToLower(), "text/plain");

            this.SupportedEncodings.Add(Encoding.UTF8);
        }

        public override bool CanReadType(Type type) => false;

        public override bool CanWriteType(Type type)
        {
            return type == typeof(string);
        }

        public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {
            using (var writer = new StreamWriter(writeStream, this.SelectCharacterEncoding(content.Headers)))
            {
                writer.Write(value as string);
            }
        }
    }
}
