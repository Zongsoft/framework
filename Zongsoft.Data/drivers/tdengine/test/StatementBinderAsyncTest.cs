using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine.Tests;

[CollectionDefinition(nameof(OfflineMappingCollection), DisableParallelization = true)]
public sealed class OfflineMappingCollection { }

[Collection(nameof(OfflineMappingCollection))]
[Trait("Category", "Unit")]
public sealed class StatementBinderAsyncTest : IDisposable
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
	private readonly DataEntity _entity;

	public StatementBinderAsyncTest()
	{
		_entity = new DataEntity(null, $"TDengineBinder_{Guid.NewGuid():N}");
		_entity.Properties.Simplex(nameof(Row.Value), DataType.Int32, false);
		Mapping.Entities.Add(_entity);
	}
	public void Dispose() => Mapping.Entities.Remove(_entity.Name);

	[Fact]
	public async Task BindAsync_DeleteRemovesInlinedParameters()
	{
		await using var accessor = new DataAccess("TDengineBinder");
		using var context = new DeleteContext(accessor, _entity.Name);
		await using var command = new LocalCommand();
		var parameter = new LocalParameter { ParameterName = "value", Value = 17 };
		command.Parameters.Add(parameter);
		var statement = new DeleteStatement(_entity);
		statement.Parameters.Add(new ParameterExpression("value", DataType.Int32, 17));

		Assert.Single(command.Parameters);
		await TDengineStatementBinder.Instance.BindAsync(context, statement, command);

		Assert.Empty(command.Parameters);
		Assert.Equal(1, command.ClearCount);
		Assert.Equal(17, parameter.Value);
		Assert.Equal(0, command.Executions);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task BindAsync_InsertAwaitsValueAndPreservesParameters(int outcome)
	{
		using var cancellation = new CancellationTokenSource();
		await using var accessor = new DataAccess("TDengineBinder");
		var row = new Row { Value = 17 };
		var schema = Assert.IsType<Schema>(SchemaParser.Instance.Parse(_entity.Name, nameof(Row.Value), typeof(Row)));
		var member = Assert.Single(schema.Members);
		using var context = new InsertContext(accessor, _entity.Name, row, schema);
		await using var command = new LocalCommand();
		var parameter = new LocalParameter { ParameterName = "value", Value = -1 };
		command.Parameters.Add(parameter);
		var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var binder = new ValueBinder((observedContext, data, value, token) =>
		{
			calls++;
			Assert.Same(context, observedContext);
			Assert.Same(row, data);
			Assert.Equal(17, value);
			Assert.Equal(cancellation.Token, token);
			return new ValueTask<object>(release.Task.WaitAsync(token));
		});
		var statement = new InsertStatement(_entity, null);
		statement.Parameters.Add(new ParameterExpression("value", DataType.Int32) { Value = binder, Type = DataType.Int32, Schema = member });
		var pending = TDengineStatementBinder.Instance.BindAsync(context, statement, command, cancellation.Token).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);
			Assert.Same(parameter, Assert.Single(command.Parameters));
			Assert.Equal(-1, parameter.Value);
			Assert.Equal(17, row.Value);

			if(outcome == 1)
			{
				var failure = new InvalidOperationException();
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(_timeout)));
			}
			else if(outcome == 2)
			{
				cancellation.Cancel();
				var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, failure.CancellationToken);
			}
			else
			{
				release.SetResult(42);
				await pending.WaitAsync(_timeout);
			}

			Assert.Same(parameter, Assert.Single(command.Parameters));
			Assert.Equal(outcome == 0 ? 42 : -1, parameter.Value);
			Assert.Equal(outcome == 0 ? 42 : 17, row.Value);
			Assert.Equal(1, calls);
			Assert.Equal(0, command.ClearCount);
			Assert.Equal(0, command.Executions);
		}
		finally
		{
			release.TrySetResult(42);
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	private sealed class Row
	{
		public int Value { get; set; }
	}
	private sealed class ValueBinder(Func<IDataMutateContextBase, object, object, CancellationToken, ValueTask<object>> callback) : IDataValueBinder
	{
		public ValueTask<object> BindAsync(IDataMutateContextBase context, object data, object value, CancellationToken cancellation = default) => callback(context, data, value, cancellation);
	}
	private sealed class DeleteContext(IDataAccess accessor, string name) : DataDeleteContextBase(accessor, name, null, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class InsertContext(IDataAccess accessor, string name, object data, ISchema schema) : DataInsertContextBase(accessor, name, false, data, schema)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class LocalCommand : DbCommand
	{
		private readonly LocalParameters _parameters = new();
		public int ClearCount => _parameters.ClearCount;
		public int Executions { get; private set; }
		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		protected override DbConnection DbConnection { get; set; }
		protected override DbTransaction DbTransaction { get; set; }
		protected override DbParameterCollection DbParameterCollection => _parameters;
		public override void Cancel() => throw new InvalidOperationException();
		public override void Prepare() => throw new InvalidOperationException();
		public override int ExecuteNonQuery() { this.Executions++; throw new InvalidOperationException(); }
		public override object ExecuteScalar() { this.Executions++; throw new InvalidOperationException(); }
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { this.Executions++; throw new InvalidOperationException(); }
		protected override DbParameter CreateDbParameter() => new LocalParameter();
		public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	private sealed class LocalParameter : DbParameter
	{
		public override DbType DbType { get; set; } = DbType.Int32;
		public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
		public override bool IsNullable { get; set; }
		public override string ParameterName { get; set; }
		public override string SourceColumn { get; set; }
		public override object Value { get; set; }
		public override bool SourceColumnNullMapping { get; set; }
		public override int Size { get; set; }
		public override void ResetDbType() => this.DbType = DbType.Int32;
	}
	private sealed class LocalParameters : DbParameterCollection
	{
		private readonly List<DbParameter> _items = [];
		public int ClearCount { get; private set; }
		public override int Count => _items.Count;
		public override object SyncRoot => this;
		public override bool Contains(object value) => _items.Contains((DbParameter)value);
		public override bool Contains(string value) => this.IndexOf(value) >= 0;
		public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
		public override int IndexOf(string parameterName) => _items.FindIndex(item => item.ParameterName == parameterName);
		public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
		public override IEnumerator GetEnumerator() => _items.GetEnumerator();
		public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
		public override void AddRange(Array values)
		{
			foreach(var value in values)
				this.Add(value);
		}
		public override void Clear() { this.ClearCount++; _items.Clear(); }
		public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
		public override void Remove(object value) => _items.Remove((DbParameter)value);
		public override void RemoveAt(int index) => _items.RemoveAt(index);
		public override void RemoveAt(string parameterName) => _items.RemoveAt(this.IndexOf(parameterName));
		protected override DbParameter GetParameter(int index) => _items[index];
		protected override DbParameter GetParameter(string parameterName) => _items[this.IndexOf(parameterName)];
		protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
		protected override void SetParameter(string parameterName, DbParameter value) => _items[this.IndexOf(parameterName)] = value;
	}
}
