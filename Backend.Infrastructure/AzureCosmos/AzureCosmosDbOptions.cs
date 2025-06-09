namespace Backend.Infrastructure.AzureCosmos
{
    public class AzureCosmosDbOptions
    {
        public const string CosmosDb = "CosmosDb";

        public required string Endpoint { get; set; }
        public required string Key { get; set; }
        public required string DatabaseName { get; set; }
        public required string ContainerName { get; set; }
    }
}
