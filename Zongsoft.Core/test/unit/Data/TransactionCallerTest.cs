using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Data.Transactions;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Data.Tests;

public class TransactionCallerTest
{
	[Theory]
	[InlineData("data-access", false)]
	[InlineData("data-access", true)]
	[InlineData("data-service", false)]
	[InlineData("data-service", true)]
	[InlineData("service-base", false)]
	[InlineData("service-base", true)]
	[InlineData("roles", false)]
	[InlineData("roles", true)]
	[InlineData("users", false)]
	[InlineData("users", true)]
	public async Task BatchOperation_EnumerationFails_PreservesOperationAndRollbackErrors(string target, bool rollbackFails)
	{
		using var services = new ServiceCollection().BuildServiceProvider();
		var accessor = DispatchProxy.Create<IDataAccess, UnusedAccessor>();
		var service = new ServiceProbe(services);
		var operationFailure = new InvalidOperationException();
		var rollbackFailure = new ApplicationException();
		var enlistment = new EnlistmentProbe(rollbackFails ? rollbackFailure : null);
		var previous = Transaction.Current;
		Transaction transaction = null;

		var failure = await Record.ExceptionAsync(async () =>
		{
			var items = Enumerate();

			switch(target)
			{
				case "data-access":
					await DataAccessExtension.UpdateManyAsync(accessor, "Items", items);
					break;
				case "data-service":
					await DataServiceExtension.UpdateManyAsync(service, items);
					break;
				case "service-base":
					await service.UpdateManyAsync("key", items);
					break;
				case "roles":
					await new RoleServiceProbe(accessor).DeleteAsync(items);
					break;
				case "users":
					await new UserServiceProbe(accessor).DeleteAsync(items);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(target));
			}
		});

		if(rollbackFails)
		{
			var aggregate = Assert.IsType<AggregateException>(failure);
			Assert.Collection(aggregate.InnerExceptions,
				error => Assert.Same(operationFailure, error),
				error => Assert.Same(rollbackFailure, error));
			Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
		}
		else
		{
			Assert.Same(operationFailure, failure);
			Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
		}

		Assert.Same(previous, Transaction.Current);
		Assert.Equal(1, enlistment.Calls);
		Assert.Equal(EnlistmentPhase.Rollback, enlistment.Phase);

		IEnumerable<Identifier> Enumerate()
		{
			transaction = Assert.IsType<Transaction>(Transaction.Current);
			Assert.True(transaction.Context.Enlist(enlistment));

			//输入集合的枚举失败发生在事务范围内，无需访问真实数据库。
			if(operationFailure != null)
				throw operationFailure;

			yield break;
		}
	}

	public class UnusedAccessor : DispatchProxy
	{
		protected override object Invoke(MethodInfo targetMethod, object[] args) => throw new NotSupportedException();
	}

	private sealed class ServiceProbe(IServiceProvider services) : DataServiceBase<object>("Items", services);
	private sealed class RoleServiceProbe(IDataAccess accessor) : RoleServiceBase<IRole>
	{
		protected override IDataAccess Accessor => accessor;
	}

	private sealed class UserServiceProbe(IDataAccess accessor) : UserServiceBase<IUser>
	{
		protected override IDataAccess Accessor => accessor;
	}

	private sealed class EnlistmentProbe(Exception failure) : IEnlistment
	{
		public int Calls { get; private set; }
		public EnlistmentPhase Phase { get; private set; }

		public async ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Phase = context.Phase;
			Assert.False(cancellation.CanBeCanceled);
			await Task.Yield();

			if(failure != null)
				throw failure;
		}
	}
}
