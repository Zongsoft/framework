using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MsSql.Tests;

[Collection(nameof(ImporterAsyncCollection))]
public class ImporterAsyncTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetTableAsync_AwaitsValidationAndPreservesStructUpdates(bool asynchronousSource)
	{
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entity = new DataEntity(null, $"Importer{Guid.NewGuid():N}");
		var first = entity.Properties.Simplex(nameof(Row.Value), DataType.Int32, false);
		var second = entity.Properties.Simplex(nameof(Row.Following), DataType.Int32, false);
		var rows = new[] { new Row { Value = 7 } };
		object input = asynchronousSource ? Zongsoft.Collections.Enumerable.EnumerateAsync<Row>(rows) : rows;
		var previous = DataEnvironment.Validators;
		var validations = 0;
		using var cancellation = new CancellationTokenSource();
		var accessor = new DataAccess(entity.Name);
		Mapping.Entities.Add(entity);

		try
		{
			((DataProvider)accessor.Provider).Multiplexer = new Multiplexer();
			DataEnvironment.Validators = new ValidatorProvider(new Validator(async (context, property, token) =>
			{
				Assert.Same(input, context.Data);
				Assert.Equal(cancellation.Token, token);
				validations++;

				if(ReferenceEquals(first, property))
				{
					entered.TrySetResult();
					await release.Task.WaitAsync(token);
					return new DataValidatorResult { HasValue = true, Value = 41 };
				}

				Assert.Same(second, property);
				Assert.True(release.Task.IsCompletedSuccessfully);
				return default;
			}));

			using var context = new DataImportContext(accessor, entity.Name, input, null);
			await using var session = context.Session;
			var members = new DataImporterBase.MemberCollection
			{
				new(context, first, typeof(Row).GetField(nameof(Row.Value))),
				new(context, second, typeof(Row).GetProperty(nameof(Row.Following))),
			};
			var method = typeof(MsSqlImporter).GetMethod("GetTableAsync", BindingFlags.Static | BindingFlags.NonPublic);
			Assert.NotNull(method);

			var operation = ((ValueTask<DataTable>)method.Invoke(null, [context, members, cancellation.Token])).AsTask();

			try
			{
				await entered.Task.WaitAsync(_timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(1, validations);
				release.TrySetResult();

				using var table = await operation.WaitAsync(_timeout);
				Assert.Equal(2, validations);
				Assert.Equal(2, table.Columns.Count);
				Assert.Equal(typeof(int), table.Columns[nameof(Row.Value)].DataType);
				Assert.Equal(typeof(int), table.Columns[nameof(Row.Following)].DataType);
				Assert.Equal(1, table.Rows.Count);
				Assert.Equal(41, table.Rows[0][nameof(Row.Value)]);
				Assert.Equal(42, table.Rows[0][nameof(Row.Following)]);
				Assert.Equal(7, rows[0].Value);
				Assert.Null(session.Connection);
			}
			finally
			{
				release.TrySetResult();
				await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
			}
		}
		finally
		{
			DataEnvironment.Validators = previous;

			try { await ((IAsyncDisposable)accessor).DisposeAsync(); }
			finally { Mapping.Entities.Remove(entity.QualifiedName); }
		}
	}

	private struct Row
	{
		public int Value;
		public int Following { get => this.Value + 1; set { } }
	}

	private sealed class ValidatorProvider(IDataValidator validator) : IDataValidatorProvider
	{
		public IDataValidator GetValidator(IDataAccessContextBase context) => validator;
	}

	private sealed class Validator(Func<DataImportContextBase, IDataEntityProperty, CancellationToken, ValueTask<DataValidatorResult>> callback) : IDataValidator
	{
		public ValueTask<DataValidatorResult> OnImportAsync(DataImportContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => callback(context, property, cancellation);
		public ValueTask<ICondition> ValidateAsync(IDataAccessContextBase context, ICondition criteria, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<DataValidatorResult> OnInsertAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<DataValidatorResult> OnUpdateAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	private sealed class Multiplexer : IDataMultiplexer
	{
		private readonly IDataSource _source = new Source();
		public IDataSourceProvider Provider => throw new NotSupportedException();
		public IDataSourceSelector Selector => throw new NotSupportedException();
		public IDataSource GetSource(IDataAccessContextBase context) => _source;
	}

	private sealed class Source : IDataSource
	{
		public string Name { get; } = $"Importer{Guid.NewGuid():N}";
		public string ConnectionString => throw new NotSupportedException();
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver => throw new NotSupportedException();
		public FeatureCollection Features { get; } = new();
		public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
		public ValueTask<DataTable> GetSchemaAsync(string name, bool refresh, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool Equals(IDataSource other) => ReferenceEquals(this, other);
	}
}

[CollectionDefinition(nameof(ImporterAsyncCollection), DisableParallelization = true)]
public sealed class ImporterAsyncCollection { }
