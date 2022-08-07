using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Own.Platform.Application
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSwagger(this IServiceCollection services, Type type, string title, string description)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = title,
                    Description = description,
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Quinntyne Brown",
                        Email = "quinntynebrown@gmail.com"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under MIT",
                        Url = new Uri("https://opensource.org/licenses/MIT"),
                    }
                });

                options.EnableAnnotations();

                var xmlFilename = $"{type.Assembly.GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            }).AddSwaggerGenNewtonsoftSupport();
        }

        public static void UseSwagger(this WebApplication app)
        {
            app.UseSwagger(options => options.SerializeAsV2 = true);
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "");
                options.RoutePrefix = string.Empty;
                options.DisplayOperationId();
            });
        }
    }
}
