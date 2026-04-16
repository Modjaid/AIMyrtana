using LinguaBot.Agent;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;

public static class LinguaBotAgentServiceCollectionExtensions
{
    public static IServiceCollection AddLinguaBotAgent(
        this IServiceCollection services,
        string openAiApiKey,
        string model = "gpt-4o-mini")
    {
        services.AddSingleton(_ =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(model, openAiApiKey);
            return builder.Build();
        });

        services.AddSingleton<ILanguageTutorAgent, LanguageTutorAgent>();
        return services;
    }
}
