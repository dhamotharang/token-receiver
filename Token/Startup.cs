using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using HappyTravel.ErrorHandling.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Token.Infrastructure;
using Token.Services;

namespace Token
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory;
        }


        public IConfiguration Configuration { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProblemDetailsFactory();
            services.AddControllers();
            services.Configure<BaseUrlOptions>(o =>
            {
                o.Api = Configuration["BaseUrls:Api"];
                o.Application = Configuration["BaseUrls:Application"];
            });
            services.AddSingleton(provider =>
            {
                var options = provider.GetService<IOptions<BaseUrlOptions>>();

                var factory = new PageFactory(options);
                factory.Init().GetAwaiter().GetResult();

                return factory;
            });

            services
                .AddMemoryCache()
                .AddMemoryFlow()
                .AddHealthChecks()
                .AddCheck<ControllerResolveHealthCheck>(nameof(ControllerResolveHealthCheck));

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1.0", new OpenApiInfo { Title = "HappyTravel.com Dev get token API", Version = "v1.0" });
            });
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var logger = _loggerFactory.CreateLogger<Startup>();
            app.UseProblemDetailsExceptionHandler(env, logger);

            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "HappyTravel.com Dev get token API");
                options.RoutePrefix = string.Empty;
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }


        private readonly ILoggerFactory _loggerFactory;
    }
}