using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

[Collection(nameof(DataConnectorCollection))]
public sealed class SelectStatementBuilderTest
{
	[Fact]
	public async Task GroupedNavigation_DoesNotAddEntityKeysOrChangeGrouping()
	{
		await using var scope = new Scope();
		var schema = scope.Parse("Payment{Branch{TenantId,BranchId,Name}}");
		var grouping = Grouping.Group("Payment.TenantId", "Payment.BranchId", "Payment.Status").Count().Sum("Amount");

		var statement = await scope.BuildAsync(schema, grouping);

		Assert.Equal(["TenantId", "BranchId", "Status"], statement.GroupBy.Keys.Select(field => field.Name));
		Assert.Equal(["TenantId", "BranchId", "Status", "Payment.Branch.TenantId", "Payment.Branch.BranchId", "Payment.Branch.Name"], GetNames(statement));
		Assert.Collection(statement.Select.Members.OfType<AggregateExpression>(),
			aggregate => Assert.Equal(DataAggregateFunction.Count, aggregate.Function),
			aggregate => Assert.Equal(DataAggregateFunction.Sum, aggregate.Function));
		Assert.Equal("Payment{Branch{TenantId,BranchId,Name}}", schema.ToString());
	}

	[Fact]
	public async Task OrdinaryThenGrouped_ReusesUnmodifiedSchema()
	{
		await using var scope = new Scope();
		var schema = scope.Parse("Payment{Branch{Name}}");
		var original = schema.ToString();

		var ordinary = await scope.BuildAsync(schema);
		Assert.Equal(["Payment.Branch.Name", "Payment.Branch.TenantId", "Payment.Branch.BranchId", "Payment.PaymentId"], GetNames(ordinary));
		Assert.Equal(original, schema.ToString());

		var grouped = await scope.BuildAsync(schema, Grouping.Group("Payment.BranchId").Count());
		Assert.Equal(["BranchId", "Payment.Branch.Name"], GetNames(grouped));
		Assert.Single(grouped.GroupBy.Keys);
		Assert.Equal(original, schema.ToString());

		var repeated = await scope.BuildAsync(schema);
		Assert.Equal(GetNames(ordinary), GetNames(repeated));
		Assert.Equal(original, schema.ToString());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OrdinaryNavigation_RetainsIdentityAndNullSemantics(bool present)
	{
		await using var scope = new Scope();
		var statement = await scope.BuildAsync(scope.Parse("RefundId,Payment{Note}"));
		Assert.Equal(["RefundId", "Payment.Note", "Payment.PaymentId"], GetNames(statement));
		using var table = new DataTable();

		foreach(var field in statement.Select.Members.Cast<FieldIdentifier>())
			table.Columns.Add(field.Alias ?? field.Name, field.Name == "Note" ? typeof(string) : typeof(int));

		table.Rows.Add(7, DBNull.Value, present ? 42 : DBNull.Value);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());

		var populator = ModelPopulatorProvider.Instance.GetPopulator(scope.Driver, typeof(Refund), reader, scope.Entity);
		var result = populator.Populate<Refund>(reader);

		Assert.Equal(7, result.RefundId);
		if(present)
		{
			Assert.NotNull(result.Payment);
			Assert.Equal(42, result.Payment.PaymentId);
			Assert.Null(result.Payment.Note);
		}
		else
			Assert.Null(result.Payment);
	}

	[Fact]
	public async Task AggregateWithoutKeys_DoesNotAddEntityKeys()
	{
		await using var scope = new Scope();
		var statement = await scope.BuildAsync(scope.Parse("Payment{Note}"), Grouping.Group(string.Empty).Count());

		Assert.Null(statement.GroupBy);
		Assert.Equal(["Payment.Note"], GetNames(statement));
		Assert.Equal(DataAggregateFunction.Count, Assert.Single(statement.Select.Members.OfType<AggregateExpression>()).Function);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GroupedNavigation_PopulatesExplicitNestedKeysWithoutIdentity(bool present)
	{
		await using var scope = new Scope();
		var schema = scope.Parse("Payment{Branch{TenantId,BranchId,Name}}");
		var statement = await scope.BuildAsync(schema, Grouping.Group("Payment.TenantId", "Payment.BranchId").Count());
		using var table = new DataTable();

		foreach(var field in statement.Select.Members.OfType<FieldIdentifier>())
			table.Columns.Add(field.Alias ?? field.Name, field.Name == "Name" ? typeof(string) : typeof(int));

		table.Rows.Add(3, 8, present ? 3 : DBNull.Value, present ? 8 : DBNull.Value, DBNull.Value);
		using var reader = table.CreateDataReader();
		Assert.True(reader.Read());
		var result = ModelPopulatorProvider.Instance.GetPopulator(scope.Driver, typeof(Refund), reader, scope.Entity).Populate<Refund>(reader);

		Assert.Equal(3, result.TenantId);
		if(present)
		{
			Assert.NotNull(result.Payment);
			Assert.Equal(0, result.Payment.PaymentId);
			Assert.NotNull(result.Payment.Branch);
			Assert.Equal(3, result.Payment.Branch.TenantId);
			Assert.Equal(8, result.Payment.Branch.BranchId);
			Assert.Null(result.Payment.Branch.Name);
		}
		else
			Assert.Null(result.Payment);
	}

	[Fact]
	public async Task GroupedCollection_RetainsKeysInIndependentChildQuery()
	{
		await using var scope = new Scope();
		var schema = scope.Parse("Payments{Branch{Name}}");
		var original = schema.ToString();
		var statement = await scope.BuildAsync(schema, Grouping.Group("TenantId").Count());
		var child = Assert.IsType<SelectStatement>(Assert.Single(statement.Slaves));

		Assert.Single(statement.GroupBy.Keys);
		Assert.Null(child.GroupBy);
		Assert.Equal(["Branch.Name", "Branch.TenantId", "Branch.BranchId"], GetNames(child));
		Assert.Equal(original, schema.ToString());
	}

	[Fact]
	public async Task GroupedNavigation_PreservesExplicitKey()
	{
		await using var scope = new Scope();
		var statement = await scope.BuildAsync(scope.Parse("Payment{PaymentId}"), Grouping.Group("Payment.PaymentId").Count());

		Assert.Equal(["PaymentId", "Payment.PaymentId"], GetNames(statement));
		Assert.Equal("PaymentId", Assert.Single(statement.GroupBy.Keys).Name);
	}

	private static string[] GetNames(SelectStatement statement) => statement.Select.Members.OfType<FieldIdentifier>().Select(field => field.Alias ?? field.Name).ToArray();

	private sealed class Scope : IAsyncDisposable
	{
		private readonly DataAccess _access;
		private readonly DataEntity[] _entities;
		public Scope()
		{
			var prefix = $"SelectTests{Guid.NewGuid():N}";
			var branch = new DataEntity(prefix, nameof(Branch));
			branch.Properties.Simplex(nameof(Branch.TenantId), DataType.Int32, false);
			branch.Properties.Simplex(nameof(Branch.BranchId), DataType.Int32, false);
			branch.Properties.Simplex(nameof(Branch.Name), DataType.String, 50, true);
			branch.SetKey(nameof(Branch.TenantId), nameof(Branch.BranchId));

			var payment = new DataEntity(prefix, nameof(Payment));
			payment.Properties.Simplex(nameof(Payment.PaymentId), DataType.Int32, false);
			payment.Properties.Simplex(nameof(Payment.TenantId), DataType.Int32, false);
			payment.Properties.Simplex(nameof(Payment.BranchId), DataType.Int32, false);
			payment.Properties.Simplex(nameof(Payment.Status), DataType.Int32, false);
			payment.Properties.Simplex(nameof(Payment.Note), DataType.String, 50, true);
			payment.SetKey(nameof(Payment.PaymentId));
			var navigation = payment.Properties.Complex(nameof(Payment.Branch), branch.Name);
			navigation.Links = [new(navigation, "TenantId"), new(navigation, "BranchId")];

			this.Entity = new DataEntity(prefix, nameof(Refund));
			this.Entity.Properties.Simplex(nameof(Refund.RefundId), DataType.Int32, false);
			this.Entity.Properties.Simplex(nameof(Refund.PaymentId), DataType.Int32, false);
			this.Entity.Properties.Simplex(nameof(Refund.TenantId), DataType.Int32, false);
			this.Entity.Properties.Simplex(nameof(Refund.Amount), DataType.Decimal, false);
			this.Entity.SetKey(nameof(Refund.RefundId));
			navigation = this.Entity.Properties.Complex(nameof(Refund.Payment), payment.Name);
			navigation.Links = [new(navigation, "PaymentId")];
			navigation = this.Entity.Properties.Complex(nameof(Refund.Payments), payment.Name, false, DataAssociationMultiplicity.Many);
			navigation.Links = [new(navigation, "TenantId")];
			_entities = [branch, payment, this.Entity];

			foreach(var entity in _entities)
				Mapping.Entities.Add(entity);

			_access = new DataAccess(prefix);
			((DataProvider)_access.Provider).Multiplexer = new Multiplexer(new DataConnectorTest.DataSourceMocker(this.Driver));
		}

		public DataEntity Entity { get; }
		public DataConnectorTest.DataDriverMocker Driver { get; } = new();
		public Schema Parse(string text) => (Schema)SchemaParser.Instance.Parse(this.Entity.QualifiedName, text, typeof(Refund));
		public async Task<SelectStatement> BuildAsync(Schema schema, Grouping grouping = null)
		{
			using var context = new DataSelectContext(_access, this.Entity.QualifiedName, typeof(Refund), grouping, null, schema, null, null);
			await using var session = context.Session;
			var statements = new List<IStatementBase>();

			await foreach(var statement in new SelectStatementBuilder().BuildAsync(context))
				statements.Add(statement);

			return Assert.IsType<SelectStatement>(Assert.Single(statements));
		}

		public async ValueTask DisposeAsync()
		{
			try { await _access.DisposeAsync(); }
			finally
			{
				foreach(var entity in _entities)
					Mapping.Entities.Remove(entity.QualifiedName);
			}
		}
	}

	private sealed class Multiplexer(IDataSource source) : IDataMultiplexer
	{
		public IDataSourceProvider Provider => throw new NotSupportedException();
		public IDataSourceSelector Selector => throw new NotSupportedException();
		public IDataSource GetSource(IDataAccessContextBase context) => source;
	}

	private sealed class Refund
	{
		public int RefundId { get; set; }
		public int PaymentId { get; set; }
		public int TenantId { get; set; }
		public decimal Amount { get; set; }
		public Payment Payment { get; set; }
		public IList<Payment> Payments { get; set; }
	}

	private sealed class Payment
	{
		public int PaymentId { get; set; }
		public int TenantId { get; set; }
		public int BranchId { get; set; }
		public int Status { get; set; }
		public string Note { get; set; }
		public Branch Branch { get; set; }
	}

	private sealed class Branch
	{
		public int TenantId { get; set; }
		public int BranchId { get; set; }
		public string Name { get; set; }
	}
}
