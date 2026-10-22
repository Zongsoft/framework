using System;

using Xunit;

using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.ClickHouse.Tests;

public class InsertStatementTest
{
	[Fact]
	public void InsertContainsAllRowsWithTypedParameters()
	{
		var entity = new DataEntity(null, "Batch");
		var property = entity.Properties.Simplex("Value", DataType.Int32, false);
		var statement = new InsertStatement(entity, null);
		statement.Fields.Add(statement.Table.CreateField(property));

		for(int index = 1; index <= 3; index++)
			statement.Values.Add(statement.Parameters.Add($"p{index}", DataType.Int32));

		var sql = new ClickHouseExpressionVisitor().Visit(statement);
		Assert.Contains("({p1:Int32}),({p2:Int32}),({p3:Int32})", sql);
	}
}
