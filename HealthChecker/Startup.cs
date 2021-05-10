using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using GraphQL.Server;
using HealthChecker.GraphQL;
using GraphQL.Types;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Server.Ui.Playground;
using Microsoft.EntityFrameworkCore;
using GraphQL.Utilities;
using Microsoft.Extensions.Options;

namespace HealthChecker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("Id", () =>
                    HealthCheckResult.Healthy("Id"), tags: new[] { "Id_tag" })
                .AddCheck("name",  () =>
                    HealthCheckResult.Degraded("name"), tags: new[] { "name_tag" })
                .AddCheck("ping", () =>
                {
                    try
                    {
                        using (var ping = new Ping())
                        {
                            var reply = ping.Send("localhost");
                            if (reply.Status != IPStatus.Success)
                            {
                                return HealthCheckResult.Unhealthy();
                            }

                            if (reply.RoundtripTime > 499999)
                            {
                                return HealthCheckResult.Degraded();
                            }

                            return HealthCheckResult.Healthy();
                        }
                    }
                    catch
                    {
                        return HealthCheckResult.Unhealthy();
                    }
                })
                .AddCheck<HealthCheck>("Checker", null, new[] { "Checker" });

            services.AddControllersWithViews();

            services.AddRazorPages();

            services.AddSingleton<ServerType>();

            services.AddSingleton<HealthCheckerSchema>();

            services.AddGraphQL(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
                //var logger = provider.GetRequiredService<ILogger<Startup>>();
                //options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occured", ctx.OriginalException.Message);
            })//.AddSystemTextJson(deserializerSettings => { }, serializerSettings => { })
              //.AddNewtonsoftJson(deserializerSettings => { }, serializerSettings => { })
              .AddGraphTypes(typeof(Schema));

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseGraphQL<HealthCheckerSchema, GraphQLHttpMiddlewareWithLogs<HealthCheckerSchema>>("/graphql");
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions
            {
                Path = "/ui/playground"
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHealthChecks("/health");

                endpoints.MapGraphQL("/graphql");
            });
        }
    }

    public class GraphQLHttpMiddlewareWithLogs<TSchema> : GraphQLHttpMiddleware<TSchema>
        where TSchema : ISchema
    {
        private readonly ILogger _logger;

        public GraphQLHttpMiddlewareWithLogs(
            ILogger<GraphQLHttpMiddleware<TSchema>> logger,
            RequestDelegate next,
            PathString path,
            IGraphQLRequestDeserializer requestDeserializer)
            : base(next, path, requestDeserializer)
        {
            _logger = logger;
        }

        protected override Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            if (requestExecutionResult.Result.Errors != null)
            {
                if (requestExecutionResult.IndexInBatch.HasValue)
                    _logger.LogError("GraphQL execution completed in {Elapsed} with error(s) in batch [{Index}]: {Errors}", requestExecutionResult.Elapsed, requestExecutionResult.IndexInBatch, requestExecutionResult.Result.Errors);
                else
                    _logger.LogError("GraphQL execution completed in {Elapsed} with error(s): {Errors}", requestExecutionResult.Elapsed, requestExecutionResult.Result.Errors);
            }
            else
                _logger.LogInformation("GraphQL execution successfully completed in {Elapsed}", requestExecutionResult.Elapsed);

            return base.RequestExecutedAsync(requestExecutionResult);
        }

        protected override CancellationToken GetCancellationToken(HttpContext context)
        {
            // custom CancellationToken
            var cts = CancellationTokenSource.CreateLinkedTokenSource(base.GetCancellationToken(context), new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);
            return cts.Token;
        }
    }
    public class HealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken = default(CancellationToken))
        {
            var healthCheckResultHealthy = true;

            if (healthCheckResultHealthy)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("A healthy result."));
            }
            return Task.FromResult(
                HealthCheckResult.Unhealthy("An unhealthy result."));
        }
    }
}
