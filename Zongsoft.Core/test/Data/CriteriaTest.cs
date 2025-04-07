using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

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
		var text = @"name:popeye+createdTime:thisyear+creator.phone:13812345678";
		var criteria = Criteria.Transform(typeof(DummyCriteria), text) as ConditionCollection;

		Assert.NotNull(criteria);
		Assert.Equal(3, criteria.Count);

		var conditions = criteria[0] as ConditionCollection;
		Assert.NotNull(conditions);
		Assert.Equal(2, conditions.Count);
		Assert.IsType<Condition>(conditions[0]);
		Assert.IsType<Condition>(conditions[1]);
		Assert.Equal("Name", ((Condition)conditions[0]).Name);
		Assert.NotNull(((Condition)conditions[0]).Value);
		Assert.True(((Condition)conditions[0]).Value.ToString().Contains("popeye", StringComparison.OrdinalIgnoreCase));
		Assert.Equal("PinYin", ((Condition)conditions[1]).Name);
		Assert.NotNull(((Condition)conditions[1]).Value);
		Assert.True(((Condition)conditions[1]).Value.ToString().Contains("popeye", StringComparison.OrdinalIgnoreCase));

		var condition = criteria[1] as Condition;
		Assert.NotNull(condition);
		Assert.Equal("CreatedTime", condition.Name);
		Assert.IsType<Zongsoft.Data.Range<DateTime>>(condition.Value);
		var range = (Zongsoft.Data.Range<DateTime>)condition.Value;
		Assert.Equal(DateTime.Today.Year, range.Minimum.Value.Year);
		Assert.Equal(1, range.Minimum.Value.Month);
		Assert.Equal(1, range.Minimum.Value.Day);
		Assert.Equal(DateTime.Today.Year, range.Maximum.Value.Year);
		Assert.Equal(12, range.Maximum.Value.Month);
		Assert.Equal(31, range.Maximum.Value.Day);

		condition = criteria[2] as Condition;
		Assert.NotNull(condition);
		Assert.Equal($"{nameof(DummyCriteria.Creator)}.{nameof(UserCriteria.Phone)}", condition.Name);
		Assert.Equal("13812345678", condition.Value);
	}

	public abstract class DummyCriteria : CriteriaBase
	{
		public abstract int? CorporationId { get; set; }
		public abstract short? DepartmentId { get; set; }
		[Condition(ConditionOperator.Like, "Name", "PinYin")]
		public abstract string Name { get; set; }
		public abstract Range<DateTime>? CreatedTime { get; set; }
		public abstract Range<int> Staffs { get; set; }
		public abstract UserCriteria Creator { get; set; }
	}

	public abstract class UserCriteria : CriteriaBase
	{
		public abstract string Phone { get; set; }
		public abstract Range<DateTime> Birthdate { get; set; }
	}
}
