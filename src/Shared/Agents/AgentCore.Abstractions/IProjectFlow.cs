namespace AgentCore.Abstractions;

/// <summary>
/// Product-specific conversation / tool flow; implemented in <c>ProductX.ProjectFlows</c>.
/// </summary>
public interface IProjectFlow
{
    Task<AgentRunResult> RunAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
