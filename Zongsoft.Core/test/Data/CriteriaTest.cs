using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class CriteriaTest
	{
		[Fact]
		public void Test()
		{
			var criteria = Model.Build<DummyCriteria>();

			Assert.Null(criteria.CorporationId);
			Assert.Null(criteria.DepartmentId);
			Assert.Null(criteria.Name);
			Assert.Null(criteria.CreatedTime);

			Assert.Null(criteria.Transform());

			criteria.CorporationId = 0;
			criteria.DepartmentId = null;
			Assert.NotNull(criteria.CorporationId);
			Assert.Null(criteria.DepartmentId);
			Assert.Equal(0, criteria.CorporationId);

			criteria.CreatedTime = new Range<DateTime>(new DateTime(2010, 1, 1), null);
			Assert.NotNull(criteria.CreatedTime);
			Assert.Equal(new DateTime(2010, 1, 1), criteria.CreatedTime.Value.Minimum);

			criteria.Staffs = Range.Create(50, 100);
			Assert.Equal(50, criteria.Staffs.Minimum);
			Assert.Equal(100, criteria.Staffs.Maximum);

			var conditions = (ConditionCollection)criteria.Transform();
			Assert.NotNull(conditions);
			Assert.NotEmpty(conditions);

			Assert.True(conditions.Match(nameof(DummyCriteria.CreatedTime), condition =>
			{
				Assert.Equal(ConditionOperator.GreaterThanEqual, condition.Operator);
				Assert.IsType<DateTime>(condition.Value);
				Assert.False(Range.IsRange(condition.Value));
			}));

			Assert.True(conditions.Match(nameof(DummyCriteria.Staffs), condition =>
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

		[Fact]
		public void TestExpression()
		{
			var text = @"name:popeye+createdTime:thisyear";
			var conditions = Criteria.Transform(typeof(DummyCriteria), text);

			Assert.NotNull(conditions);
		}

		public abstract class DummyCriteria : CriteriaBase
		{
			public abstract int? CorporationId { get; set; }
			public abstract short? DepartmentId { get; set; }
			[Condition(ConditionOperator.Like, "Name", "PinYin")]
			public abstract string Name { get; set; }
			public abstract Range<DateTime>? CreatedTime { get; set; }
			public abstract Range<int> Staffs { get; set; }
		}
	}
}
