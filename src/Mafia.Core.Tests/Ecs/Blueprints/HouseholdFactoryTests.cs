using fennecs;
using FluentAssertions;
using Mafia.Core.Ecs.Blueprints;
using Mafia.Core.Ecs.Components.Identity;
using Mafia.Core.Ecs.Relations;
using Xunit;

namespace Mafia.Core.Tests.Ecs.Blueprints;

public class HouseholdFactoryTests : IDisposable
{
    private readonly World _world = new();
    private readonly CharacterFactory _characterFactory;
    private readonly HouseholdFactory _sut = new();

    public HouseholdFactoryTests()
    {
        _characterFactory = new CharacterFactory(_world);
    }

    public void Dispose() => _world.Dispose();

    private Entity SpawnMale(string name, int age = 30)
        => _characterFactory.Spawn(new CharacterBlueprint { Name = name, Age = age, Sex = Sex.Male });

    private Entity SpawnFemale(string name, int age = 30)
        => _characterFactory.Spawn(new CharacterBlueprint { Name = name, Age = age, Sex = Sex.Female });

    [Fact]
    public void Apply_Marriage_WiresSpouseOfRelations()
    {
        var vito = SpawnMale("Vito", 55);
        var carmela = SpawnFemale("Carmela", 50);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = vito,
            ["carmela"] = carmela
        };

        HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "carmela"],
            Marriages = [new Marriage(Spouse1Id: "vito", Spouse2Id: "carmela")]
        }, roster);

        vito.Has<SpouseOf>(carmela).Should().BeTrue();
        carmela.Has<SpouseOf>(vito).Should().BeTrue();
    }

    [Fact]
    public void Apply_Parentage_WiresParentOfRelations()
    {
        var father = SpawnMale("Vito", 55);
        var mother = SpawnFemale("Carmela", 50);
        var son = SpawnMale("Michael", 25);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = father,
            ["carmela"] = mother,
            ["michael"] = son
        };

        HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "carmela", "michael"],
            Parentages = [new Parentage(Parent1Id: "vito", Parent2Id: "carmela", ["michael"])]
        }, roster);

        father.Has<ParentOf>(son).Should().BeTrue();
        mother.Has<ParentOf>(son).Should().BeTrue();
    }

    [Fact]
    public void Apply_Parentage_MultipleChildren_AllHaveParentOfRelations()
    {
        var father = SpawnMale("Vito", 55);
        var mother = SpawnFemale("Carmela", 50);
        var michael = SpawnMale("Michael", 25);
        var connie = SpawnFemale("Connie", 20);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = father,
            ["carmela"] = mother,
            ["michael"] = michael,
            ["connie"] = connie
        };

        HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "carmela", "michael", "connie"],
            Parentages = [new Parentage(Parent1Id: "vito", Parent2Id: "carmela", ["michael", "connie"])]
        }, roster);

        father.Has<ParentOf>(michael).Should().BeTrue();
        father.Has<ParentOf>(connie).Should().BeTrue();
        mother.Has<ParentOf>(michael).Should().BeTrue();
        mother.Has<ParentOf>(connie).Should().BeTrue();
    }

    [Fact]
    public void Apply_FullHousehold_WiresCanonicalRelations()
    {
        var vito = SpawnMale("Vito", 55);
        var carmela = SpawnFemale("Carmela", 50);
        var michael = SpawnMale("Michael", 25);
        var sonny = SpawnMale("Sonny", 30);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = vito,
            ["carmela"] = carmela,
            ["michael"] = michael,
            ["sonny"] = sonny
        };

        HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "carmela", "michael", "sonny"],
            Marriages = [new Marriage(Spouse1Id: "vito", Spouse2Id: "carmela")],
            Parentages = [new Parentage(Parent1Id: "vito", Parent2Id: "carmela", ["michael", "sonny"])]
        }, roster);

        // Marriage - canonical SpouseOf
        vito.Has<SpouseOf>(carmela).Should().BeTrue();
        carmela.Has<SpouseOf>(vito).Should().BeTrue();

        // Parentage - canonical ParentOf
        vito.Has<ParentOf>(michael).Should().BeTrue();
        vito.Has<ParentOf>(sonny).Should().BeTrue();
        carmela.Has<ParentOf>(michael).Should().BeTrue();
        carmela.Has<ParentOf>(sonny).Should().BeTrue();
    }

    [Fact]
    public void Apply_MissingId_ThrowsKeyNotFoundException()
    {
        var vito = SpawnMale("Vito", 55);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = vito
        };

        var act = () => HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "ghost"],
            Marriages = []
        }, roster);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*ghost*");
    }

    [Fact]
    public void Apply_MissingMarriageId_ThrowsKeyNotFoundException()
    {
        var vito = SpawnMale("Vito", 55);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = vito
        };

        var act = () => HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito"],
            Marriages = [new Marriage(Spouse1Id: "vito", Spouse2Id: "ghost")]
        }, roster);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*ghost*");
    }

    [Fact]
    public void Apply_MissingParentageChildId_ThrowsKeyNotFoundException()
    {
        var father = SpawnMale("Vito", 55);
        var mother = SpawnFemale("Carmela", 50);
        var roster = new Dictionary<string, Entity>
        {
            ["vito"] = father,
            ["carmela"] = mother
        };

        var act = () => HouseholdFactory.Apply(new HouseholdBlueprint
        {
            Id = "corleone",
            MemberIds = ["vito", "carmela"],
            Parentages = [new Parentage(Parent1Id: "vito", Parent2Id: "carmela", ["ghost"])]
        }, roster);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*ghost*");
    }
}
