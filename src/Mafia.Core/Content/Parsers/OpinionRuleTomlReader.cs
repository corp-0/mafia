using Mafia.Core.Content.Factories;
using Mafia.Core.Content.Parsers.Dtos;
using Mafia.Core.Opinions;
using Tomlyn;

namespace Mafia.Core.Content.Parsers;

public static class OpinionRuleTomlReader
{
    public static OpinionRuleDto Deserialize(string toml)
        => Toml.ToModel<OpinionRuleDto>(toml, options: TomlOptions.Default);
    
    public static OpinionRuleDefinition Read(string toml)
        => OpinionRuleFactory.Create(Deserialize(toml));
}