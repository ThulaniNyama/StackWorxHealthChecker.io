using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecker.GraphQL
{

    public class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string HealthCheckUri { get; set; }
    }

    public class ServerType : ObjectGraphType<Server>
    {
        public ServerType()
        {
            Name = "Server";
            Description = "A server to monitor";

            Field(h => h.Id);
            Field(h => h.Name);
            Field(h => h.HealthCheckUri);

            Field<StringGraphType>(
                "status",
                resolve: context => HealthStatus.Healthy.ToString()
            );
        }
    }

    public class HealthCheckerQuery : ObjectGraphType<object>
    {
        private List<Server> servers = new List<Server>{
            new Server{
                Id = "1",
                Name = "stackworx.io",
                HealthCheckUri = "https://www.stackworx.io",
            },
            new Server{
                Id = "2",
                Name = "prima.run",
                HealthCheckUri = "https://prima.run",
            },
            new Server{
                Id = "3",
                Name = "google",
                HealthCheckUri = "https://www.google.com",
            },
        };

        public HealthCheckerQuery()
        {
            Name = "Query";

            Func<ResolveFieldContext, string, object> serverResolver = (context, id) => this.servers;

            FieldDelegate<ListGraphType<ServerType>>(
                "servers",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "id", Description = "id of server" }
                ),
                resolve: serverResolver
            );
            Field<StringGraphType>(
                "name",
                resolve: context => Name
            );
            Field<StringGraphType>(
                "status",
                resolve: context => HealthStatus.Healthy.ToString()
            );
        }
    }

    public class HealthCheckerSchema : Schema, ISchema
    {
        public HealthCheckerSchema(IServiceProvider provider) : base(provider)
        {
            {
                Query = provider.GetRequiredService<HealthCheckerQuery>();
                //Query = new HealthCheckerQuery();
            }
        }
    }
}
