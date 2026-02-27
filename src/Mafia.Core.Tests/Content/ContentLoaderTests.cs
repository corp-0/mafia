using FluentAssertions;
using Mafia.Core.Content;
using Mafia.Core.Content.Registries;
using Xunit;

namespace Mafia.Core.Tests.Content;

public class ContentLoaderTests
{
    private readonly EventDefinitionRepository _repository = new();
    private readonly ContentLoader _loader;

    public ContentLoaderTests()
    {
        _loader = new ContentLoader(_repository);
    }

    private const string MANIFEST_TOML = """
        id = "test_pack"
        name = "Test Pack"
        version = "1.0.0"
        load_order = 0
        """;

    private static string MakePulseToml(string id) => $"""
        id = "{id}"
        title_key = "Test"
        description_key = "A test event."
        trigger_type = "pulse"
        mean_time_to_happen_days = 30.0

        [[options]]
        id = "opt_a"
        display_text_key = "OK"
        resolution_text_key = "Done."
        """;

    // ═══════════════════════════════════════════════
    //  DiscoverPacks
    // ═══════════════════════════════════════════════

    [Fact]
    public void DiscoverPacks_FindsPacksInSubdirectories()
    {
        var root = CreateTempDir();
        try
        {
            CreatePack(root, "pack_a", MANIFEST_TOML);
            CreatePack(root, "pack_b", """
                id = "pack_b"
                name = "Pack B"
                version = "2.0.0"
                load_order = 10
                """);

            var packs = _loader.DiscoverPacks(root);

            packs.Should().HaveCount(2);
            packs.Select(p => p.Id).Should().Contain(["test_pack", "pack_b"]);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverPacks_SkipsDirectoriesWithoutManifest()
    {
        var root = CreateTempDir();
        try
        {
            CreatePack(root, "valid_pack", MANIFEST_TOML);
            Directory.CreateDirectory(Path.Combine(root, "no_manifest"));

            var packs = _loader.DiscoverPacks(root);

            packs.Should().HaveCount(1);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverPacks_EmptyDirectory_ReturnsEmpty()
    {
        var root = CreateTempDir();
        try
        {
            var packs = _loader.DiscoverPacks(root);
            packs.Should().BeEmpty();
        }
        finally { Cleanup(root); }
    }

    // ═══════════════════════════════════════════════
    //  LoadPacks
    // ═══════════════════════════════════════════════

    [Fact]
    public void LoadPacks_LoadsEventsFromPack()
    {
        var root = CreateTempDir();
        try
        {
            var packDir = CreatePack(root, "base", MANIFEST_TOML);
            var eventsDir = Path.Combine(packDir, "events");
            Directory.CreateDirectory(eventsDir);
            File.WriteAllText(Path.Combine(eventsDir, "event_a.toml"), MakePulseToml("evt_a"));
            File.WriteAllText(Path.Combine(eventsDir, "event_b.toml"), MakePulseToml("evt_b"));

            var packs = _loader.DiscoverPacks(root);
            _loader.LoadPacks(packs);

            _repository.GetById("evt_a").Should().NotBeNull();
            _repository.GetById("evt_b").Should().NotBeNull();
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void LoadPacks_ModOverridesSingleEvent_BaseEventsPreserved()
    {
        var root = CreateTempDir();
        try
        {
            // Base pack has two events
            var basePack = CreatePack(root, "base", """
                id = "base"
                name = "Base"
                load_order = 0
                """);
            Directory.CreateDirectory(Path.Combine(basePack, "events"));
            File.WriteAllText(Path.Combine(basePack, "events", "recruitment.toml"), $"""
                id = "recruitment"
                title_key = "Original Recruitment"
                description_key = "Base version."
                trigger_type = "pulse"
                mean_time_to_happen_days = 30.0

                [[options]]
                id = "opt"
                display_text_key = "OK"
                resolution_text_key = "Done."
                """);
            File.WriteAllText(Path.Combine(basePack, "events", "betrayal.toml"), MakePulseToml("betrayal"));

            // Mod pack (different ID) only overrides the recruitment event
            var modPack = CreatePack(root, "mod", """
                id = "balance_mod"
                name = "Balance Mod"
                load_order = 10
                """);
            Directory.CreateDirectory(Path.Combine(modPack, "events"));
            File.WriteAllText(Path.Combine(modPack, "events", "recruitment.toml"), $"""
                id = "recruitment"
                title_key = "Rebalanced Recruitment"
                description_key = "Mod version less punishing."
                trigger_type = "pulse"
                mean_time_to_happen_days = 60.0

                [[options]]
                id = "opt"
                display_text_key = "OK"
                resolution_text_key = "Done."
                """);

            var packs = _loader.DiscoverPacks(root);
            _loader.LoadPacks(packs);

            // The recruitment event was upserted, mod's version wins (loaded later)
            var recruitment = _repository.GetById("recruitment");
            recruitment.Should().NotBeNull();
            recruitment!.TitleKey.Should().Be("Rebalanced Recruitment");

            // The betrayal event from base is still there (not touched by mod)
            _repository.GetById("betrayal").Should().NotBeNull();
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void LoadPacks_ExcludesManifestFromEventParsing()
    {
        var root = CreateTempDir();
        try
        {
            var packDir = CreatePack(root, "base", MANIFEST_TOML);
            File.WriteAllText(Path.Combine(packDir, "event.toml"), MakePulseToml("evt_real"));

            var packs = _loader.DiscoverPacks(root);

            // Should not throw trying to parse content_pack.toml as an event
            var act = () => _loader.LoadPacks(packs);
            act.Should().NotThrow();

            _repository.GetById("evt_real").Should().NotBeNull();
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void LoadPacks_SortsByLoadOrderAscending()
    {
        var root = CreateTempDir();
        try
        {
            // Create packs with same event ID, different load orders
            var lowPack = CreatePack(root, "low", """
                id = "low"
                name = "Low"
                load_order = 0
                """);
            Directory.CreateDirectory(Path.Combine(lowPack, "events"));
            File.WriteAllText(Path.Combine(lowPack, "events", "e.toml"), $"""
                id = "shared"
                title_key = "Low"
                description_key = "From low pack."
                trigger_type = "pulse"
                mean_time_to_happen_days = 30.0

                [[options]]
                id = "opt"
                display_text_key = "OK"
                resolution_text_key = "Done."
                """);

            var highPack = CreatePack(root, "high", """
                id = "high"
                name = "High"
                load_order = 50
                """);
            Directory.CreateDirectory(Path.Combine(highPack, "events"));
            File.WriteAllText(Path.Combine(highPack, "events", "e.toml"), $"""
                id = "shared"
                title_key = "High"
                description_key = "From high pack."
                trigger_type = "pulse"
                mean_time_to_happen_days = 30.0

                [[options]]
                id = "opt"
                display_text_key = "OK"
                resolution_text_key = "Done."
                """);

            var packs = _loader.DiscoverPacks(root);
            _loader.LoadPacks(packs);

            // Higher load_order loaded last → upserts over low
            var evt = _repository.GetById("shared");
            evt.Should().NotBeNull();
            evt.Should().NotBeNull();
            evt!.TitleKey.Should().Be("High");
        }
        finally { Cleanup(root); }
    }

    // ═══════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mafia_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CreatePack(string root, string folderName, string manifestToml)
    {
        var dir = Path.Combine(root, folderName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "content_pack.toml"), manifestToml);
        return dir;
    }

    private static void Cleanup(string dir) =>
        Directory.Delete(dir, recursive: true);
}
