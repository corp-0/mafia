using Mafia.Core.Opinions;

namespace Mafia.Core.Content.Registries;

public interface IOpinionRuleRepository
{
    void Register(OpinionRuleDefinition opinionRule);
    IReadOnlyList<OpinionRuleDefinition> GetAll();
    OpinionRuleDefinition? GetById(string id);
}