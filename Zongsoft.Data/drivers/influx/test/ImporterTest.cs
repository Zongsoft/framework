using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Influx.Tests;

public class ImporterTest
{
	[Fact]
	public void TestImport()
	{
		const int COUNT = 100;

		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using IDataAccess accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("MachineHistory"));

		var count = accessor.Import(Generate(COUNT));
		Assert.Equal(COUNT, count);
	}

	[Fact]
	public async Task TestImportAsync()
	{
		const int COUNT = 100;

		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using IDataAccess accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("MachineHistory"));

		var count = await accessor.ImportAsync(Generate(COUNT));
		Assert.Equal(COUNT, count);
	}

	#region 私有方法
	private static IEnumerable<Models.MachineHistory> Generate(int count = 100)
	{
		for(int i = 0; i < count; i++)
		{
			var failureCode = i > 0 && i % 50 == 0 ? Random.Shared.Next(1, 10) : 0;
			var failureMessage = failureCode > 0 ? $"Message #{Math.Abs(Random.Shared.Next()):X}" : null;

			yield return new Models.MachineHistory()
			{
				MachineId = (uint)(i + 1),
				MetricId = (ulong)Random.Shared.NextInt64(),
				Value = Random.Shared.NextDouble(),
				FailureCode = failureCode,
				FailureMessage = failureMessage,
			};
		}
	}
	#endregion
}
