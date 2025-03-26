using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.Influx.Tests;

public class SelectTest
{
	[Fact]
	public void TestSelect()
	{
		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using var accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

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
		if(!System.Diagnostics.Debugger.IsAttached)
			return;

		using var accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([Global.ConnectionSettings]));

		Assert.NotNull(accessor);
		Assert.NotNull(Mapping.Entities);
		Assert.NotEmpty(Mapping.Entities);
		Assert.True(Mapping.Entities.Contains("MachineHistory"));

		//var models = accessor.SelectAsync<Models.MachineHistory>();
		//Assert.NotNull(models);
		//Assert.NotEmpty(models);
	}
}
