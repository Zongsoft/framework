using System;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

[Collection(nameof(SchemaMappingCollection))]
public class DataSequencerTest : IDisposable
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
	private readonly DataEntity _entity;
	private readonly IDataEntitySimplexProperty _local;
	private readonly IDataEntitySimplexProperty _scoped;

	public DataSequencerTest()
	{
		_entity = new DataEntity("SequenceTests", $"Entry{Guid.NewGuid():N}");
		_entity.Properties.Simplex("Tenant", DataType.String, 30, false);
		_entity.Properties.Simplex("Region", DataType.String, 30, false);
		_local = _entity.Properties.Simplex("LocalId", DataType.Int64, false);
		_scoped = _entity.Properties.Simplex("Id", DataType.Int64, false);
		_local.Sequence = DataEntityPropertySequence.Create(_local, new DataPropertySequence("#", 101, 7));
		_scoped.Sequence = DataEntityPropertySequence.Create(_scoped, new DataPropertySequence("#", 101, 7, "Tenant", "Region"));
		Mapping.Entities.Add(_entity);
	}

	public void Dispose() => Mapping.Entities.Remove(_entity.QualifiedName);

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task IncreaseAsync_FieldOverloads_UseMetadataIntervalAndPreserveZero(bool byName, bool zero)
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new RecordingSequence(async token =>
		{
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
			return 139;
		});
		using var cancellation = new CancellationTokenSource();
		await using var accessor = CreateAccessor(provider);
		var pending = (byName, zero) switch
		{
			(false, false) => accessor.Sequencer.IncreaseAsync(_local, cancellation.Token).AsTask(),
			(false, true) => accessor.Sequencer.IncreaseAsync(_local, 0, cancellation.Token).AsTask(),
			(true, false) => accessor.Sequencer.IncreaseAsync(_entity.QualifiedName, "LocalId", cancellation.Token).AsTask(),
			_ => accessor.Sequencer.IncreaseAsync(_entity.QualifiedName, "LocalId", 0, cancellation.Token).AsTask(),
		};

		try
		{
			await entered.Task.WaitAsync(_timeout);
			Assert.False(pending.IsCompleted);

			var call = Assert.Single(provider.Calls);

			Assert.Equal("increase", call.Operation);
			Assert.Equal($"Zongsoft.Sequence:{_entity.Name}.LocalId", call.Key);
			Assert.Equal(zero ? 0 : 7, call.Interval);
			Assert.Equal(101, call.Seed);
			Assert.Equal(cancellation.Token, call.Cancellation);
			release.SetResult();
			Assert.Equal(139, await pending.WaitAsync(_timeout));
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false, 1, 0, 7, 101)]
	[InlineData(false, 0, 0, 0, 101)]
	[InlineData(false, 4, 23, 4, 23)]
	[InlineData(true, 1, 0, 7, 101)]
	[InlineData(true, 0, 0, 0, 101)]
	[InlineData(true, 4, 23, 4, 23)]
	public async Task SequenceInterface_IncreaseOrDecrease_ResolvesReferencesBeforeDispatch(bool decrease, int interval, int seed, int expectedInterval, int expectedSeed)
	{
		var provider = new RecordingSequence();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = CreateAccessor(provider);
		var sequence = Assert.IsAssignableFrom<ISequenceBase>(accessor.Sequencer);
		var key = $"{_entity.QualifiedName}.Id@ tenant , region ";
		long result;

		if(interval == 1 && seed == 0)
			result = decrease ?
				await sequence.DecreaseAsync(key, cancellation: cancellation.Token) :
				await sequence.IncreaseAsync(key, cancellation: cancellation.Token);
		else
			result = decrease ?
				await sequence.DecreaseAsync(key, interval, seed, cancellation.Token) :
				await sequence.IncreaseAsync(key, interval, seed, cancellation.Token);

		var call = Assert.Single(provider.Calls);

		Assert.Equal(139, result);
		Assert.Equal(decrease ? "decrease" : "increase", call.Operation);
		Assert.Equal($"Zongsoft.Sequence:{_entity.Name}.Id:tenant-region", call.Key);
		Assert.Equal(expectedInterval, call.Interval);
		Assert.Equal(expectedSeed, call.Seed);
		Assert.Equal(cancellation.Token, call.Cancellation);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SequenceInterface_Reset_UsesMetadataSeedUnlessExplicit(bool explicitValue)
	{
		var provider = new RecordingSequence();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = CreateAccessor(provider);
		var sequence = Assert.IsAssignableFrom<ISequenceBase>(accessor.Sequencer);
		var key = $"{_entity.QualifiedName}.Id@tenant,region";

		if(explicitValue)
			await sequence.ResetAsync(key, 23, cancellation.Token);
		else
			await sequence.ResetAsync(key, cancellation: cancellation.Token);

		var call = Assert.Single(provider.Calls);

		Assert.Equal("reset", call.Operation);
		Assert.Equal($"Zongsoft.Sequence:{_entity.Name}.Id:tenant-region", call.Key);
		Assert.Equal(explicitValue ? 23 : 101, call.Seed);
		Assert.Equal(cancellation.Token, call.Cancellation);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IncreaseAsync_ReferencesWithoutContext_RejectsBeforeProvider(bool sequenceInterface)
	{
		var provider = new RecordingSequence();
		await using var accessor = CreateAccessor(provider);

		if(sequenceInterface)
		{
			var sequence = Assert.IsAssignableFrom<ISequenceBase>(accessor.Sequencer);

			await Assert.ThrowsAsync<InvalidOperationException>(() => sequence.IncreaseAsync($"{_entity.QualifiedName}.Id").AsTask());
		}
		else
			await Assert.ThrowsAsync<InvalidOperationException>(() => accessor.Sequencer.IncreaseAsync(_scoped).AsTask());

		Assert.Empty(provider.Calls);
	}

	[Fact]
	public async Task SequenceInterface_SynchronousMembersAreRejected()
	{
		var provider = new RecordingSequence();
		await using var accessor = CreateAccessor(provider);
		var sequence = Assert.IsAssignableFrom<ISequenceBase>(accessor.Sequencer);
		var key = $"{_entity.QualifiedName}.LocalId";

		Assert.Throws<NotSupportedException>(() => sequence.Increase(key));
		Assert.Throws<NotSupportedException>(() => sequence.Decrease(key));
		Assert.Throws<NotSupportedException>(() => sequence.Reset(key));
		Assert.Empty(provider.Calls);
	}

	[Theory]
	[InlineData("classic")]
	[InlineData("generic")]
	[InlineData("expando")]
	[InlineData("object")]
	public async Task BindAsync_DictionaryOrObject_ResolvesReferencesAndWritesSequence(string kind)
	{
		object data = kind switch
		{
			"classic" => new Hashtable { ["Id"] = 0L, ["Tenant"] = " tenant ", ["Region"] = " region " },
			"generic" => new Dictionary<string, object> { ["Id"] = 0L, ["Tenant"] = " tenant ", ["Region"] = " region " },
			"expando" => CreateExpando(),
			_ => new SequenceItem { Tenant = " tenant ", Region = " region " },
		};
		var provider = new RecordingSequence();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = CreateAccessor(provider);
		var schema = Assert.IsType<Schema>(SchemaParser.Instance.Parse(_entity.QualifiedName, "Id", data.GetType()));
		using var context = new InsertContext(accessor, _entity.QualifiedName, data, schema);
		await using var command = new OutputCommand();

		await StatementBinder.Default.BindAsync(context, this.CreateStatement(schema), command, cancellation.Token);

		var call = Assert.Single(provider.Calls);

		Assert.Equal("increase", call.Operation);
		Assert.Equal($"Zongsoft.Sequence:{_entity.Name}.Id:tenant-region", call.Key);
		Assert.Equal(7, call.Interval);
		Assert.Equal(101, call.Seed);
		Assert.Equal(cancellation.Token, call.Cancellation);

		var actual = data switch
		{
			IDictionary<string, object> generic => Assert.IsType<long>(generic["Id"]),
			IDictionary classic => Assert.IsType<long>(classic["Id"]),
			_ => ((SequenceItem)data).Id,
		};

		Assert.Equal(139L, actual);
	}

	[Fact]
	public async Task BindAsync_MissingReference_AwaitsValidatorBeforeSequenceAndWriteback()
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var data = new SequenceItem { Region = "region" };
		var provider = new RecordingSequence();
		using var cancellation = new CancellationTokenSource();
		await using var accessor = CreateAccessor(provider);
		var schema = Assert.IsType<Schema>(SchemaParser.Instance.Parse(_entity.QualifiedName, "Id", typeof(SequenceItem)));
		var validator = new SequenceValidator(async (validationContext, property, token) =>
		{
			Assert.Same(data, validationContext.Data);
			Assert.Same(_entity.Properties["Tenant"], property);
			Assert.Equal(cancellation.Token, token);
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
			return new DataValidatorResult { HasValue = true, Value = "validated" };
		});
		using var context = new InsertContext(accessor, _entity.QualifiedName, data, schema) { Validator = validator };
		await using var command = new OutputCommand();
		var pending = StatementBinder.Default.BindAsync(context, this.CreateStatement(schema), command, cancellation.Token).AsTask();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			Assert.False(pending.IsCompleted);
			Assert.Empty(provider.Calls);
			Assert.Equal(0, data.Id);
			release.SetResult();
			await pending.WaitAsync(_timeout);

			var call = Assert.Single(provider.Calls);

			Assert.Equal(1, validator.Calls);
			Assert.Equal("increase", call.Operation);
			Assert.Equal($"Zongsoft.Sequence:{_entity.Name}.Id:validated-region", call.Key);
			Assert.Equal(7, call.Interval);
			Assert.Equal(101, call.Seed);
			Assert.Equal(cancellation.Token, call.Cancellation);
			Assert.Equal(139, data.Id);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	private InsertStatement CreateStatement(Schema schema)
	{
		var statement = new InsertStatement(_entity, null);
		statement.Fields.Add(new FieldIdentifier("Id") { Token = Assert.Single(schema.Members).Token });
		statement.Parameters.Add(new ParameterExpression("output", DataType.Int64, ParameterDirection.Output));
		return statement;
	}

	private static DataAccess CreateAccessor(ISequenceBase sequence)
	{
		var accessor = new DataAccess("DataSequencerTests");
		accessor.Sequencer.Sequence = sequence;
		return accessor;
	}

	private static object CreateExpando()
	{
		IDictionary<string, object> data = new ExpandoObject();
		data.Add("Id", 0L);
		data.Add("Tenant", " tenant ");
		data.Add("Region", " region ");
		return data;
	}

	private sealed class SequenceItem
	{
		public long Id { get; set; }
		public string Tenant { get; set; }
		public string Region { get; set; }
	}

	private sealed class InsertContext(IDataAccess accessor, string name, object data, ISchema schema) : DataInsertContextBase(accessor, name, false, data, schema)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}

	private sealed class SequenceValidator(Func<IDataMutateContextBase, IDataEntityProperty, CancellationToken, ValueTask<DataValidatorResult>> callback) : IDataValidator
	{
		public int Calls { get; private set; }
		public ValueTask<ICondition> ValidateAsync(IDataAccessContextBase context, ICondition criteria, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask<DataValidatorResult> OnImportAsync(DataImportContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask<DataValidatorResult> OnUpdateAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask<DataValidatorResult> OnInsertAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default)
		{
			this.Calls++;
			return callback(context, property, cancellation);
		}
	}

	private sealed class OutputCommand : DbCommand
	{
		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		protected override DbConnection DbConnection { get; set; }
		protected override DbTransaction DbTransaction { get; set; }
		protected override DbParameterCollection DbParameterCollection { get; } = new OutputParameters();
		public override void Cancel() => throw new InvalidOperationException();
		public override void Prepare() => throw new InvalidOperationException();
		public override int ExecuteNonQuery() => throw new InvalidOperationException();
		public override object ExecuteScalar() => throw new InvalidOperationException();
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new InvalidOperationException();
		protected override DbParameter CreateDbParameter() => throw new InvalidOperationException();
		public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class OutputParameter : DbParameter
	{
		public override DbType DbType { get; set; } = DbType.Int64;
		public override ParameterDirection Direction { get; set; } = ParameterDirection.Output;
		public override bool IsNullable { get; set; }
		public override string ParameterName { get; set; } = "output";
		public override string SourceColumn { get; set; }
		public override object Value { get; set; }
		public override bool SourceColumnNullMapping { get; set; }
		public override int Size { get; set; }
		public override void ResetDbType() => this.DbType = DbType.Int64;
	}

	private sealed class OutputParameters : DbParameterCollection
	{
		private readonly OutputParameter _parameter = new();
		public override int Count => 1;
		public override object SyncRoot => this;
		public override bool Contains(object value) => ReferenceEquals(value, _parameter);
		public override bool Contains(string value) => value == _parameter.ParameterName;
		public override int IndexOf(object value) => this.Contains(value) ? 0 : -1;
		public override int IndexOf(string parameterName) => this.Contains(parameterName) ? 0 : -1;
		public override void CopyTo(Array array, int index) => array.SetValue(_parameter, index);
		public override IEnumerator GetEnumerator() => new[] { _parameter }.GetEnumerator();
		public override int Add(object value) => throw new InvalidOperationException();
		public override void AddRange(Array values) => throw new InvalidOperationException();
		public override void Clear() => throw new InvalidOperationException();
		public override void Insert(int index, object value) => throw new InvalidOperationException();
		public override void Remove(object value) => throw new InvalidOperationException();
		public override void RemoveAt(int index) => throw new InvalidOperationException();
		public override void RemoveAt(string parameterName) => throw new InvalidOperationException();
		protected override DbParameter GetParameter(int index) => index == 0 ? _parameter : throw new IndexOutOfRangeException();
		protected override DbParameter GetParameter(string parameterName) => this.Contains(parameterName) ? _parameter : throw new IndexOutOfRangeException();
		protected override void SetParameter(int index, DbParameter value) => throw new InvalidOperationException();
		protected override void SetParameter(string parameterName, DbParameter value) => throw new InvalidOperationException();
	}

	private sealed class RecordingSequence(Func<CancellationToken, ValueTask<long>> callback = null) : ISequenceBase
	{
		public List<(string Operation, string Key, int Interval, int Seed, CancellationToken Cancellation)> Calls { get; } = [];

		public long Increase(string key, int interval = 1, int seed = 0) => throw new InvalidOperationException();
		public long Decrease(string key, int interval = 1, int seed = 0) => throw new InvalidOperationException();
		public void Reset(string key, int value = 0) => throw new InvalidOperationException();

		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			this.Calls.Add(("increase", key, interval, seed, cancellation));
			return callback == null ? ValueTask.FromResult(139L) : callback(cancellation);
		}

		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			this.Calls.Add(("decrease", key, interval, seed, cancellation));
			return callback == null ? ValueTask.FromResult(139L) : callback(cancellation);
		}

		public ValueTask ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
		{
			this.Calls.Add(("reset", key, 0, value, cancellation));
			return ValueTask.CompletedTask;
		}
	}
}
