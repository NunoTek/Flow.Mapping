using Flow.Mapping.Abstractions;
using Flow.Mapping.Models;

namespace Flow.Mapping;

public class FlowTranscodifier
{
    private readonly IFlowResourceLoader _loader;

    private List<(string EntityName, string PropertyName)> _transcodableMappables;
    private List<Transcodification> _transcodifications;
    private List<Transcodification> _requireTranscodifications;

    public FlowTranscodifier(IFlowResourceLoader loader)
    {
        _loader = loader;
    }

    public async Task InitializeAsync(int fluxId, CancellationToken cancelToken)
    {
        _transcodableMappables = await _loader.ListTranscodableAsync(fluxId, cancelToken);
        _transcodifications = await _loader.LoadTranscodificationsAsync(fluxId, cancelToken);
        _requireTranscodifications = new List<Transcodification>();
    }

    public bool GetTranscodedValue(string entityName, string propertyName, string value, out string newValue)
    {
        newValue = null;

        var transcos = _transcodifications
            .Where(t => t.EntiteDestination == entityName && t.ChampDestination == propertyName)
            .Where(t => t.ValeurSource?.ToLower() == value?.ToLower())
            ;

        if (!transcos.Any())
        {
            RequestTranscodification(entityName, propertyName, value);
            return false;
        }

        newValue = transcos.Select(t => t.ValeurCible).FirstOrDefault();

        return true;
    }

    public void RequestTranscodification(string entityName, string propertyName, string value, string cible = null)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (!_transcodableMappables.Any(x => x.EntityName == entityName && x.PropertyName == propertyName))
            return;

        _requireTranscodifications.Add(new Transcodification()
        {
            EntiteDestination = entityName,
            ChampDestination = propertyName,
            ValeurSource = value,
            ValeurCible = cible
        });
    }

    public async Task ReportFoundTranscodificationsAsync(int fluxId, CancellationToken cancelToken)
    {
        if (!_requireTranscodifications.Any())
            return;

        var results = _requireTranscodifications
            // we delete useless cases
            .Where(x => !string.IsNullOrEmpty(x.ValeurSource))
            // we order by value found
            .OrderByDescending(x => x.ValeurCible)
            // DistinctBy => we only need the first instance pour chaque valeurs
            .GroupBy(x => new { x.EntiteDestination, x.ChampDestination, x.ValeurSource })
            .Select(x => x.First())
            .ToList();

        // check if already exist
        results = results
            .Where(x => !_transcodifications.Any(t => t.EntiteDestination == x.EntiteDestination && t.ChampDestination == x.ChampDestination && t.ValeurSource.ToLower() == x.ValeurSource.ToLower()))
            .ToList();

        if (!results.Any())
            return;

        await _loader.InsertTranscodificationAsync(fluxId, results, cancelToken);
    }
}
