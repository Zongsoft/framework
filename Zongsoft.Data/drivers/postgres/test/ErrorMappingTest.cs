using System;
using System.Threading.Tasks;

using Npgsql;
using Xunit;

namespace Zongsoft.Data.PostgreSql.Tests;

public class ErrorMappingTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("Key (misleading)=(private-test-value) already exists.")]
	public async Task UniqueViolationPreservesStructuredFieldsWithoutParsingDetailAsync(string detail)
	{
		var original = new PostgresException("unrelated 'quoted' test text", "ERROR", "ERROR", "23505",
			detail: detail, schemaName: "Schema'Name", tableName: "Order'Table", constraintName: "Unique'Name");
		var result = Assert.IsType<DataConstraintException>(await PostgreSqlDriver.Instance.OnErrorAsync(null, original, TestContext.Current.CancellationToken));
		Assert.Equal(PostgreSqlDriver.NAME, result.DriverName);
		Assert.Equal(23505, result.Code);
		Assert.Equal(DataConstraintKind.Unique, result.Kind);
		Assert.Equal(original.ConstraintName, result.Name);
		Assert.Equal(original.TableName, result.Principal.Name);
		Assert.Empty(result.Principal.Fields);
		Assert.Null(result.Value);
		Assert.Null(result.Foreigner);
		Assert.Same(original, result.InnerException);
	}

	[Fact]
	public async Task ForeignKeyViolationPreservesConflictIdentityAsync()
	{
		var original = new PostgresException("foreign key test", "ERROR", "ERROR", "23503", constraintName: "FK_Child_Parent");
		var result = Assert.IsType<DataConflictException>(await PostgreSqlDriver.Instance.OnErrorAsync(null, original, TestContext.Current.CancellationToken));
		Assert.Equal(PostgreSqlDriver.NAME, result.DriverName);
		Assert.Equal(23503, result.Code);
		Assert.Equal(original.ConstraintName, result.Name);
		Assert.Null(result.Value);
		Assert.Same(original, result.InnerException);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("DisplayName")]
	public async Task StringTruncationPreservesColumnAndOriginalExceptionAsync(string column)
	{
		var original = new PostgresException("unrelated test text", "ERROR", "ERROR", "22001", columnName: column);
		var result = Assert.IsType<DataArgumentException>(await PostgreSqlDriver.Instance.OnErrorAsync(null, original, TestContext.Current.CancellationToken));
		Assert.Equal(column, result.Name);
		Assert.Null(result.Value);
		Assert.Same(original, result.InnerException);
	}

	[Theory]
	[InlineData("23502")]
	[InlineData("42P01")]
	public async Task UnmappedSqlStateReturnsOriginalExceptionAsync(string state)
	{
		var original = new PostgresException("unmapped test", "ERROR", "ERROR", state);
		Assert.Same(original, await PostgreSqlDriver.Instance.OnErrorAsync(null, original, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task NonPostgresErrorsReturnOriginalExceptionAsync()
	{
		var provider = new NpgsqlException("provider test");
		var ordinary = new InvalidOperationException("ordinary test");
		Assert.Same(provider, await PostgreSqlDriver.Instance.OnErrorAsync(null, provider, TestContext.Current.CancellationToken));
		Assert.Same(ordinary, await PostgreSqlDriver.Instance.OnErrorAsync(null, ordinary, TestContext.Current.CancellationToken));
	}
}
