using Mafia.Core.Opinions;

namespace Mafia.Core.Content.Registries;

public class OpinionRuleRepository: IOpinionRuleRepository
{
    private readonly Dictionary<string, OpinionRuleDefinition> _byId = new();
    private readonly List<OpinionRuleDefinition> _allOpinionRules = [];
    
    public void Register(OpinionRuleDefinition definition)
    {
        if (_byId.TryGetValue(definition.Id, out OpinionRuleDefinition? existing))
            _allOpinionRules.Remove(existing);
        
        _byId[definition.Id] =  definition;
        _allOpinionRules.Add(definition);
    }

    public IReadOnlyList<OpinionRuleDefinition> GetAll() => _allOpinionRules;
    
    public OpinionRuleDefinition? GetById(string id)
        => _byId.GetValueOrDefault(id);
}