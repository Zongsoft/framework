using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class ExecuteTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task ExecuteAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var timestamp = DateTime.Today.AddSeconds(Random.Shared.Next(1, 86399));

		var script = "INSERT INTO `Log` (`UserId`, `Target`, `Action`, `TenantId`, `BranchId`, `Severity`, `Timestamp`) VALUES (@UserId, @Target, @Action, @TenantId, @BranchId, @Severity, @Timestamp)";
		var command = Mapping.Commands.Script(MySqlDriver.NAME, DataCommandMutability.Insert, script)
			.Parameter("@UserId", DataType.UInt32)
			.Parameter("@TenantId", DataType.UInt32)
			.Parameter("@BranchId", DataType.UInt32)
			.Parameter("@Severity", DataType.Byte)
			.Parameter("@Target", DataType.AnsiString, 100)
			.Parameter("@Action", DataType.AnsiString, 100)
			.Parameter("@Timestamp", DataType.DateTime);

		var count = await accessor.ExecuteAsync(command.QualifiedName, [
			new Parameter("@UserId", 1U),
			new Parameter("@TenantId", 1U),
			new Parameter("@BranchId", 0U),
			new Parameter("@Target", "MyTarget"),
			new Parameter("@Action", "MyAction"),
			new Parameter("@Severity", LogSeverity.Info),
			new Parameter("@Timestamp", timestamp)]);
		Assert.Equal(1, count);

		script = "SELECT COUNT(*) FROM `Log` WHERE `Severity`=@Severity AND `Timestamp` BETWEEN @Timestamp1 AND @Timestamp2";
		command = Mapping.Commands.Script(MySqlDriver.NAME, script)
			.Parameter("@Severity", DataType.Byte)
			.Parameter("@Timestamp1", DataType.DateTime)
			.Parameter("@Timestamp2", DataType.DateTime);

		var value = await accessor.ExecuteScalarAsync(command.QualifiedName, [
			new Parameter("@Severity", LogSeverity.Info),
			new Parameter("@Timestamp1", Range.Timing.Today().Minimum),
			new Parameter("@Timestamp2", Range.Timing.Today().Maximum)]);

		Assert.NotNull(value);
		Assert.Equal(1L, value);

		command = Mapping.Commands.Script(MySqlDriver.NAME, "SELECT * FROM `Log`");
		var logs = accessor.ExecuteAsync<Log>(command.QualifiedName);

		#if NET10_0_OR_GREATER
		var log = await logs.SingleOrDefaultAsync();

		Assert.NotNull(log);
		Assert.True(log.LogId > 0);
		Assert.Equal(1U, log.UserId);
		Assert.Equal(1U, log.TenantId);
		Assert.Equal(0U, log.BranchId);
		Assert.Equal("MyTarget", log.Target);
		Assert.Equal("MyAction", log.Action);
		Assert.Equal(timestamp, log.Timestamp);
		#endif
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Execute("TruncateLog");
	}
}
