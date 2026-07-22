using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Components.Tests;

public class EventFilteringTest
{
	[Fact]
	public async Task PredicateAsync_EmptyEntries_ReturnsTrue()
	{
		var filtering = new EventFiltering();
		var context = CreateContext("ModuleA", "Target.Created");

		Assert.True(await filtering.PredicateAsync(context));
	}

	[Theory]
	[InlineData("*", "Target.Created", "ModuleA", "Target.Created")]
	[InlineData("ModuleA", "*", "ModuleA", "Target.Created")]
	[InlineData("*", "*", "ModuleA", "Target.Created")]
	[InlineData("modulea", "target.created", "ModuleA", "Target.Created")]
	public async Task PredicateAsync_ExclusiveEntryMatchesPattern_ReturnsFalse(string registryPattern, string eventPattern, string registryName, string eventName)
	{
		var filtering = new EventFiltering
		{
			new(EventFiltering.EntryKind.Exclusive, registryPattern, eventPattern),
		};

		var context = CreateContext(registryName, eventName);

		Assert.False(await filtering.PredicateAsync(context));
	}

	[Theory]
	[InlineData("ModuleB", "Target.Created")]
	[InlineData("ModuleA", "Target.Removed")]
	public async Task PredicateAsync_ExclusiveEntryDoesNotMatch_ReturnsTrue(string registryName, string eventName)
	{
		var filtering = new EventFiltering
		{
			new(EventFiltering.EntryKind.Exclusive, "ModuleA", "Target.Created"),
		};

		var context = CreateContext(registryName, eventName);

		Assert.True(await filtering.PredicateAsync(context));
	}

	[Fact]
	public async Task PredicateAsync_FirstMatchingEntryDeterminesResult()
	{
		var filtering = new EventFiltering
		{
			new(EventFiltering.EntryKind.Inclusive, "ModuleA", "Target.Created"),
			new(EventFiltering.EntryKind.Exclusive, "*", "*"),
		};

		var context = CreateContext("ModuleA", "Target.Created");

		Assert.True(await filtering.PredicateAsync(context));
	}

	[Fact]
	public async Task PredicateAsync_ParsedExclamationPrefixEntryMatchesPattern_ReturnsFalse()
	{
		var filtering = new EventFiltering
		{
			EventFiltering.Entry.Parse("!ModuleA.*"),
		};

		var context = CreateContext("ModuleA", "Target.Created");

		Assert.False(await filtering.PredicateAsync(context));
	}

	[Fact]
	public void Entry_ParseRecognizesKindAndWildcard()
	{
		var entry = EventFiltering.Entry.Parse("!ModuleA.*");

		Assert.Equal(EventFiltering.EntryKind.Exclusive, entry.Kind);
		Assert.Equal("ModuleA", entry.RegistryName);
		Assert.Null(entry.EventName);
		Assert.Equal("!ModuleA.*", entry.ToString());

		Assert.True(EventFiltering.Entry.TryParse("*", out entry));
		Assert.Equal(EventFiltering.EntryKind.Inclusive, entry.Kind);
		Assert.Null(entry.RegistryName);
		Assert.Null(entry.EventName);
		Assert.Equal("*", entry.ToString());
	}

	[Theory]
	[InlineData("!", null, null)]
	[InlineData("!ModuleA.*", "ModuleA", null)]
	public void Entry_ParseExclamationPrefixCreatesExclusiveEntry(string text, string expectedRegistryName, string expectedEventName)
	{
		Assert.True(EventFiltering.Entry.TryParse(text, out var entry));
		Assert.Equal(EventFiltering.EntryKind.Exclusive, entry.Kind);
		Assert.Equal(expectedRegistryName, entry.RegistryName);
		Assert.Equal(expectedEventName, entry.EventName);
	}

	[Theory]
	[InlineData(EventFiltering.EntryKind.Inclusive, null, null, "*")]
	[InlineData(EventFiltering.EntryKind.Inclusive, "", "", "*")]
	[InlineData(EventFiltering.EntryKind.Inclusive, "*", "*", "*")]
	[InlineData(EventFiltering.EntryKind.Inclusive, "ModuleA", null, "ModuleA.*")]
	[InlineData(EventFiltering.EntryKind.Inclusive, null, "Target.Created", "*.Target.Created")]
	[InlineData(EventFiltering.EntryKind.Exclusive, null, null, "!")]
	[InlineData(EventFiltering.EntryKind.Exclusive, "*", "*", "!")]
	[InlineData(EventFiltering.EntryKind.Exclusive, "ModuleA", "*", "!ModuleA.*")]
	[InlineData(EventFiltering.EntryKind.Exclusive, "ModuleA", "Target.Created", "!ModuleA.Target.Created")]
	public void Entry_ToStringFormatsEntries(EventFiltering.EntryKind kind, string registryName, string eventName, string expected)
	{
		var entry = new EventFiltering.Entry(kind, registryName, eventName);

		Assert.Equal(expected, entry.ToString());
	}

	private static EventContext CreateContext(string registryName, string eventName) => new(new TestEventRegistry(registryName), eventName);

	private sealed class TestEventRegistry(string name) : EventRegistryBase(name);
}
