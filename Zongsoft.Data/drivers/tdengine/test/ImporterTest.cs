using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
public class ImporterTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestImport()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		var count = accessor.Import(Generate(COUNT));
		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task TestImportAsync()
	{
		const int COUNT = 100;

		if(!Global.IsTestingEnabled)
			return;

		IDataAccess accessor = _database.Accessor;

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("GatewayHistory"));

		var count = await accessor.ImportAsync(Generate(COUNT));
		Assert.Equal(COUNT, count);
	}

	#region 私有方法
	private static IEnumerable<Models.GatewayHistory> Generate(int count = 100)
	{
		var timestamp = DateTime.Now;

		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? Random.Shared.Next(1, 10) : 0;
			var failureMessage = failureCode > 0 ? $"Message #{Math.Abs(Random.Shared.Next()):X}" : null;

			yield return new Models.GatewayHistory()
			{
				Timestamp = timestamp.AddMilliseconds(1),
				GatewayId = (uint)(i + 1),
				MetricId = (ulong)Random.Shared.NextInt64(),
				Value = Random.Shared.NextDouble(),
				FailureCode = failureCode,
				FailureMessage = failureMessage,
			};
		}
	}
	#endregion
}
