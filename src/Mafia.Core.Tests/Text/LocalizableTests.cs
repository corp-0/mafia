using FluentAssertions;
using Mafia.Core.Text;
using Xunit;

namespace Mafia.Core.Tests.Text;

public class LocalizableTests
{
    [Fact]
    public void Format_SimpleArgSubstitution()
    {
        var loc = new Localizable("greeting", new Dictionary<string, object?>
        {
            ["name"] = "Alice"
        });

        var result = loc.Format("Hello {name}");

        result.Should().Be("Hello Alice");
    }

    [Fact]
    public void Format_GenderSelect()
    {
        var loc = new Localizable("betrayal", new Dictionary<string, object?>
        {
            ["gender"] = "male"
        });

        var result = loc.Format("{gender, select, male {He} female {She} other {They}} left");

        result.Should().Be("He left");
    }

    [Fact]
    public void Format_Plural()
    {
        var loc = new Localizable("items", new Dictionary<string, object?>
        {
            ["count"] = 5
        });

        var result = loc.Format("{count, plural, one {# item} other {# items}}");

        result.Should().Be("5 items");
    }

    [Fact]
    public void Format_CombinedGenderAndArgs()
    {
        var loc = new Localizable("theft", new Dictionary<string, object?>
        {
            ["gender"] = "female",
            ["amount"] = "500"
        });

        var result = loc.Format("{gender, select, male {He} female {She} other {They}} stole {amount}");

        result.Should().Be("She stole 500");
    }

    [Fact]
    public void Format_PlainTextNoArgs()
    {
        var loc = new Localizable("plain", new Dictionary<string, object?>());

        var result = loc.Format("Plain text");

        result.Should().Be("Plain text");
    }
}
