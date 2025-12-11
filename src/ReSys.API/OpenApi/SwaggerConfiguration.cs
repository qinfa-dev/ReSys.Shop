using System.Text.Json;
using System.Text.RegularExpressions;

using FluentStorage.Utils.Extensions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using ReSys.Infrastructure.Security.Authentication.Externals.Options;

using Serilog;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ReSys.API.OpenApi;

public static class SwaggerConfiguration
{
    internal static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(setupAction: o =>
        {
            o.SwaggerDoc(name: "v1", info: new OpenApiInfo
            {
                Title = "Stellar FashionShop API",
                Version = "v1",
                Description = "An API for managing fashion shop operations."
            });

            o.CustomSchemaIds(type => SchemaIdStrategy.GenerateSchemaId(type));
            o.SchemaFilter<SnakeCaseSchemaFilter>();
            o.ParameterFilter<SnakeCaseParameterFilter>();

            // Add security schemes
            o.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme,
                securityScheme: CreateJwtSecurityScheme());
            o.AddSecurityDefinition(name: "Google",
                securityScheme: CreateGoogleOAuth2Scheme());
            o.AddSecurityDefinition(name: "Facebook",
                securityScheme: CreateFacebookOAuth2Scheme());

            // Add security requirements
            o.UseAllOfToExtendReferenceSchemas();
            o.AddSecurityRequirement(securityRequirement: CreateJwtSecurityRequirement());
            o.AddSecurityRequirement(securityRequirement: CreateGoogleSecurityRequirement());
            o.AddSecurityRequirement(securityRequirement: CreateFacebookSecurityRequirement());

            o.DocumentFilter<AuthEndpointOrderDocumentFilter>();
        });

        Log.Information(messageTemplate: "Register: Swagger with JWT, Google OAuth2, and Facebook OAuth2 authentication configuration.");
        return services;
    }

    internal static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        GoogleOption googleOptions = app.Services.GetRequiredService<IOptions<GoogleOption>>().Value;
        FacebookOption facebookOptions = app.Services.GetRequiredService<IOptions<FacebookOption>>().Value;
        
        app.UseSwagger(setupAction: options => options.RouteTemplate = "/openapi/{documentName}.json");
        app.UseSwaggerUI(setupAction: c =>
        {
            c.SwaggerEndpoint(url: "/openapi/v1.json",
                name: "Stellar FashionShop API V1");
            c.RoutePrefix = string.Empty;
            
            //// OAuth2 configuration for Google (uses PKCE)
            //c.OAuthClientId(value: googleOptions.ClientId);
            //c.OAuthAppName(value: "Stellar FashionShop API");
            //c.OAuthUsePkce();
            //c.OAuthScopeSeparator(value: " ");
            
            //// OAuth2 configuration for Facebook
            //c.OAuthAdditionalQueryStringParams(value: new Dictionary<string, string>
            //{
            //    { "response_type", "code" },
            //    { "client_id", facebookOptions.AppId }
            //});
            
            //// Set OAuth2 redirect URL (adjust based on your configuration)
            //c.OAuth2RedirectUrl(url: $"{app.Configuration[key: "BaseUrl"] ?? "https://localhost"}/swagger/oauth2-redirect.html");
            
            // For Facebook, if you need client secret (not recommended for public clients)
            // c.OAuthClientSecret(facebookOptions.ClientSecret);
        });

        Log.Information(messageTemplate: "Use: Swagger with UI and OAuth2 configuration.");
        return app;
    }

    private static OpenApiSecurityScheme CreateJwtSecurityScheme()
    {
        return new OpenApiSecurityScheme
        {
            Name = "JWT Authentication",
            Description = "Enter your JWT token in this field",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT"
        };
    }

    private static OpenApiSecurityScheme CreateGoogleOAuth2Scheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Google OAuth2 Authentication",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(uriString: "https://accounts.google.com/o/oauth2/v2/auth"),
                    TokenUrl = new Uri(uriString: "https://oauth2.googleapis.com/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "User profile information" },
                        { "email", "User email address" }
                    }
                }
            }
        };
    }

    private static OpenApiSecurityScheme CreateFacebookOAuth2Scheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Facebook OAuth2 Authentication",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(uriString: "https://www.facebook.com/v18.0/dialog/oauth"),
                    TokenUrl = new Uri(uriString: "https://graph.facebook.com/v18.0/oauth/access_token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "email", "User email address" },
                        { "public_profile", "User public profile information" }
                    }
                }
            }
        };
    }

    private static OpenApiSecurityRequirement CreateJwtSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
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
                new List<string>()
            }
        };
    }

    private static OpenApiSecurityRequirement CreateGoogleSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Google"
                    }
                },
                new List<string> { "openid", "profile", "email" }
            }
        };
    }

    private static OpenApiSecurityRequirement CreateFacebookSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Facebook"
                    }
                },
                new List<string> { "email", "public_profile" }
            }
        };
    }
    public sealed class AuthEndpointOrderDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Define the desired order of tags, with "Authentication Management" and "Account" first
            var tagOrder = new List<string> { "Authentication", "Account" };

            // Reorder tags in the document
            var orderedTags = swaggerDoc.Tags
                ?.OrderBy(keySelector: tag =>
                {
                    int index = tagOrder.IndexOf(item: tag.Name);
                    return index == -1 ? int.MaxValue : index; // Place unlisted tags at the end
                })
                .ThenBy(keySelector: tag => tag.Name) // Alphabetical sort for tags with the same priority or unlisted tags
                .ToList()  ?? [];

            swaggerDoc.Tags.AddRange(source: orderedTags);
        }
    }
    public sealed class SnakeCaseSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null) return;

            List<KeyValuePair<string, OpenApiSchema>> propertiesToUpdate = schema.Properties.ToList();
            schema.Properties.Clear();

            foreach ((string key, OpenApiSchema value) in propertiesToUpdate)
            {
                string snakeCaseKey = JsonNamingPolicy.SnakeCaseLower.ConvertName(name: key);
                schema.Properties[key: snakeCaseKey] = value;
            }
        }
    }

    public sealed class SnakeCaseParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.In == ParameterLocation.Query)
            {
                parameter.Name = JsonNamingPolicy.SnakeCaseLower.ConvertName(name: parameter.Name);
            }
        }
    }
}

public static class SchemaIdStrategy
{
    public static string GenerateSchemaId(Type type)
    {
        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArguments = type.GetGenericArguments()
                .Select(GenerateSchemaId) // Recursively simplify generic argument names
                .ToArray();

            return $"{genericTypeName}Of{string.Join("", genericArguments)}";
        }

        // Handle nested types by combining parent names
        var nameParts = new List<string>();
        Type? currentType = type; // Use nullable Type
        while (currentType != null && currentType != typeof(object))
        {
            string part = currentType.Name;
            // Remove generic type indicators (`1, `2, etc.) from the name part
            part = Regex.Replace(part, @"`\d+", string.Empty);
            // Replace '+' (used for nested types in FullName) with empty string to concatenate smoothly
            part = part.Replace("+", "");

            if (!string.IsNullOrEmpty(part)) // Only add non-empty parts
            {
                nameParts.Insert(0, part); // Insert at the beginning to maintain order from outermost to innermost
            }
            currentType = currentType.DeclaringType;
        }

        // Filter out empty parts and join them
        string result = string.Join("", nameParts.Where(p => !string.IsNullOrEmpty(p)));
        
        // Fallback if the generated result is empty
        if (string.IsNullOrEmpty(result))
        {
            // Try to use type.FullName as a fallback, being null-safe
            string? fullName = type.FullName;
            if (fullName != null)
            {
                return fullName.Replace(".", "_").Replace("+", "_").Replace("`", "_").Replace("[", "").Replace("]", "");
            }
            else
            {
                // Ultimate fallback if FullName is also null
                return "UnknownSchema";
            }
        }
        
        return result;
    }
}