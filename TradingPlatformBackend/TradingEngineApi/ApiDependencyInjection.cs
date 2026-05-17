using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TradingEngine.Api.Common;
using TradingEngine.Api.Hubs;
using TradingEngine.Api.Services;
using TradingEngine.Application.Options;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngineApi.Validators;

namespace TradingEngine.Api
{
    public static class ApiDependencyInjection
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var jwtOptions = configuration.GetSection(JwtSettings.Section).Get<JwtSettings>();

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            services.AddSignalR();
            services.AddSingleton<IMarketDataNotifier, SignalRMarketDataNotifier>();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<PlaceOrderCommandValidator>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trading Engine API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                    };
                });

            services.AddAuthorization();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyNames.Frontend, policy =>
                {
                    var allowedOrigins = configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>()
                        ?? (environment.IsDevelopment()
                            ? ["http://localhost:3000", "http://localhost:4200", "http://localhost:5173"]
                            : Array.Empty<string>());

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            services.AddHttpContextAccessor();
            services.AddScoped<Application.Interfaces.IUserResolverService, UserResolverService>();

            return services;
        }
    }
}