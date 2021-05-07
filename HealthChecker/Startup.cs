using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using HealthChecker.GraphQL;
using GraphQL.Types;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using GraphQL.Server.Transports.AspNetCore.Common;
using System.Threading.Tasks;
using System.Threading;
using System;

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
            services.AddRazorPages();

            services.AddSingleton<ServerType>();

            services.AddSingleton<HealthCheckerSchema, HealthCheckerSchema>();

            services.AddGraphQL(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
                //var logger = provider.GetRequiredService<ILogger<Startup>>();
                //options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occured", ctx.OriginalException.Message);
            }).AddSystemTextJson(deserializerSettings => { }, serializerSettings => { });

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
                app.UseExceptionHandler("/Error");
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

            // app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
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
            // custom CancellationToken example 
            var cts = CancellationTokenSource.CreateLinkedTokenSource(base.GetCancellationToken(context), new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            return cts.Token;
        }
    }
}
