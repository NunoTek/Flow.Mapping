using Flow.Mapping.Models;

namespace Flow.Mapping.Abstractions;

public interface IFlowValueResolver
{
    public object Resolve(ResolverContext context, IFlowInternalMapper mapper);
}
