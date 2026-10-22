using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(MappingCollection.Name)]
public sealed class DataValidationAsyncTest : IDisposable
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
	private readonly string _entityName = $"AsyncValidator_{Guid.NewGuid():N}";
	private readonly IDataAccess _accessor = DispatchProxy.Create<IDataAccess, UnusedAccessor>();

	public DataValidationAsyncTest() => Mapping.Entities.Add(new DataEntity(null, _entityName));
	public void Dispose() => Mapping.Entities.Remove(_entityName);

	public static TheoryData<DataAccessMethod, bool> ConditionCases
	{
		get
		{
			var cases = new TheoryData<DataAccessMethod, bool>();

			foreach(var method in new[] { DataAccessMethod.Exists, DataAccessMethod.Aggregate, DataAccessMethod.Select, DataAccessMethod.Delete, DataAccessMethod.Update })
			{
				cases.Add(method, false);
				cases.Add(method, true);
			}

			return cases;
		}
	}

	[Theory]
	[MemberData(nameof(ConditionCases))]
	public async Task ValidateAsync_PendingConditionValidator_UsesExplicitOrContextCriteria(DataAccessMethod method, bool explicitCriteria)
	{
		using var cancellation = new CancellationTokenSource();
		var original = Condition.Equal("Original", 1);
		var supplied = Condition.Equal("Supplied", 2);
		var replacement = Condition.Equal("Scoped", 3);
		var release = new TaskCompletionSource<ICondition>(TaskCreationOptions.RunContinuationsAsynchronously);
		var observed = new List<(IDataAccessContextBase Context, ICondition Criteria, CancellationToken Token)>();
		var validator = new Validator
		{
			Criteria = (context, criteria, token) =>
			{
				observed.Add((context, criteria, token));
				return new ValueTask<ICondition>(release.Task.WaitAsync(token));
			},
		};
		using var scenario = this.CreateConditionContext(method, original, validator);
		var pending = scenario.Validate(explicitCriteria ? supplied : null, cancellation.Token).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);
			var call = Assert.Single(observed);
			Assert.Same(scenario.Context, call.Context);
			Assert.Same(explicitCriteria ? supplied : original, call.Criteria);
			Assert.Equal(cancellation.Token, call.Token);
			Assert.Same(original, scenario.Criteria());
			release.SetResult(replacement);
			Assert.Same(replacement, await pending.WaitAsync(_timeout));
			Assert.Same(original, scenario.Criteria());
			Assert.Single(observed);
		}
		finally
		{
			release.TrySetResult(replacement);
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Theory]
	[MemberData(nameof(ConditionCases))]
	public async Task ValidateAsync_SuppressedOrMissingValidator_ReturnsUnchangedCriteria(DataAccessMethod method, bool missing)
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var original = Condition.Equal("Original", 1);
		var supplied = Condition.Equal("Supplied", 2);
		var calls = 0;
		var validator = new Validator
		{
			Criteria = (_, _, _) =>
			{
				calls++;
				throw new InvalidOperationException();
			},
		};
		using var scenario = this.CreateConditionContext(method, original, missing ? null : validator);
		scenario.Suppress(!missing);

		Assert.Same(original, await scenario.Validate(null, cancellation.Token));
		Assert.Same(supplied, await scenario.Validate(supplied, cancellation.Token));
		Assert.Same(original, scenario.Criteria());
		Assert.Equal(0, calls);
	}

	[Theory]
	[MemberData(nameof(ConditionCases))]
	public async Task ValidateAsync_CancellationAndFailure_ArePropagated(DataAccessMethod method, bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();
		var release = new TaskCompletionSource<ICondition>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var validator = new Validator
		{
			Criteria = (_, _, token) =>
			{
				calls++;
				Assert.Equal(cancellation.Token, token);
				return new ValueTask<ICondition>(release.Task.WaitAsync(token));
			},
		};
		using var scenario = this.CreateConditionContext(method, Condition.Equal("Id", 1), validator);
		var pending = scenario.Validate(null, cancellation.Token).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);

			if(cancel)
			{
				cancellation.Cancel();
				var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, exception.CancellationToken);
			}
			else
			{
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(_timeout)));
			}

			Assert.Equal(1, calls);
		}
		finally
		{
			release.TrySetResult(null);
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false, DataAccessMethod.Insert)]
	[InlineData(false, DataAccessMethod.Update)]
	[InlineData(true, DataAccessMethod.Insert)]
	[InlineData(true, DataAccessMethod.Update)]
	public async Task FieldValidation_AwaitsSelectedBranchAndPreservesResult(bool upsert, DataAccessMethod method)
	{
		using var cancellation = new CancellationTokenSource();
		var binder = new DeferredBinder();
		var property = new DataEntitySimplexProperty("Name", System.Data.DbType.String, true);
		var release = new TaskCompletionSource<DataValidatorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var observed = new List<(DataAccessMethod Method, IDataMutateContextBase Context, IDataEntityProperty Property, CancellationToken Token)>();
		var result = new DataValidatorResult { HasValue = true, Value = binder };
		var validator = new Validator
		{
			Field = (branch, context, member, token) =>
			{
				observed.Add((branch, context, member, token));
				return observed.Count == 1 ? new ValueTask<DataValidatorResult>(release.Task) : ValueTask.FromResult(result);
			},
		};
		var context = this.CreateMutationContext(upsert, method, validator);
		using var owner = (IDisposable)context;
		var original = context.Data;
		var pending = ValidateFieldAsync(context, method, property, cancellation.Token).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);
			var call = Assert.Single(observed);
			Assert.Equal(method, call.Method);
			Assert.Same(context, call.Context);
			Assert.Same(property, call.Property);
			Assert.Equal(cancellation.Token, call.Token);
			release.SetResult(result);
			var first = await pending.WaitAsync(_timeout);
			Assert.True(first.HasValue);
			Assert.Same(binder, first.Value);
			Assert.Equal(0, binder.Calls);

			result = new DataValidatorResult { HasValue = true, Value = null };
			var second = await ValidateFieldAsync(context, method, property, cancellation.Token);
			Assert.True(second.HasValue);
			Assert.Null(second.Value);

			result = default;
			var third = await ValidateFieldAsync(context, method, property, cancellation.Token);
			Assert.False(third.HasValue);
			Assert.Null(third.Value);
			Assert.Equal(3, observed.Count);
			Assert.Same(original, context.Data);
		}
		finally
		{
			release.TrySetResult(result);
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false, DataAccessMethod.Insert, false)]
	[InlineData(false, DataAccessMethod.Insert, true)]
	[InlineData(false, DataAccessMethod.Update, false)]
	[InlineData(false, DataAccessMethod.Update, true)]
	[InlineData(true, DataAccessMethod.Insert, false)]
	[InlineData(true, DataAccessMethod.Insert, true)]
	[InlineData(true, DataAccessMethod.Update, false)]
	[InlineData(true, DataAccessMethod.Update, true)]
	public async Task FieldValidation_SuppressedOrMissingValidator_DoesNotInvokeCallbacks(bool upsert, DataAccessMethod method, bool missing)
	{
		var calls = 0;
		var validator = new Validator
		{
			Field = (_, _, _, _) =>
			{
				calls++;
				throw new InvalidOperationException();
			},
		};
		var context = this.CreateMutationContext(upsert, method, missing ? null : validator);
		using var owner = (IDisposable)context;
		context.Options.ValidatorSuppressed = !missing;

		var result = await ValidateFieldAsync(context, method, new DataEntitySimplexProperty("Name", System.Data.DbType.String, true), CancellationToken.None);

		Assert.False(result.HasValue);
		Assert.Null(result.Value);
		Assert.Equal(0, calls);
	}

	[Theory]
	[InlineData(false, DataAccessMethod.Insert)]
	[InlineData(false, DataAccessMethod.Update)]
	[InlineData(true, DataAccessMethod.Insert)]
	[InlineData(true, DataAccessMethod.Update)]
	public async Task FieldValidation_Cancellation_ReachesSelectedValidator(bool upsert, DataAccessMethod method)
	{
		using var cancellation = new CancellationTokenSource();
		var release = new TaskCompletionSource<DataValidatorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var validator = new Validator
		{
			Field = (branch, _, _, token) =>
			{
				calls++;
				Assert.Equal(method, branch);
				Assert.Equal(cancellation.Token, token);
				return new ValueTask<DataValidatorResult>(release.Task.WaitAsync(token));
			},
		};
		var context = this.CreateMutationContext(upsert, method, validator);
		using var owner = (IDisposable)context;
		var pending = ValidateFieldAsync(context, method, null, cancellation.Token).AsTask();

		try
		{
			Assert.False(pending.IsCompleted);
			cancellation.Cancel();
			var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout));
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			Assert.Equal(1, calls);
		}
		finally
		{
			release.TrySetResult(default);
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Fact]
	public async Task FieldValidation_UnsupportedMethodOrNullContext_PreservesGuardBehavior()
	{
		var calls = 0;
		var context = this.CreateMutationContext(true, DataAccessMethod.Insert, new Validator
		{
			Field = (_, _, _, _) =>
			{
				calls++;
				throw new InvalidOperationException();
			},
		});
		using var owner = (IDisposable)context;

		foreach(var method in new[] { DataAccessMethod.Delete, DataAccessMethod.Import, DataAccessMethod.Upsert })
		{
			var result = await context.ValidateAsync(method, null);
			Assert.False(result.HasValue);
			Assert.Null(result.Value);
		}

		Assert.Equal(0, calls);
		var failure = await Assert.ThrowsAsync<ArgumentNullException>(() =>
			DataAccessContextExtension.ValidateAsync((IDataMutateContextBase)null, DataAccessMethod.Insert, null).AsTask());
		Assert.Equal("context", failure.ParamName);
	}

	private ConditionContext CreateConditionContext(DataAccessMethod method, ICondition criteria, IDataValidator validator)
	{
		switch(method)
		{
			case DataAccessMethod.Exists:
				var exists = new ExistsContext(_accessor, _entityName, criteria) { Validator = validator };
				return new(exists, (criteria, token) => criteria == null ? exists.ValidateAsync(token) : exists.ValidateAsync(criteria, token), () => exists.Criteria, value => exists.Options.ValidatorSuppressed = value);
			case DataAccessMethod.Aggregate:
				var aggregate = new AggregateContext(_accessor, _entityName, criteria) { Validator = validator };
				return new(aggregate, (criteria, token) => criteria == null ? aggregate.ValidateAsync(token) : aggregate.ValidateAsync(criteria, token), () => aggregate.Criteria, value => aggregate.Options.ValidatorSuppressed = value);
			case DataAccessMethod.Select:
				var select = new SelectContext(_accessor, _entityName, criteria) { Validator = validator };
				return new(select, (criteria, token) => criteria == null ? select.ValidateAsync(token) : select.ValidateAsync(criteria, token), () => select.Criteria, value => select.Options.ValidatorSuppressed = value);
			case DataAccessMethod.Delete:
				var delete = new DeleteContext(_accessor, _entityName, criteria) { Validator = validator };
				return new(delete, (criteria, token) => criteria == null ? delete.ValidateAsync(token) : delete.ValidateAsync(criteria, token), () => delete.Criteria, value => delete.Options.ValidatorSuppressed = value);
			case DataAccessMethod.Update:
				var update = new UpdateContext(_accessor, _entityName, criteria) { Validator = validator };
				return new(update, (criteria, token) => criteria == null ? update.ValidateAsync(token) : update.ValidateAsync(criteria, token), () => update.Criteria, value => update.Options.ValidatorSuppressed = value);
			default:
				throw new ArgumentOutOfRangeException(nameof(method));
		}
	}

	private IDataMutateContextBase CreateMutationContext(bool upsert, DataAccessMethod method, IDataValidator validator) => upsert ?
		new UpsertContext(_accessor, _entityName) { Validator = validator } : method == DataAccessMethod.Insert ?
		new InsertContext(_accessor, _entityName) { Validator = validator } :
		new UpdateContext(_accessor, _entityName, null) { Validator = validator };

	private static ValueTask<DataValidatorResult> ValidateFieldAsync(IDataMutateContextBase context, DataAccessMethod method, IDataEntityProperty property, CancellationToken cancellation) => context switch
	{
		DataInsertContextBase insert => insert.ValidateAsync(property, cancellation),
		DataUpdateContextBase update => DataAccessContextExtension.ValidateAsync(update, property, cancellation),
		_ => context.ValidateAsync(method, property, cancellation),
	};

	private sealed class ConditionContext(IDataAccessContextBase context, Func<ICondition, CancellationToken, ValueTask<ICondition>> validate, Func<ICondition> criteria, Action<bool> suppress) : IDisposable
	{
		public IDataAccessContextBase Context => context;
		public Func<ICondition, CancellationToken, ValueTask<ICondition>> Validate => validate;
		public Func<ICondition> Criteria => criteria;
		public Action<bool> Suppress => suppress;
		public void Dispose() => ((IDisposable)context).Dispose();
	}

	private sealed class Validator : IDataValidator
	{
		public Func<IDataAccessContextBase, ICondition, CancellationToken, ValueTask<ICondition>> Criteria { get; init; }
		public Func<DataAccessMethod, IDataMutateContextBase, IDataEntityProperty, CancellationToken, ValueTask<DataValidatorResult>> Field { get; init; }
		public ValueTask<ICondition> ValidateAsync(IDataAccessContextBase context, ICondition criteria, CancellationToken cancellation = default) => this.Criteria(context, criteria, cancellation);
		public ValueTask<DataValidatorResult> OnImportAsync(DataImportContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<DataValidatorResult> OnInsertAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => this.Field(DataAccessMethod.Insert, context, property, cancellation);
		public ValueTask<DataValidatorResult> OnUpdateAsync(IDataMutateContextBase context, IDataEntityProperty property, CancellationToken cancellation = default) => this.Field(DataAccessMethod.Update, context, property, cancellation);
	}

	private sealed class DeferredBinder : IDataValueBinder
	{
		public int Calls { get; private set; }
		public ValueTask<object> BindAsync(IDataMutateContextBase context, object data, object value, CancellationToken cancellation = default)
		{
			this.Calls++;
			throw new NotSupportedException();
		}
	}

	public class UnusedAccessor : DispatchProxy
	{
		protected override object Invoke(MethodInfo targetMethod, object[] args) => throw new NotSupportedException();
	}

	private sealed class ExistsContext(IDataAccess accessor, string name, ICondition criteria) : DataExistContextBase(accessor, name, criteria)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class AggregateContext(IDataAccess accessor, string name, ICondition criteria) : DataAggregateContextBase(accessor, name, DataAggregate.Count("Id"), criteria)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class SelectContext(IDataAccess accessor, string name, ICondition criteria) : DataSelectContextBase(accessor, name, typeof(object), null, criteria, null, null, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class DeleteContext(IDataAccess accessor, string name, ICondition criteria) : DataDeleteContextBase(accessor, name, criteria, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class InsertContext(IDataAccess accessor, string name) : DataInsertContextBase(accessor, name, false, new object(), null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class UpdateContext(IDataAccess accessor, string name, ICondition criteria) : DataUpdateContextBase(accessor, name, new object(), criteria, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
	private sealed class UpsertContext(IDataAccess accessor, string name) : DataUpsertContextBase(accessor, name, false, new object(), null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}
}
