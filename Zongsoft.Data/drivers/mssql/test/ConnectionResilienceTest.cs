using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Tests.Models;

namespace Zongsoft.Data.MsSql.Tests;

[Collection("Database")]
[Trait("Category", "Resilience")]
public class ConnectionResilienceTest
{
	private const int REQUEST_COUNT = 768;

	public ConnectionResilienceTest(DatabaseFixture _) { }

	[Fact]
	public async Task FailedConnectionRejectsConcurrentReadsWritesAndImports()
	{
		var name = $"{nameof(ConnectionResilienceTest)}-{Guid.NewGuid():N}";
		var settings = Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings(
			name,
			"Server=127.0.0.1,1;Database=unavailable;UserName=invalid;Password=invalid;" +
			"Timeout=1s;Pooling=true;MaximumPoolSize=5;TrustServerCertificate=true;" +
			"CircuitBreaker.Duration=00:01:00;CircuitBreaker.MaximumDuration=00:01:00");
		Assert.Equal("00:01:00", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("00:01:00", settings.Properties["CircuitBreaker.MaximumDuration"]);

		using IDataAccess accessor = DataAccessProvider.Instance.GetAccessor(name, new DataAccessOptions([settings]));
		var criteria = Condition.Equal(nameof(UserModel.UserId), 999_999u);

		var initialFailure = await Record.ExceptionAsync(async () => await accessor.ExistsAsync<UserModel>(criteria));
		Assert.NotNull(initialFailure);
		Assert.IsNotType<DataConnectionException>(initialFailure);

		await Assert.ThrowsAsync<DataConnectionException>(
			async () => await accessor.ExistsAsync<UserModel>(criteria));

		ThreadPool.GetAvailableThreads(out var workersBefore, out _);
		var started = System.Diagnostics.Stopwatch.StartNew();

		var requests = Enumerable.Range(0, REQUEST_COUNT)
			.Select(index => Task.Run(async () =>
			{
				try
				{
					switch(index % 3)
					{
						case 0:
							await accessor.ExistsAsync<UserModel>(criteria);
							break;
						case 1:
							await accessor.InsertAsync(CreateModel(index));
							break;
						default:
							await accessor.ImportAsync(new[] { CreateModel(index) });
							break;
					}

					return null;
				}
				catch(Exception exception)
				{
					return exception;
				}
			}))
			.ToArray();

		var failures = await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(15));
		started.Stop();

		Assert.Equal(REQUEST_COUNT, failures.Length);
		Assert.All(failures, failure => Assert.IsType<DataConnectionException>(failure));
		Assert.True(started.Elapsed < TimeSpan.FromSeconds(15));

		var sentinel = Task.Run(static () => 100);
		Assert.Equal(100, await sentinel.WaitAsync(TimeSpan.FromSeconds(2)));

		ThreadPool.GetAvailableThreads(out var workersAfter, out _);
		Assert.True(workersBefore > 0);
		Assert.True(workersAfter > 0);
	}

	private static UserModel CreateModel(int index) => Model.Build<UserModel>(model =>
	{
		model.UserId = (uint)(1_000_000 + index);
		model.Name = $"CircuitBreaker-{index}";
	});
}
