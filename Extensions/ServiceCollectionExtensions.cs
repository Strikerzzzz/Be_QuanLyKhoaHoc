using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Be_QuanLyKhoaHoc.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
        {
            services.AddSwaggerGen(o =>
            {
                //o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Nhập token của bạn theo định dạng: Bearer {your_token}",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                };

                o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme
                            }
                        },
                        new string[] {}
                    }
                };

                o.AddSecurityRequirement(securityRequirement);
            });
            return services;
        }
    }
}
