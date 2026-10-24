using System;
using System.Data;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ReturningTest
{
	[Theory]
	[InlineData(null, ReturningKind.Older, true)]
	[InlineData(null, ReturningKind.Newer, false)]
	[InlineData(ReturningKind.Older, ReturningKind.Older, true)]
	[InlineData(ReturningKind.Newer, ReturningKind.Newer, true)]
	public void ColumnsTryGetValueUsesDefaultKind(ReturningKind? kind, ReturningKind columnKind, bool found)
	{
		var column = new Returning.Column("Name", columnKind);
		var columns = new Returning.ColumnCollection(kind, column);
		var expected = found ? column : default;

		Assert.Equal(found, columns.TryGetValue("name", out var result));
		Assert.Equal(expected, result);
		Assert.Equal(expected.Name, result.Name);
		Assert.Equal(found, columns.Contains("name"));

		Assert.Equal(found, columns.TryGetValue("NAME", null, out result));
		Assert.Equal(expected, result);
		Assert.Equal(expected.Name, result.Name);
	}

	[Fact]
	public void ColumnsTryGetValueDistinguishesKindsAndPreservesNames()
	{
		var columns = DataUpdateOptions.Return(ReturningKind.Older, "Name")
			.Return(ReturningKind.Newer, "NAME").Build().Returning.Columns;

		Assert.True(columns.TryGetValue("name", ReturningKind.Older, out var previous));
		Assert.Equal("Name", previous.Name);
		Assert.Equal(ReturningKind.Older, previous.Kind);
		Assert.True(columns.Contains("name", ReturningKind.Older));

		Assert.True(columns.TryGetValue("name", ReturningKind.Newer, out var current));
		Assert.Equal("NAME", current.Name);
		Assert.Equal(ReturningKind.Newer, current.Kind);
		Assert.True(columns.Contains("name", ReturningKind.Newer));
		Assert.Equal(2, columns.Count);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Unknown")]
	[InlineData(" Name")]
	[InlineData("Name@Older")]
	public void ColumnsTryGetValueResetsResultWhenNameDoesNotMatch(string name)
	{
		var columns = DataDeleteOptions.Return("Name").Build().Returning.Columns;

		Assert.True(columns.TryGetValue("Name", out var result));
		Assert.False(columns.TryGetValue(name, out result));
		Assert.Equal(default, result);
		Assert.False(columns.Contains(name));

		Assert.True(columns.TryGetValue("Name", out result));
		Assert.False(columns.TryGetValue(name, ReturningKind.Older, out result));
		Assert.Equal(default, result);
		Assert.False(columns.Contains(name, ReturningKind.Older));
	}

	[Theory]
	[InlineData(ReturningKind.Older, ReturningKind.Newer)]
	[InlineData(ReturningKind.Newer, ReturningKind.Older)]
	public void ColumnsTryGetValueRejectsDifferentFixedKind(ReturningKind kind, ReturningKind requested)
	{
		var columns = new Returning.ColumnCollection(kind, new Returning.Column("Name", kind));

		Assert.True(columns.TryGetValue("Name", out var result));
		Assert.False(columns.TryGetValue("Name", requested, out result));
		Assert.Equal(default, result);
		Assert.False(columns.Contains("Name", requested));
	}

	[Fact]
	public void ColumnsTryGetValueReflectsCollectionChanges()
	{
		var columns = new Returning.ColumnCollection(null);

		Assert.False(columns.TryGetValue("Name", out var result));
		Assert.Equal(default, result);
		Assert.True(columns.Append("Name", ReturningKind.Newer));
		Assert.True(columns.Prepend("Name", ReturningKind.Older));

		Assert.True(columns.TryGetValue("name", out result));
		Assert.Equal(new Returning.Column("Name", ReturningKind.Older), result);
		Assert.True(columns.Remove(result));
		Assert.False(columns.TryGetValue("name", out result));
		Assert.Equal(default, result);

		Assert.True(columns.TryGetValue("name", ReturningKind.Newer, out result));
		Assert.Equal(new Returning.Column("Name", ReturningKind.Newer), result);
		columns.Clear();

		Assert.False(columns.TryGetValue("name", ReturningKind.Newer, out result));
		Assert.Equal(default, result);
	}

	[Fact]
	public void PopulateResolvesEachResultSchema()
	{
		var returning = DataUpdateOptions.Return(ReturningKind.Newer, "Name", "Enabled").Build().Returning;
		using var first = new DataTable();
		first.Columns.Add("Name@Newer", typeof(string));
		first.Columns.Add("Enabled@Newer", typeof(bool));
		first.Rows.Add("First", false);
		using var second = new DataTable();
		second.Columns.Add("Enabled@Newer", typeof(bool));
		second.Columns.Add("Name@Newer", typeof(string));
		second.Rows.Add(true, "Second");

		foreach(var table in new[] { first, second })
		{
			using var reader = table.CreateDataReader();
			Assert.True(reader.Read());

			returning.Rows.Populate(reader);
		}

		Assert.True(returning.Rows[1].TryGetValue("Name", ReturningKind.Newer, out var name));
		Assert.Equal("Second", name);
		Assert.True(returning.Rows[1].TryGetValue("Enabled", ReturningKind.Newer, out var enabled));
		Assert.Equal(true, enabled);
	}

	[Fact]
	public void SharedColumnsKeepResultOrdinalsIndependent()
	{
		var columns = DataUpdateOptions.Return(ReturningKind.Newer, "Name", "Enabled").Build().Returning.Columns;
		var firstRows = new Returning.RowCollection(columns);
		var secondRows = new Returning.RowCollection(columns);
		using var first = new DataTable();
		first.Columns.Add("Name@Newer", typeof(string));
		first.Columns.Add("Enabled@Newer", typeof(bool));
		first.Rows.Add("First", false);
		using var second = new DataTable();
		second.Columns.Add("Enabled@Newer", typeof(bool));
		second.Columns.Add("Name@Newer", typeof(string));
		second.Rows.Add(true, "Second");
		using var firstReader = first.CreateDataReader();
		using var secondReader = second.CreateDataReader();
		Assert.True(firstReader.Read());
		Assert.True(secondReader.Read());
		firstRows.Populate(firstReader);
		secondRows.Populate(secondReader);
		firstRows.Populate(firstReader);
		Assert.True(firstRows[1].TryGetValue("Name", ReturningKind.Newer, out var name));
		Assert.Equal("First", name);
		Assert.True(secondRows[0].TryGetValue("Name", ReturningKind.Newer, out name));
		Assert.Equal("Second", name);
	}

	[Fact]
	public void ClearRemovesColumnsAndRebuildsProjection()
	{
		var returning = DataDeleteOptions.Return("First").Build().Returning;
		using var table = new DataTable();
		table.Columns.Add("First", typeof(int));
		table.Columns.Add("Second", typeof(int));
		table.Rows.Add(1, 2);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		returning.Rows.Populate(reader);
		returning.Columns.Clear();
		Assert.Empty(returning.Columns);
		Assert.True(returning.Columns.IsEmpty);
		returning.Columns.Append("Second");
		returning.Rows.Populate(reader);
		Assert.True(returning.Rows[1].TryGetValue("Second", out var value));
		Assert.Equal(2, value);
	}

	[Theory]
	[InlineData("Append")]
	[InlineData("Prepend")]
	[InlineData("Remove")]
	[InlineData("Clear")]
	public void ExistingRowsKeepTheirColumnNames(string operation)
	{
		var returning = DataDeleteOptions.Return("Name", "Enabled").Build().Returning;
		using var table = CreateTable();
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		returning.Rows.Populate(reader);
		var previous = Assert.Single(returning.Rows);

		switch(operation)
		{
			case "Append":
				Assert.True(returning.Columns.Append("Id"));
				break;
			case "Prepend":
				Assert.True(returning.Columns.Prepend("Id"));
				break;
			case "Remove":
				Assert.True(returning.Columns.Remove(new("Name", ReturningKind.Older)));
				break;
			case "Clear":
				returning.Columns.Clear();
				Assert.True(returning.Columns.Append("Id"));
				break;
		}

		returning.Rows.Populate(reader);
		Assert.True(previous.TryGetValue("NAME", out var name));
		Assert.Equal("First", name);
		Assert.True(previous.TryGetValue("enabled", out var enabled));
		Assert.Equal(true, enabled);
		Assert.False(previous.TryGetValue("Id", out _));
		Assert.Equal("First", previous[0]);
		Assert.Equal(true, previous.Values[1]);
		var current = returning.Rows[1];

		for(int i = 0; i < returning.Columns.Count; i++)
		{
			var column = returning.Columns[i];
			Assert.True(current.TryGetValue(column.Name, column.Kind, out var value));
			Assert.Equal(reader[column.Name], value);
			Assert.Equal(value, current[i]);
		}
	}

	[Fact]
	public void ColumnArraysAndPublicRowsKeepTheirNames()
	{
		var definitions = new Returning.Column[] { new("Name", ReturningKind.Older), new("Enabled", ReturningKind.Older) };
		var columns = new Returning.ColumnCollection(ReturningKind.Older, definitions);
		var values = new object[] { "First", true };
		var row = new Returning.Row(columns, values);
		definitions[0] = new("Id", ReturningKind.Older);
		Assert.Equal("Name", columns[0].Name);
		Assert.True(columns.Contains("name"));
		var copy = new Returning.Column[2];
		columns.CopyTo(copy, 0);
		Assert.Equal(new Returning.Column("Name", ReturningKind.Older), copy[0]);
		Assert.True(columns.Prepend("Id"));
		Assert.True(row.TryGetValue("Name", out var name));
		Assert.Equal("First", name);
		Assert.Same(values, row.Values);
		Assert.False(row.TryGetValue("Id", out _));
		Assert.False(columns.Contains("Name", ReturningKind.Newer));
		Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => row.TryGetValue("Name", ReturningKind.Newer, out _)).ParamName);
	}

	[Fact]
	public void AddRangePreservesExistingRowsAndSourceRows()
	{
		var returning = DataDeleteOptions.Return("Name").Build().Returning;
		var rows = new Returning.RowCollection(returning.Columns);
		using var table = CreateTable();
		table.Rows.Add(2, "Second", false);

		using(var reader = table.CreateDataReader())
		{
			Assert.True(reader.Read());
			returning.Rows.Populate(reader);
			Assert.True(reader.Read());
			rows.Populate(reader);
			Assert.Single(returning.Rows);
		}

		returning.Rows.AddRange(rows);
		Assert.Equal(2, returning.Rows.Count);
		Assert.Equal("First", returning.Rows[0][0]);
		Assert.True(returning.Rows[1].TryGetValue("name", out var name));
		Assert.Equal("Second", name);
		Assert.Same(Assert.Single(rows).Values, returning.Rows[1].Values);
	}

	[Fact]
	public void AddRangePreservesSourceColumnNames()
	{
		var returning = DataDeleteOptions.Return("Name").Build().Returning;
		var source = DataUpdateOptions.Return(ReturningKind.Newer, "Enabled", "Name").Build().Returning;
		using var table = CreateTable();
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		source.Rows.Populate(reader);
		returning.Rows.AddRange(source.Rows);
		var row = Assert.Single(returning.Rows);
		Assert.True(row.TryGetValue("Name", ReturningKind.Newer, out var name));
		Assert.Equal("First", name);
		Assert.True(row.TryGetValue("Enabled", ReturningKind.Newer, out var enabled));
		Assert.Equal(true, enabled);
	}

	[Fact]
	public void PopulatePreservesKindsAliasesMissingAndNull()
	{
		var returning = DataUpdateOptions.Return(ReturningKind.Older, "Name")
			.Return(ReturningKind.Newer, "Name", "Enabled", "Description", "Missing").Build().Returning;
		using var table = new DataTable();
		table.Columns.Add("NAME@Newer", typeof(string));
		table.Columns.Add("Description@Newer", typeof(string));
		table.Columns.Add("Name@Older", typeof(string));
		table.Columns.Add("enabled", typeof(bool));
		table.Rows.Add("New", DBNull.Value, "Old", true);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());

		returning.Rows.Populate(reader);

		var row = Assert.Single(returning.Rows);
		Assert.True(row.TryGetValue("name", ReturningKind.Older, out var previous));
		Assert.Equal("Old", previous);
		Assert.True(row.TryGetValue("NAME", ReturningKind.Newer, out var current));
		Assert.Equal("New", current);
		Assert.True(row.TryGetValue("Enabled", ReturningKind.Newer, out var enabled));
		Assert.Equal(true, enabled);
		Assert.True(row.TryGetValue("Description", ReturningKind.Newer, out var description));
		Assert.Null(description);
		Assert.False(row.TryGetValue("Missing", ReturningKind.Newer, out _));
		Assert.False(row.TryGetValue("Unknown", ReturningKind.Newer, out _));
	}

	[Fact]
	public void EmptyCollectionsAndInvalidRecordsDoNotAddRows()
	{
		var returning = DataDeleteOptions.Return("Name").Build().Returning;
		Assert.Equal("rows", Assert.Throws<ArgumentNullException>(() => returning.Rows.AddRange(null)).ParamName);
		Assert.Equal("record", Assert.Throws<ArgumentNullException>(() => returning.Rows.Populate(null)).ParamName);
		using var table = CreateTable();
		using var reader = table.CreateDataReader();
		reader.Close();
		Assert.Throws<InvalidOperationException>(() => returning.Rows.Populate(reader));
		Assert.Empty(returning.Rows);
		returning.Rows.AddRange(new Returning.RowCollection(returning.Columns));
		Assert.Empty(returning.Rows);
	}

	private static DataTable CreateTable()
	{
		var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		table.Columns.Add("Name", typeof(string));
		table.Columns.Add("Enabled", typeof(bool));
		table.Rows.Add(1, "First", true);
		return table;
	}
}
