using McAttributes.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Net.Http.Headers;
using System.Text;

namespace McAttributes {
    public class CsvOutputFormatter: TextOutputFormatter {
        public CsvOutputFormatter() {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type? type)
            => typeof(User).IsAssignableFrom(type)
                || typeof(IEnumerable<User>).IsAssignableFrom(type);

        public override async Task WriteResponseBodyAsync(
            OutputFormatterWriteContext context, Encoding selectedEncoding) {
            var httpContext = context.HttpContext;
            var serviceProvider = httpContext.RequestServices;

            var logger = serviceProvider.GetRequiredService<ILogger<CsvOutputFormatter>>();
            var buffer = new StringBuilder();

            if (context.Object is IEnumerable<User> users) {
                foreach (var user in users) {
                    FormatCsvLine(buffer, user, logger);
                }
            } else {
                FormatCsvLine(buffer, (User)context.Object!, logger);
            }

            await httpContext.Response.WriteAsync(buffer.ToString(), selectedEncoding);
        }

        private static void FormatCsvLine(
            StringBuilder buffer, User user, ILogger logger) {
            


            logger.LogInformation("Writing {Id} {AadId}",
                user.Id, user.AadId);
        }
    }
    
}
