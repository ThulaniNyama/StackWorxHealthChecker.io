# Read Me

## Getting Started

Once you have started the server via Visual Studio you can access the graphql query window with [https://localhost:5001/ui/playground](https://localhost:5001/ui/playground)

Alternatively you can start it via the command line

```bash
dotnet run --project HealthChecker
```

Try this basic query to confirm everything is working

```
{
  hello
}
```

It should respond with

```
{
  "data": {
    "hello": "world"
  }
}
```

Another sample query:

```
{
  servers {
    id
    name
    healthCheckUri
    status
  }
}
```

## Technologies Used

- [GraphQL](https://graphql.org/)
- [GraphQL .NET](https://graphql-dotnet.github.io/)
- [EF Core](https://docs.microsoft.com/en-us/ef/core/get-started/?tabs=netcore-cli#install-entity-framework-core)

## Learning Resources

- [GraphQL Learn](https://graphql.org/learn/)
