using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class ConditionCollectionTest
	{
		[Fact]
		public void TestOperator()
		{
			var and1 = new Condition("a", 1) & new Condition("b", 2);
			Assert.Equal(ConditionCombination.And, and1.Combination);
			Assert.Equal(2, and1.Count);

			var and2 = new Condition("a", 1) & new Condition("b", 2) & new Condition("c", 3);
			Assert.Equal(ConditionCombination.And, and2.Combination);
			Assert.Equal(3, and2.Count);

			var or1 = new Condition("a", 1) | new Condition("b", 2);
			Assert.Equal(ConditionCombination.Or, or1.Combination);
			Assert.Equal(2, or1.Count);

			var or2 = new Condition("a", 1) | new Condition("b", 2) | new Condition("c", 3);
			Assert.Equal(ConditionCombination.Or, or2.Combination);
			Assert.Equal(3, or2.Count);

			ConditionCollection cs;

			//测试加法运算符

			cs = and1 + new Condition("key", "value");
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(3, cs.Count);

			cs = new Condition("key", "value") + and2;
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(4, cs.Count);

			//开始条件集合的并运算

			cs = and1 & and2;
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(5, cs.Count);

			cs = and1 & or2;
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(3, cs.Count);

			cs = or1 & and2;
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(4, cs.Count);

			cs = or1 & or2;
			Assert.Equal(ConditionCombination.And, cs.Combination);
			Assert.Equal(2, cs.Count);


			//开始条件集合的或运算

			cs = or1 | or2;
			Assert.Equal(ConditionCombination.Or, cs.Combination);
			Assert.Equal(5, cs.Count);

			cs = and1 | or2;
			Assert.Equal(ConditionCombination.Or, cs.Combination);
			Assert.Equal(4, cs.Count);

			cs = or1 | and2;
			Assert.Equal(ConditionCombination.Or, cs.Combination);
			Assert.Equal(3, cs.Count);

			cs = and1 | and2;
			Assert.Equal(ConditionCombination.Or, cs.Combination);
			Assert.Equal(2, cs.Count);

		}

		[Fact]
		public void TestFlatten()
		{
			var conditions = ConditionCollection.And
			(
				Condition.Equal("Field_1", "F1.Value"),
				Condition.Equal("Field_2", 100),
				ConditionCollection.And
				(
				),
				Condition.Equal("Field_3", DateTime.Now),
				ConditionCollection.Or
				(
					Condition.Equal("Field_11", "F11.Value")
				),
				Condition.GreaterThan("Field_4", 150),
				Condition.LessThan("Field_5", 5000.67),
				ConditionCollection.And
				(
					Condition.Equal("Field_21", "F21.Value")
				),
				ConditionCollection.Or
				(
					Condition.Equal("Field_31", "F31.Value"),
					Condition.Equal("Field_32", "F32.Value")
				),
				ConditionCollection.And
				(
					Condition.Equal("Field_41", "F41.Value"),
					Condition.Equal("Field_42", "F42.Value"),
					Condition.Equal("Field_43", "F43.Value")
				),
				ConditionCollection.And
				(
					Condition.Equal("Field_51", "F51.Value"),
					Condition.Equal("Field_52", "F52.Value"),
					ConditionCollection.And
					(
					),
					ConditionCollection.And
					(
						Condition.Equal("Field_501", "F501.Value"),
						Condition.Equal("Field_502", "F502.Value")
					),
					ConditionCollection.Or
					(
						Condition.Equal("Field_503", "F503.Value")
					),
					ConditionCollection.Or
					(
						Condition.Equal("Field_504", "F504.Value"),
						Condition.Equal("Field_505", "F505.Value")
					),
					ConditionCollection.Or
					(
						Condition.Equal("Field_506", "F506.Value"),
						ConditionCollection.Or
						(
							Condition.Equal("Field_510", "F510.Value")
						)
					),
					Condition.Equal("Field_53", "F53.Value")
				)
			);

			conditions.Flatten();

			//Assert.Equal(20, conditions.Count);
		}
	}
}
