namespace Backend.Infrastructure.AzureOpenAI
{
    public class AzureOpenAIOptions
    {
        public const string AzureOpenAI = "AzureOpenAI";

        public required string Endpoint { get; set; }
        public required string ApiKey { get; set; }
        public required string DeploymentName { get; set; }
    }
}
