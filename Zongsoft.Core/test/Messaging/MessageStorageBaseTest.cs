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
	public void StorageDisposableReflectsSettings()
	{
		var storage = new TestStorage(new StorageSettings());
		Assert.False(storage.Disposable);
		Assert.False(((IMessageStorage)storage).Disposable);

		storage.Settings = new StorageSettings("Disposable=true");
		Assert.True(storage.Disposable);
		Assert.True(((IMessageStorage)storage).Disposable);

		storage.Settings = new StorageSettings("Disposable=false");
		Assert.False(storage.Disposable);
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
	public async Task InvalidMessagesAndCancellationDoNotReachImplementation()
	{
		var storage = new TestStorage(new StorageSettings());
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var message = new Message("message", "topic", []);

		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SetAsync(default).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SetAsync(new Message("topic", [])).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => storage.RemoveAsync(null).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.ClearAsync(cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.ClearAsync(string.Empty, cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.SetAsync(message, cancellation: cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.RemoveAsync("message", cancellation.Token).AsTask());
		Assert.ThrowsAny<OperationCanceledException>(() => storage.GetAsync(cancellation.Token));
		Assert.ThrowsAny<OperationCanceledException>(() => storage.GetAsync(string.Empty, cancellation.Token));

		Assert.Equal(0, storage.ClearCount);
		Assert.Equal(0, storage.SetCount);
		Assert.Equal(0, storage.RemoveCount);
		Assert.Equal(0, storage.GetCount);
	}

	[Fact]
	public async Task UnfilteredOperationsForwardNullSentinelToUnifiedTemplates()
	{
		var storage = new TestStorage(new StorageSettings());
		await storage.SetAsync(new Message("first", "topic/one", [1]));
		await storage.SetAsync(new Message("second", "topic/two", [2]));

		var messages = new List<Message>();
		await foreach(var message in storage.GetAsync())
			messages.Add(message);

		Assert.Equal(2, messages.Count);
		Assert.Null(storage.LastTopic);
		Assert.Equal(1, storage.GetCount);

		messages.Clear();
		await foreach(var message in storage.GetAsync((string)null))
			messages.Add(message);

		Assert.Equal(2, messages.Count);
		Assert.Null(storage.LastTopic);
		Assert.Equal(2, storage.GetCount);
		Assert.Equal(2, await storage.ClearAsync((string)null));
		Assert.Null(storage.LastTopic);
		Assert.Equal(1, storage.ClearCount);

		await storage.SetAsync(new Message("first", "topic/one", [1]));
		await storage.SetAsync(new Message("second", "topic/two", [2]));
		Assert.Equal(2, await storage.ClearAsync());
		Assert.Null(storage.LastTopic);
		Assert.Equal(2, storage.ClearCount);
	}

	[Fact]
	public async Task ClearOperationsReturnActualRemovalCount()
	{
		var storage = new TestStorage(new StorageSettings());
		await storage.SetAsync(new Message("lower-1", "topic/one", [1]));
		await storage.SetAsync(new Message("lower-2", "topic/one", [2]));
		await storage.SetAsync(new Message("upper", "Topic/one", [3]));
		await storage.SetAsync(new Message("default", string.Empty, [4]));

		Assert.Equal(2, await storage.ClearAsync("topic/one"));
		Assert.Equal(2, storage.Messages.Count);
		Assert.True(storage.Messages.ContainsKey("upper"));
		Assert.True(storage.Messages.ContainsKey("default"));
		Assert.Equal("topic/one", storage.LastTopic);

		Assert.Equal(1, await storage.ClearAsync(string.Empty));
		Assert.Single(storage.Messages);
		Assert.True(storage.Messages.ContainsKey("upper"));
		Assert.Equal(string.Empty, storage.LastTopic);

		Assert.Equal(1, await storage.ClearAsync());
		Assert.Empty(storage.Messages);
		Assert.Equal(3, storage.ClearCount);
	}

	[Fact]
	public async Task TopicQueriesUseOrdinalExactMatchingAndReturnSnapshots()
	{
		var storage = new TestStorage(new StorageSettings());
		var acknowledged = 0;
		await storage.SetAsync(new Message("lower", "topic/one", [1, 2, 3], "alpha", () => acknowledged++));
		await storage.SetAsync(new Message("upper", "Topic/one", [4, 5, 6]));
		await storage.SetAsync(new Message("child", "topic/one/child", [7, 8, 9]));

		var first = new List<Message>();
		await foreach(var message in storage.GetAsync("topic/one"))
			first.Add(message);

		var stored = Assert.Single(first);
		Assert.Equal("lower", stored.Identifier);
		Assert.Equal("alpha", stored.Tags);
		Assert.Equal([1, 2, 3], stored.Data);
		stored.Data[0] = 9;
		stored.Tags = "changed";
		stored.Acknowledge();
		Assert.Equal(0, acknowledged);

		var second = new List<Message>();
		await foreach(var message in storage.GetAsync("topic/one"))
			second.Add(message);

		stored = Assert.Single(second);
		Assert.Equal("alpha", stored.Tags);
		Assert.Equal([1, 2, 3], stored.Data);
		Assert.Equal("topic/one", storage.LastTopic);
		Assert.Equal(2, storage.GetCount);
	}

	private sealed class TestStorage(StorageSettings settings) : MessageStorageBase<StorageSettings>(settings)
	{
		private readonly Dictionary<string, Message> _messages = new();

		public override string Name => "memory";
		public int ClearCount { get; private set; }
		public int SetCount { get; private set; }
		public int RemoveCount { get; private set; }
		public int GetCount { get; private set; }
		public string LastTopic { get; private set; }
		public TimeSpan Expiry { get; private set; }
		public IReadOnlyDictionary<string, Message> Messages => _messages;

		protected override ValueTask<int> OnClearAsync(string topic, CancellationToken cancellation)
		{
			this.ClearCount++;
			this.LastTopic = topic;
			if(topic == null)
			{
				var count = _messages.Count;
				_messages.Clear();
				return ValueTask.FromResult(count);
			}

			var identifiers = new List<string>();

			foreach(var entry in _messages)
			{
				if(string.Equals(entry.Value.Topic, topic, StringComparison.Ordinal))
					identifiers.Add(entry.Key);
			}

			foreach(var identifier in identifiers)
				_messages.Remove(identifier);

			return ValueTask.FromResult(identifiers.Count);
		}

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

		protected override async IAsyncEnumerable<Message> OnGetAsync(string topic, [EnumeratorCancellation] CancellationToken cancellation)
		{
			this.GetCount++;
			this.LastTopic = topic;
			await Task.Yield();

			foreach(var message in _messages.Values)
			{
				cancellation.ThrowIfCancellationRequested();
				if(topic == null || string.Equals(message.Topic, topic, StringComparison.Ordinal))
				{
					yield return new Message(message.Identifier, message.Topic, (byte[])message.Data.Clone())
					{
						Identity = message.Identity,
						Tags = message.Tags,
						Timestamp = message.Timestamp,
					};
				}
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
