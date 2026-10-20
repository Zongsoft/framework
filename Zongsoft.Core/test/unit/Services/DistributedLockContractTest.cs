using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Services.Distributing;

namespace Zongsoft.Tests.Services;

public class DistributedLockContractTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Options_RequirePositiveExpiry(int milliseconds)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new DistributedLockOptions(TimeSpan.FromMilliseconds(milliseconds)));
	}

	[Fact]
	public void Options_DefaultToManualRenewal()
	{
		var options = new DistributedLockOptions(TimeSpan.FromSeconds(5));

		Assert.Equal(TimeSpan.FromSeconds(5), options.Expiry);
		Assert.Null(options.RenewalInterval);
		Assert.False(options.AutoRenewal);

		options.RenewalInterval = TimeSpan.Zero;
		Assert.False(options.AutoRenewal);
		options.RenewalInterval = TimeSpan.FromSeconds(1);
		Assert.True(options.AutoRenewal);
	}

	[Fact]
	public async Task InterfaceDefaults_PreserveCompatibilityForExistingImplementations()
	{
		IDistributedLock distributedLock = new LegacyLock();
		Assert.Equal(0, distributedLock.FencingToken);
		await Assert.ThrowsAsync<NotSupportedException>(async () => await distributedLock.RenewAsync());
	}

	[Fact]
	public async Task EnterAsync_SuccessCallsOnEnteredOnceAfterHeldStateIsUpdated()
	{
		var manager = new LegacyManager();
		await using var distributedLock = new EnteredProbeLock(manager);

		Assert.True(distributedLock.IsUnheld);
		await distributedLock.EnterAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

		Assert.Equal(1, distributedLock.EnterAttemptCount);
		Assert.Equal(1, distributedLock.EnteredCount);
		Assert.True(distributedLock.WasHeldWhenEntered);
		Assert.True(distributedLock.WasLockedWhenEntered);
		Assert.True(distributedLock.IsLocked);

		await distributedLock.EnterAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
		Assert.Equal(1, distributedLock.EnterAttemptCount);
		Assert.Equal(1, distributedLock.EnteredCount);
	}

	private sealed class LegacyLock : IDistributedLock
	{
		public string Key => "legacy";
		public byte[] Token => [1];
		public bool IsExpired => false;
		public bool IsHeld => true;
		public bool IsUnheld => false;
		public bool IsLocked => true;
		public bool IsUnlocked => false;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
		public ValueTask EnterAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
		ValueTask<bool> IDistributedLock.RenewAsync(CancellationToken cancellation) => throw new NotSupportedException();
	}

	private sealed class LegacyManager : IDistributedLockManager
	{
		public IDistributedLockTokenizer Tokenizer { get; set; }
		public ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult<TimeSpan?>(null);
		public ValueTask<bool> ReleaseAsync(string key, byte[] token, CancellationToken cancellation = default) => ValueTask.FromResult(true);
		public ValueTask<IDistributedLock> AcquireAsync(string key, TimeSpan expiry, CancellationToken cancellation = default) => ValueTask.FromResult<IDistributedLock>(new LegacyLock());
		public ValueTask<IDistributedLock> AcquireAsync(string key, DistributedLockOptions options, CancellationToken cancellation = default) => ValueTask.FromResult<IDistributedLock>(new LegacyLock());
	}

	private sealed class EnteredProbeLock : DistributedLockBase<LegacyManager>
	{
		public EnteredProbeLock(LegacyManager manager) : base(manager, "probe", [1], TimeSpan.FromSeconds(5), false) { }

		public int EnterAttemptCount { get; private set; }
		public int EnteredCount { get; private set; }
		public bool WasHeldWhenEntered { get; private set; }
		public bool WasLockedWhenEntered { get; private set; }

		protected override ValueTask<bool> OnEnterAsync(CancellationToken cancellation)
		{
			this.EnterAttemptCount++;
			return ValueTask.FromResult(true);
		}

		protected override void OnEntered()
		{
			this.EnteredCount++;
			this.WasHeldWhenEntered = this.IsHeld;
			this.WasLockedWhenEntered = this.IsLocked;
		}
	}
}
