using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.PostgreSql.Tests;

[Collection("Database")]
public class CommandTest(DatabaseFixture database) : IDisposable
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task ExecuteAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		var log = Model.Build<Log>(log =>
		{
			log.UserId = 1;
			log.Target = "MyTarget";
			log.Action = "MyAction";
			log.TenantId = 1;
			log.BranchId = 0;
			log.Timestamp = DateTime.Now;
			log.Severity = LogSeverity.Info;
		});

		var count = await accessor.InsertAsync(log);
		Assert.Equal(1, count);
		Assert.True(log.LogId > 0);

		var script = "SELECT COUNT(*) FROM \"Log\" WHERE \"Severity\"=@Severity AND \"Timestamp\" BETWEEN @Timestamp1 AND @Timestamp2";
		var command = Mapping.Commands.Script(PostgreSqlDriver.NAME, script)
			.Parameter("@Severity", DataType.Byte)
			.Parameter("@Timestamp1", DataType.DateTime)
			.Parameter("@Timestamp2", DataType.DateTime);

		var value = await accessor.ExecuteScalarAsync(command.QualifiedName, [
			new Parameter("@Severity", LogSeverity.Info),
			new Parameter("@Timestamp1", Range.Timing.Today().Minimum),
			new Parameter("@Timestamp2", Range.Timing.Today().Maximum),
		]);

		Assert.NotNull(value);
		Assert.Equal(1L, value);
	}

	void IDisposable.Dispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		var accessor = _database.Accessor;
		accessor.Execute("TruncateLog");
	}
}
