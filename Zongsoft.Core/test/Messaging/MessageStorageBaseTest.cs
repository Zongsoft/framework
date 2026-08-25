using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Tests;

public class MessageStorageBaseTest
{
	[Fact]
	public void StorageExposesNameAndStronglyTypedSettingsThroughInterface()
	{
		var first = new StorageSettings("server=first");
		var second = new StorageSettings("server=second");
		var storage = new TestStorage(first);
		var abstraction = (IMessageStorage)storage;

		Assert.Equal("memory", storage.Name);
		Assert.Equal(storage.Name, abstraction.Name);
		Assert.Same(first, storage.Settings);
		Assert.Same(first, abstraction.Settings);

		abstraction.Settings = second;
		Assert.Same(second, storage.Settings);
		Assert.Same(second, abstraction.Settings);
	}

	[Fact]
	public void InterfaceSettingsRejectsNullOrIncompatibleSettings()
	{
		var settings = new StorageSettings();
		var storage = new TestStorage(settings);
		var abstraction = (IMessageStorage)storage;

		Assert.Throws<ArgumentNullException>(() => new TestStorage(null));
		Assert.Throws<ArgumentNullException>(() => storage.Settings = null);
		Assert.Throws<ArgumentNullException>(() => abstraction.Settings = null);
		Assert.Throws<ArgumentException>(() => abstraction.Settings = new ConnectionSettings());
		Assert.Same(settings, storage.Settings);
		Assert.Same(settings, abstraction.Settings);
	}

	[Fact]
	public async Task TemplateMethodsValidateAndForwardWithoutLogicalPartition()
	{
		var storage = new TestStorage(new StorageSettings());
		var timestamp = DateTime.UtcNow.AddMinutes(-1);
		var expiry = TimeSpan.FromMinutes(5);
		var acknowledged = 0;
		var message = new Message("message-1", "topic/one", new byte[] { 1, 2, 3 }, "alpha", () => acknowledged++)
		{
			Identity = "producer-one",
			Timestamp = timestamp,
		};

		await storage.SetAsync(message, expiry);
		message.Data[0] = 9;
		message.Tags = "changed";
		var loaded = new List<Message>();
		await foreach(var item in storage.GetAsync())
			loaded.Add(item);

		var stored = Assert.Single(loaded);
		Assert.Equal("message-1", stored.Identifier);
		Assert.Equal("topic/one", stored.Topic);
		Assert.Equal("producer-one", stored.Identity);
		Assert.Equal("alpha", stored.Tags);
		Assert.Equal(new byte[] { 1, 2, 3 }, stored.Data);
		Assert.Equal(timestamp, stored.Timestamp);
		Assert.Equal(expiry, storage.Expiry);
		stored.Acknowledge();
		Assert.Equal(0, acknowledged);
		Assert.True(await storage.RemoveAsync("message-1"));
		Assert.Empty(storage.Messages);
		Assert.Equal(1, storage.SetCount);
		Assert.Equal(1, storage.RemoveCount);
		Assert.Equal(1, storage.GetCount);
	}

	[Fact]
	public async Task InvalidArgumentsAndCancellationDoNotReachImplementation()
	{
		var storage = new TestStorage(new StorageSettings());
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var message = new Message("message", "topic", []);

		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SetAsync(default).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SetAsync(new Message("topic", [])).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.RemoveAsync(null).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.SetAsync(message, cancellation: cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.RemoveAsync("message", cancellation.Token).AsTask());
		Assert.ThrowsAny<OperationCanceledException>(() => storage.GetAsync(cancellation.Token));

		Assert.Equal(0, storage.SetCount);
		Assert.Equal(0, storage.RemoveCount);
		Assert.Equal(0, storage.GetCount);
	}

	private sealed class TestStorage(StorageSettings settings) : MessageStorageBase<StorageSettings>(settings)
	{
		private readonly Dictionary<string, Message> _messages = new();

		public override string Name => "memory";
		public int SetCount { get; private set; }
		public int RemoveCount { get; private set; }
		public int GetCount { get; private set; }
		public TimeSpan Expiry { get; private set; }
		public IReadOnlyDictionary<string, Message> Messages => _messages;

		protected override ValueTask OnSetAsync(Message message, TimeSpan expiry, CancellationToken cancellation)
		{
			this.SetCount++;
			this.Expiry = expiry;
			_messages[message.Identifier] = new Message(message.Identifier, message.Topic, (byte[])message.Data.Clone())
			{
				Identity = message.Identity,
				Tags = message.Tags,
				Timestamp = message.Timestamp,
			};
			return ValueTask.CompletedTask;
		}

		protected override ValueTask<bool> OnRemoveAsync(string identifier, CancellationToken cancellation)
		{
			this.RemoveCount++;
			return ValueTask.FromResult(_messages.Remove(identifier));
		}

		protected override async IAsyncEnumerable<Message> OnGetAsync([EnumeratorCancellation] CancellationToken cancellation)
		{
			this.GetCount++;
			await Task.Yield();

			foreach(var message in _messages.Values)
			{
				cancellation.ThrowIfCancellationRequested();
				yield return new Message(message.Identifier, message.Topic, (byte[])message.Data.Clone())
				{
					Identity = message.Identity,
					Tags = message.Tags,
					Timestamp = message.Timestamp,
				};
			}
		}
	}

	private sealed class StorageSettings(string settings = null) : ConnectionSettingsBase<StorageSettingsDriver>(StorageSettingsDriver.Instance, settings);

	private sealed class StorageSettingsDriver : ConnectionSettingsDriver<StorageSettings>
	{
		public static readonly StorageSettingsDriver Instance = new();
		private StorageSettingsDriver() : base("StorageTest") { }
	}
}
