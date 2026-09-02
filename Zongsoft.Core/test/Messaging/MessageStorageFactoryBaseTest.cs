using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MessageStorageFactoryCollection
{
	public const string Name = nameof(MessageStorageFactoryCollection);
}

[Collection(MessageStorageFactoryCollection.Name)]
public class MessageStorageFactoryBaseTest
{
	private const string IDENTIFIER_ENVIRONMENT_VARIABLE = "ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER";

	[Fact]
	public void Create_ReturnsTypedStorageAndBridgesNonGenericInterface()
	{
		using var environment = new EnvironmentVariableScope("typed-identifier");
		var factory = new TestFactory();

		TestStorage typed = factory.Create("typed");
		var untyped = ((IMessageStorageFactory)factory).Create("untyped");

		Assert.IsType<TestStorage>(typed);
		Assert.IsType<TestStorage>(untyped);
		Assert.Equal(2, factory.Count);
		Assert.Equal("Zongsoft.Messaging.Storage:typed:typed-identifier", typed.Partition);
		Assert.Equal("Zongsoft.Messaging.Storage:untyped:typed-identifier", Assert.IsType<TestStorage>(untyped).Partition);
	}

	[Fact]
	public void CreateRejectsInvalidNamesAndNormalizesValidName()
	{
		using var environment = new EnvironmentVariableScope("identifier-one");
		var factory = new TestFactory();

		Assert.Throws<ArgumentNullException>(() => factory.Create(null));
		Assert.Throws<ArgumentNullException>(() => factory.Create(string.Empty));
		Assert.Throws<ArgumentNullException>(() => factory.Create(" \t "));

		var storage = factory.Create("  QueueServer  ");
		Assert.Equal("QueueServer", factory.LastName);
		Assert.Equal("Zongsoft.Messaging.Storage:QueueServer:identifier-one", storage.Partition);
	}

	[Fact]
	public void FirstUseTrimsAndFreezesEnvironmentIdentifier()
	{
		using var environment = new EnvironmentVariableScope("constructor-identifier");
		var factory = new TestFactory();

		Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, "  identifier-one  ");
		var first = factory.Create("first");
		Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, "identifier-two");
		var second = factory.Create("second");

		Assert.Equal("identifier-one", factory.StorageIdentifier);
		Assert.EndsWith(":identifier-one", first.Partition, StringComparison.Ordinal);
		Assert.EndsWith(":identifier-one", second.Partition, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void MissingIdentifierFallsBackToMachineName(string identifier)
	{
		using var environment = new EnvironmentVariableScope(identifier);
		var factory = new TestFactory();
		var storage = factory.Create("QueueServer");

		Assert.Equal(Environment.MachineName, factory.StorageIdentifier);
		Assert.Equal($"Zongsoft.Messaging.Storage:QueueServer:{Environment.MachineName}", storage.Partition);
	}

	[Fact]
	public void PartitionHonorsLengthBoundaryAndUsesStableSha256Fallback()
	{
		using var environment = new EnvironmentVariableScope("identifier");
		const string prefix = "Zongsoft.Messaging.Storage";
		const string identifier = "identifier";
		var boundaryName = new string('a', 128 - prefix.Length - identifier.Length - 2);
		var factory = new TestFactory();

		var boundary = factory.Create(boundaryName);
		Assert.Equal(128, boundary.Partition.Length);
		Assert.Equal($"{prefix}:{boundaryName}:{identifier}", boundary.Partition);

		var overflowName = boundaryName + "b";
		var raw = $"{prefix}:{overflowName}:{identifier}";
		var expected = $"{prefix}:sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()}";
		var overflow = factory.Create(overflowName);

		Assert.Equal(expected, overflow.Partition);
		Assert.True(overflow.Partition.Length <= 128);
	}

	[Fact]
	public void DerivedFactoryCanOverridePartitionGeneration()
	{
		using var environment = new EnvironmentVariableScope("ignored");
		var storage = new CustomPartitionFactory().Create(" QueueServer ");

		Assert.Equal("custom:QueueServer", storage.Partition);
	}

	[Fact]
	public void PartitionRejectsInvalidConnectionNames()
	{
		using var environment = new EnvironmentVariableScope("identifier");
		var factory = new TestFactory();

		Assert.True(Assert.Throws<Common.OperationException>(() => factory.BuildPartition(null)).IsArgument);
		Assert.True(Assert.Throws<Common.OperationException>(() => factory.BuildPartition(string.Empty)).IsArgument);
		Assert.True(Assert.Throws<Common.OperationException>(() => factory.BuildPartition(" \t ")).IsArgument);
	}

	[Fact]
	public void NullStorageResultBecomesOperationException()
	{
		using var environment = new EnvironmentVariableScope("identifier");
		var factory = new TestFactory { ReturnNull = true };
		var exception = Assert.Throws<Common.OperationException>(() => factory.Create("QueueServer"));

		Assert.True(exception.IsUnprocessed);
		Assert.Contains("QueueServer", exception.Message, StringComparison.Ordinal);
	}

	private class TestFactory : MessageStorageFactoryBase<TestStorage>
	{
		public int Count { get; private set; }
		public string LastName { get; private set; }
		public bool ReturnNull { get; set; }
		public string StorageIdentifier => this.Identifier;
		public string BuildPartition(string name) => this.GetPartition(name);

		protected override TestStorage OnCreate(string name)
		{
			this.Count++;
			this.LastName = name;
			return this.ReturnNull ? null : new TestStorage(this.GetPartition(name));
		}
	}

	private sealed class CustomPartitionFactory : MessageStorageFactoryBase<TestStorage>
	{
		protected override TestStorage OnCreate(string name) => new(this.GetPartition(name));
		protected override string GetPartition(string name) => $"custom:{name}";
	}

	private sealed class TestStorage(string partition) : IMessageStorage
	{
		public string Name => "Test";
		public string Partition { get; } = partition;
		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public IAsyncEnumerable<Message> GetAsync(CancellationToken cancellation = default) => Empty();
		public IAsyncEnumerable<Message> GetAsync(string topic, CancellationToken cancellation = default) => Empty();
		public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default) => ValueTask.FromResult(false);

		private static async IAsyncEnumerable<Message> Empty()
		{
			await Task.CompletedTask;
			yield break;
		}
	}

	private sealed class EnvironmentVariableScope : IDisposable
	{
		private readonly string _original;

		public EnvironmentVariableScope(string value)
		{
			_original = Environment.GetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE);
			Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, value);
		}

		public void Dispose() => Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, _original);
	}
}
