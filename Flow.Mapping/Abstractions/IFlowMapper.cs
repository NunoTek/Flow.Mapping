using Flow.Mapping.Models;

namespace Flow.Mapping.Abstractions;

public interface IFlowMapper<TRoot>
    where TRoot : FlowRoot, new()
{
    public Task<TRoot> MapAsync(int fluxId, string fileName, CancellationToken cancelToken = default);
}
