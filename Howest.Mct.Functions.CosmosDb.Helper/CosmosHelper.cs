using System.Linq.Expressions;
using Microsoft.Azure.Cosmos;

namespace Howest.Mct.Functions.CosmosDb.Helper;

public static class CosmosHelper
{
    private const string ConnectionString = "CosmosConnectionString";
    private const string DatabaseString = "CosmosDatabase";
    private const string ContainerString = "CosmosContainer";
    
    private const string SelectAll = "SELECT * FROM c";

    private static string? _cosmosConnectionString;
    private static string CosmosConnectionString => _cosmosConnectionString ??= Environment.GetEnvironmentVariable(ConnectionString)
                                                    ?? throw new ArgumentNullException($"{ConnectionString} is a required value in local.settings!");

    private static string? _database;
    private static string Database => _database ??= Environment.GetEnvironmentVariable(DatabaseString)
                                                    ?? throw new ArgumentNullException($"{DatabaseString} is a required value in local.settings!");

    private static string? _container;
    private static string Container => _container ??= Environment.GetEnvironmentVariable(ContainerString)
                                                      ?? throw new ArgumentNullException($"{ContainerString} is a required value in local.settings!");

    public static CosmosClient GetCosmosClient()
    {
        var options = new CosmosClientOptions()
        {
            ConnectionMode = ConnectionMode.Gateway
        };

        return new CosmosClient(CosmosConnectionString, options);
    }

    public static Container GetContainer(string container)
    {
        return GetContainer(null, null, container);
    }
    
    public static Container GetContainer(CosmosClient? client = null, string? database = null, string? container = null)
    {
        return (client ?? GetCosmosClient()).GetContainer(database ?? Database, container ?? Container);
    }

    public static IAsyncEnumerable<T> GetItems<T>(this Container container)
        where T : class
    {
        return GetItems<T>(container, SelectAll);
    }

    public static async Task<IList<T>> GetItems<T>(this Container container, Expression<Func<T, bool>> predicate)
        where T : class
    {
        return await container.GetItems<T>($"{SelectAll} WHERE {ExpressionHelper.GetCosmosDbPredicate(predicate)}").ToListAsync();
    }

    public static async IAsyncEnumerable<T> GetItems<T>(this Container container, string query)
        where T: class
    {
        var iterator = container.GetItemQueryIterator<T>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }

    public static async Task<T?> GetItem<T>(this Container container, Expression<Func<T, bool>> predicate)
        where T : class, new()
    {
        return await GetItem<T>(container, $"{SelectAll} WHERE {ExpressionHelper.GetCosmosDbPredicate(predicate)}");
    }

    public static async Task<T?> GetItem<T>(Container container, string query)
        where T: class, new()
    {
        var iterator = container.GetItemQueryIterator<T>(query);

        var item = new T();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            if (response.Count > 1)
                throw new InvalidDataException();

            item = response.FirstOrDefault();
        }
        return item;
    }
}