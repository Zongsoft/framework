using System;
using System.Data;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ReturningTest
{
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
}
