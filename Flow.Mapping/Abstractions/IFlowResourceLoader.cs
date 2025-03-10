using Flow.Mapping.Models;

namespace Flow.Mapping.Abstractions;

public interface IFlowResourceLoader
{
    public Task <List<Type>> ListMappableClassTypesAsync(int fluxId, CancellationToken cancelToken);

    public Task<List<(string EntityName, string PropertyName)>> ListTranscodableAsync(int fluxId, CancellationToken cancelToken);



    public Task<List<FlowMapping>> LoadMappingsAsync(int fluxId, CancellationToken cancelToken);

    public Task<List<Transcodification>> LoadTranscodificationsAsync(int fluxId, CancellationToken cancelToken);

    public Task<Dictionary<ValueResolverTypes, IFlowValueResolver>> LoadResolversAsync(int fluxId, CancellationToken cancelToken);



    Task InsertTranscodificationAsync(int fluxId, List<Transcodification> transcodifications, CancellationToken cancelToken);
}
