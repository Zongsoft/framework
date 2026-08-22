using System;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Etcd.Tests;

public class EtcdContractTests
{
	[Fact]
	public void ConnectionSettings_ParseValues()
	{
		var settings = Configuration.EtcdConnectionSettingsDriver.Instance.GetSettings("sample", "server=localhost;port=12379;username=user;password=secret;application=test;timeout=3s;heartbeat=4s");

		Assert.Equal("sample", settings.Name);
		Assert.Equal("localhost", settings.Server);
		Assert.Equal(12379, settings.Port);
		Assert.Equal("user", settings.UserName);
		Assert.Equal("secret", settings.Password);
		Assert.Equal("test", settings.Application);
		Assert.Equal(TimeSpan.FromSeconds(3), settings.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(4), settings.Heartbeat);
	}

	[Fact]
	public void Constructors_ValidateAndNormalizeInputs()
	{
		Assert.Throws<ArgumentNullException>(() => new EtcdService(" "));
		Assert.Throws<ArgumentNullException>(() => new EtcdService("test", " "));

		using var service = new EtcdService(" sample ", Global.ConnectionString) { Namespace = " scope: " };
		Assert.Equal("sample", service.Name);
		Assert.Equal("scope", service.Namespace);
		Assert.IsAssignableFrom<ISequence>(service);
		Assert.IsAssignableFrom<ISequenceBase>(service);
		Assert.IsAssignableFrom<IDistributedLockManager>(service);
	}

	[Fact]
	public async System.Threading.Tasks.Task Operations_ValidateBeforeConnecting()
	{
		using var service = new EtcdService("test", Global.ConnectionString);
		await Assert.ThrowsAsync<ArgumentNullException>(() => service.IncreaseAsync(null).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => service.AcquireAsync(null, TimeSpan.FromSeconds(1)).AsTask());
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AcquireAsync("lock", TimeSpan.Zero).AsTask());
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AcquireAsync("lock", new DistributedLockOptions(TimeSpan.FromSeconds(2)) { RenewalInterval = TimeSpan.FromSeconds(2) }).AsTask());
	}

	[Fact]
	public async System.Threading.Tasks.Task Dispose_IsIdempotentAndRejectsOperations()
	{
		var service = new EtcdService("test", Global.ConnectionString);
		service.Dispose();
		service.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => service.HeartbeatAsync().AsTask());
	}
}
