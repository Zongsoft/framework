using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class ConditionalTest
	{
		[Fact]
		public void Test()
		{
			var conditional = Model.Build<IDummyConditional>();

			Assert.Null(conditional.CorporationId);
			Assert.Null(conditional.DepartmentId);
			Assert.Null(conditional.Name);
			Assert.Null(conditional.CreatedTime);

			Assert.Null(conditional.ToCondition());
			Assert.Null(conditional.ToConditions());

			conditional.CorporationId = 0;
			conditional.DepartmentId = null;
			Assert.NotNull(conditional.CorporationId);
			Assert.Null(conditional.DepartmentId);
			Assert.Equal(0, conditional.CorporationId);

			conditional.CreatedTime = new Range<DateTime>(new DateTime(2010, 1, 1), null);
			Assert.NotNull(conditional.CreatedTime);
			Assert.Equal(new DateTime(2010, 1, 1), conditional.CreatedTime.Value.Minimum);

			conditional.Staffs = Range.Create(50, 100);
			Assert.Equal(50, conditional.Staffs.Minimum);
			Assert.Equal(100, conditional.Staffs.Maximum);

			var conditions = conditional.ToConditions();
			Assert.NotNull(conditions);
			Assert.NotEmpty(conditions);

			Assert.True(conditions.Match(nameof(IDummyConditional.CreatedTime), condition =>
			{
				Assert.Equal(ConditionOperator.GreaterThanEqual, condition.Operator);
				Assert.IsType<DateTime>(condition.Value);
				Assert.False(Range.IsRange(condition.Value));
			}));

			Assert.True(conditions.Match(nameof(IDummyConditional.Staffs), condition =>
			{
				Assert.Equal(ConditionOperator.Between, condition.Operator);
				Assert.IsType<Range<int>>(condition.Value);
				Assert.True(Range.IsRange(condition.Value));
				Assert.False(Range.IsEmpty(condition.Value));

				var range = (Range<int>)condition.Value;
				Assert.Equal(50, range.Minimum);
				Assert.Equal(100, range.Maximum);
			}));
		}

		public interface IDummyConditional : IModel
		{
			int? CorporationId
			{
				get; set;
			}

			short? DepartmentId
			{
				get; set;
			}

			[Conditional("Name", "PinYin")]
			string Name
			{
				get; set;
			}

			Range<DateTime>? CreatedTime
			{
				get; set;
			}

			Range<int> Staffs
			{
				get; set;
			}
		}
	}
}
