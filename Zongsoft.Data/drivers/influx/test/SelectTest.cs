using System;

using Xunit;

namespace Zongsoft.Data.Influx.Tests;

[Collection("Database")]
public class SelectTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestSelect()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("MachineHistory"));

		//var models = accessor.Select<Models.MachineHistory>();
		//Assert.NotNull(models);
		//Assert.NotEmpty(models);
	}

	[Fact]
	public void TestSelectAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("MachineHistory"));

		//var models = accessor.SelectAsync<Models.MachineHistory>();
		//Assert.NotNull(models);
		//Assert.NotEmpty(models);
	}
}
