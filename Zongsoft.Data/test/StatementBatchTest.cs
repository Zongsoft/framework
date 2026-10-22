using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

public class StatementBatchTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void SplitPreservesSharedParametersAndRestoresStatement(int kind)
	{
		var entity = new DataEntity(null, "Batch");
		IBatchStatement statement = kind switch
		{
			0 => new InsertStatement(entity, null) { ParameterLimit = 3 },
			1 => new UpsertStatement(entity, null) { ParameterLimit = 3 },
			_ => new CustomStatement(entity),
		};
		var fields = statement.Fields;
		var values = statement.Values;
		fields.Add(statement.Table.CreateField("Value"));
		fields.Add(statement.Table.CreateField("Constant"));
		var shared = statement.Parameters.Add("shared", DataType.Int32);

		for(int index = 0; index < 5; index++)
		{
			values.Add(statement.Parameters.Add($"p{index}", DataType.Int32));
			values.Add(Expression.Constant(index));
		}

		var original = values.ToArray();
		var parameters = statement.Parameters.ToArray();
		Assert.Equal(2, StatementBatch.GetSize(statement));
		var offset = 0;

		foreach(var batch in StatementBatch.Split(statement, new[] { 0, 1, 2, 3, 4 }))
		{
			var rows = Assert.IsAssignableFrom<IList>(batch);
			Assert.Equal(Enumerable.Range(offset, rows.Count), rows.Cast<int>());
			Assert.Equal(original.Skip(offset * 2).Take(rows.Count * 2), values);
			Assert.Equal(rows.Count + 1, statement.Parameters.Count);
			Assert.Same(shared, statement.Parameters["shared"]);
			offset += rows.Count;
		}

		Assert.Equal(5, offset);
		Assert.Equal(original, values);
		Assert.Equal(parameters, statement.Parameters);
	}

	[Fact]
	public void SplitRestoresStatementWhenEnumerationStops()
	{
		var statement = new InsertStatement(new DataEntity(null, "Batch"), null) { BatchSize = 1 };
		statement.Fields.Add(statement.Table.CreateField("Value"));
		statement.Values.Add(statement.Parameters.Add("first", DataType.Int32));
		statement.Values.Add(statement.Parameters.Add("second", DataType.Int32));
		var original = statement.Values.ToArray();
		var parameters = statement.Parameters.ToArray();

		foreach(var batch in StatementBatch.Split(statement, new[] { 17, 23 }))
		{
			Assert.Equal(17, Assert.IsType<int>(batch));
			Assert.Single(statement.Values);
			Assert.Same(parameters[0], Assert.Single(statement.Parameters));
			break;
		}

		Assert.Equal(original, statement.Values);
		Assert.Equal(parameters, statement.Parameters);
	}

	[Fact]
	public void SingleRowCannotExceedParameterLimit()
	{
		var statement = new InsertStatement(new DataEntity(null, "Batch"), null) { ParameterLimit = 1 };
		statement.Fields.Add(statement.Table.CreateField("Value"));
		statement.Values.Add(statement.Parameters.Add("value", DataType.Int32));
		statement.Parameters.Add("shared", DataType.Int32);
		Assert.Throws<ArgumentOutOfRangeException>("statement", () => StatementBatch.GetSize(statement));
	}

	private sealed class CustomStatement(IDataEntity entity) : StatementBase(entity, null), IBatchStatement
	{
		public int BatchSize => 256;
		public int ParameterLimit => 3;
		public SchemaMember Schema { get; set; }
		public ReturningClause Returning { get; set; }
		public IList<FieldIdentifier> Fields { get; } = new List<FieldIdentifier>();
		public IList<IExpression> Values { get; } = new List<IExpression>();
	}
}
