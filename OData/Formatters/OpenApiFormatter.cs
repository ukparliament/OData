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
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Writers;

    internal class OpenApiFormatter : TextOutputFormatter
    {
        private readonly Type writer;

        internal OpenApiFormatter(string mediaType, Type writer)
        {
            this.SupportedMediaTypes.Add(mediaType);
            this.SupportedEncodings.Add(Encoding.UTF8);

            this.writer = writer;
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(OpenApiDocument).IsAssignableFrom(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding encoding)
        {
            return new TaskFactory().StartNew(() =>
            {
                var document = context.Object as OpenApiDocument;
                using (var writer = context.WriterFactory(context.HttpContext.Response.Body, encoding))
                {
                    document.SerializeAsV3(Activator.CreateInstance(this.writer, writer) as IOpenApiWriter);
                }
            });
        }
    }
}
