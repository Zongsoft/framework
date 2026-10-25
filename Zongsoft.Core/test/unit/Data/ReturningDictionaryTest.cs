using System;
using System.Data;
using System.Linq;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ReturningDictionaryTest
{
	[Fact]
	public void ConversionSeparatesKindsAndPreservesNullAndMissingFields()
	{
		var returning = DataUpdateOptions.Return(ReturningKind.Older, "Name")
			.Return(ReturningKind.Newer, "Name", "Enabled", "Description", "Missing").Build().Returning;
		using var table = new DataTable();
		table.Columns.Add("Name@Older", typeof(string));
		table.Columns.Add("Name@Newer", typeof(string));
		table.Columns.Add("Enabled@Newer", typeof(bool));
		table.Columns.Add("Description@Newer", typeof(string));
		table.Rows.Add("Before", "After", false, DBNull.Value);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		returning.Rows.Populate(reader);
		var row = Assert.Single(returning.Rows);
		var older = row.ToDictionary(ReturningKind.Older);
		var newer = row.ToDictionary<Record>(ReturningKind.Newer);

		Assert.Single(older);
		Assert.Equal("Before", older["NAME"]);
		Assert.Equal(3, newer.Count());
		Assert.Equal("After", newer.GetValue(record => record.Name));
		Assert.True(newer.TryGetValue(record => record.Enabled, out var enabled));
		Assert.False(enabled);
		Assert.True(newer.TryGetValue<string>("description", out var description));
		Assert.Null(description);
		Assert.False(newer.ContainsKey("Missing"));
		Assert.False(newer.ContainsKey(nameof(Record.Id)));
		Assert.False(newer.ContainsKey("Name@Newer"));
		Assert.True(newer.HasChanges("name"));

		var model = newer.AsModel();
		Assert.Equal("After", model.Name);
		Assert.False(model.Enabled);
		Assert.False(newer.ContainsKey(nameof(Record.Id)));
	}

	[Fact]
	public void ConversionCopiesTheDictionaryWithoutChangingTheRow()
	{
		var columns = new Returning.ColumnCollection(ReturningKind.Newer, new Returning.Column("Name", ReturningKind.Newer));
		var values = new object[] { "Original" };
		var row = new Returning.Row(columns, values);
		var first = row.ToDictionary(ReturningKind.Newer);
		var second = row.ToDictionary<Record>(ReturningKind.Newer);
		first["name"] = "Changed";
		first.Add("Id", 42);
		Assert.Equal("Original", second["Name"]);
		Assert.False(second.ContainsKey("Id"));
		Assert.Equal("Original", row[0]);
		values[0] = "Replaced";
		Assert.Equal("Original", second["Name"]);
		Assert.Equal("Replaced", row.ToDictionary(ReturningKind.Newer)["Name"]);
	}

	[Fact]
	public void CollectionUsesEachRowsCapturedColumns()
	{
		var returning = DataUpdateOptions.Return(ReturningKind.Newer, "Name").Build().Returning;
		using var table = new DataTable();
		table.Columns.Add("Name", typeof(string));
		table.Columns.Add("Enabled", typeof(bool));
		table.Rows.Add("First", true);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		returning.Rows.Populate(reader);
		returning.Columns.Clear();
		returning.Columns.Append("Enabled", ReturningKind.Newer);
		returning.Rows.Populate(reader);

		var source = DataUpdateOptions.Return(ReturningKind.Newer, "Name", "Enabled").Build().Returning;
		source.Rows.Populate(reader);
		returning.Rows.AddRange(source.Rows);
		returning.Columns.Clear();

		var dictionaries = returning.Rows.ToDictionaries<Record>(ReturningKind.Newer).ToArray();
		Assert.Equal(3, dictionaries.Length);
		Assert.Single(dictionaries[0]);
		Assert.Equal("First", dictionaries[0]["Name"]);
		Assert.Single(dictionaries[1]);
		Assert.Equal(true, dictionaries[1]["Enabled"]);
		Assert.Equal(2, dictionaries[2].Count());
		Assert.Equal("First", dictionaries[2]["Name"]);
		Assert.Equal(true, dictionaries[2]["Enabled"]);
		Assert.Equal(dictionaries.Select(dictionary => dictionary.Count()), returning.Rows.ToDictionaries(ReturningKind.Newer).Select(dictionary => dictionary.Count()));
	}

	[Fact]
	public void CollectionConvertsOnEnumerationAndDoesNotCacheMutableDictionaries()
	{
		var returning = DataDeleteOptions.Return("Name").Build().Returning;
		var dictionaries = returning.Rows.ToDictionaries(ReturningKind.Older);
		var typed = returning.Rows.ToDictionaries<Record>(ReturningKind.Older);
		Assert.Empty(dictionaries);
		Assert.Empty(typed);
		using var table = new DataTable();
		table.Columns.Add("Name", typeof(string));
		table.Rows.Add("First");
		table.Rows.Add("Second");
		using var reader = table.CreateDataReader();

		while(reader.Read())
			returning.Rows.Populate(reader);

		Assert.Equal(new[] { "First", "Second" }, dictionaries.Select(dictionary => dictionary.GetValue<string>("Name", null)));
		var first = typed.ToArray();
		first[0]["Name"] = "Changed";
		Assert.Equal("First", typed.First()["Name"]);
		Assert.Equal("Second", first[1]["Name"]);
		Assert.Equal(2, returning.Rows.Count);
	}

	[Fact]
	public void ConversionRetainsRowsWithoutAvailableFields()
	{
		var returning = DataUpdateOptions.Return(ReturningKind.Older, "Name").Build().Returning;
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		table.Rows.Add(1);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		returning.Rows.Populate(reader);

		Assert.Empty(Assert.Single(returning.Rows.ToDictionaries(ReturningKind.Older)));
		Assert.Empty(Assert.Single(returning.Rows.ToDictionaries<Record>(ReturningKind.Newer)));
		Assert.Empty(default(Returning.Row).ToDictionary(ReturningKind.Newer));
		Assert.Empty(default(Returning.Row).ToDictionary<Record>(ReturningKind.Older));
	}

	[Theory]
	[InlineData(ReturningKind.Newer, ReturningKind.Older)]
	[InlineData(ReturningKind.Older, ReturningKind.Newer)]
	public void ConversionRejectsConflictingFixedKinds(ReturningKind kind, ReturningKind requested)
	{
		var columns = new Returning.ColumnCollection(kind, new Returning.Column("Name", kind));
		var row = new Returning.Row(columns, "Name");
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => row.ToDictionary(requested)).ParamName);
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => row.ToDictionary<Record>(requested)).ParamName);
	}

	[Fact]
	public void ConversionRejectsUndefinedKindsIncludingEmptyCollections()
	{
		var invalid = (ReturningKind)42;
		var rows = DataDeleteOptions.Return("Name").Build().Returning.Rows;
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => default(Returning.Row).ToDictionary(invalid)).ParamName);
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => default(Returning.Row).ToDictionary<Record>(invalid)).ParamName);
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => rows.ToDictionaries(invalid).ToArray()).ParamName);
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => rows.ToDictionaries<Record>(invalid).ToArray()).ParamName);
	}

	private sealed class Record
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool Enabled { get; set; }
	}
}
