using System.Globalization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Be_QuanLyKhoaHoc.Extensions
{
    // Swashbuckle 10 ghi [Range(0, double.MaxValue)] thành "maximum": 1.7976931348623157E+308.
    // NSwag (đọc số dạng decimal) không parse được giá trị này nên không sinh được client.
    // Bỏ giới hạn trên vô nghĩa đó, giống cách Swashbuckle 7 đã làm; validation phía server không đổi.
    internal sealed class UnboundedMaximumSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is OpenApiSchema concrete && IsDoubleMaxValue(concrete.Maximum))
            {
                concrete.Maximum = null;
            }
        }

        private static bool IsDoubleMaxValue(string? value) =>
            value is not null
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && number >= double.MaxValue;
    }
}
