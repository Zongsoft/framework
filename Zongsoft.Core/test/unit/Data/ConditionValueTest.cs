using System;
using System.Collections;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ConditionValueTest
{
	public static TheoryData<object, object> Constants => new()
	{
		{ 17, 17 },
		{ Operand.Constant(17), 17 },
		{ Operand.Constant(17U), 17U },
		{ Operand.Constant(17UL), 17UL },
		{ Operand.Constant(17.25m), 17.25m },
		{ Operand.Constant(true), true },
		{ Operand.Constant("Outputer"), "Outputer" },
		{ Operand.Constant(DayOfWeek.Friday), DayOfWeek.Friday },
		{ Operand.Constant(Guid.Empty), Guid.Empty },
		{ Operand.Constant(new DateTime(2026, 10, 25)), new DateTime(2026, 10, 25) },
		{ null, null },
		{ DBNull.Value, null },
		{ Operand.Constant<string>(null), null },
		{ Operand.Constant<int?>(null), null },
		{ Operand.Constant(DBNull.Value), null },
	};

	[Theory]
	[MemberData(nameof(Constants))]
	public void ConstantsAreUnwrappedWithoutChangingConditions(object input, object expected)
	{
		ICondition criteria = new Condition("BranchId", input);
		Assert.True(criteria.TryGetValue("branchid", out var value));
		Assert.Equal(expected, value);
		Assert.Same(input, criteria.Find("BranchId").Value);
		Assert.Equal(ConditionOperator.Equal, criteria.Find("BranchId").Operator);
	}

	public static TheoryData<ICondition> Unresolved => new()
	{
		null,
		new ConditionCollection(ConditionCombination.And),
		Condition.Equal("OtherId", 17),
		Condition.In("Id", new[] { 17 }),
		Condition.In("Id", new[] { 17, 23 }),
		Condition.NotIn("Id", new[] { 17, 23 }),
		Condition.Between("Id", 17, 23),
		Condition.GreaterThan("Id", 17),
		Condition.NotEqual("Id", 17),
		Condition.Like("Id", "17"),
		new Condition("Id", Range.Create(17, 23)),
		new Condition("Id", Operand.Constant(Range.Create(17, 23))),
		new Condition("Id", new[] { 17, 23 }),
		new Condition("Id", Operand.Constant(new[] { 17, 23 })),
		new Condition("Id", new UnreadableCollection()),
		Condition.Equal("Id", Operand.Field("OtherId")),
		Condition.Equal("Id", Operand.Field("OtherId") + 1),
		Condition.Equal("Id", Operand.Function("GetId")),
		Condition.Equal("Id", Operand.Constant(Operand.Field("OtherId"))),
		Condition.Equal(Operand.Field("Id") + 1, Operand.Constant(17)),
		Condition.Exists("Details", Condition.Equal("Id", 17)),
		Condition.Equal("Id", 17) | Condition.Equal("Id", 23),
		new ConditionCollection(ConditionCombination.Or) { Condition.Equal("Id", 17) },
		Condition.Equal("Name", "Building") & (Condition.Equal("Id", 17) | Condition.Equal("Id", 23)),
	};

	[Theory]
	[MemberData(nameof(Unresolved))]
	public void NonConstantConditionsDoNotProduceValues(ICondition criteria)
	{
		Assert.False(criteria.TryGetValue("Id", out var value));
		Assert.Null(value);
		Assert.False(criteria.TryGetValue<uint>("Id", out var identifier));
		Assert.Equal(0U, identifier);
	}

	[Fact]
	public void AndScopeKeepsIndependentValuesAndRejectsConflicts()
	{
		var criteria = new ConditionCollection(ConditionCombination.And)
		{
			Condition.In("BranchId", new uint[] { 17, 23 }),
			new ConditionCollection(ConditionCombination.And)
			{
				Condition.Equal("BranchId", 17U),
				new Condition("BranchId", 17U),
			},
			Condition.Equal("BranchId", 23U) | Condition.Equal("BranchId", 31U),
			Condition.Equal("TenantId", 8U),
		};
		Assert.True(criteria.TryGetValue<uint>("BranchId", out var branchId));
		Assert.Equal(17U, branchId);
		Assert.True(criteria.TryGetValue<uint>("TenantId", out var tenantId));
		Assert.Equal(8U, tenantId);

		criteria.Add(Condition.Equal("BranchId", 31U));
		Assert.False(criteria.TryGetValue("BranchId", out var value));
		Assert.Null(value);
		Assert.True(criteria.TryGetValue<uint>("TenantId", out tenantId));
		Assert.Equal(8U, tenantId);
	}

	[Theory]
	[InlineData(17, 17, true)]
	[InlineData(17, 23, false)]
	[InlineData(17, 17L, false)]
	[InlineData(17, "17", false)]
	[InlineData("abc", "ABC", false)]
	[InlineData(null, null, true)]
	[InlineData(null, 17, false)]
	public void CandidateComparisonDoesNotGuessDatabaseCoercion(object first, object second, bool expected)
	{
		var criteria = new Condition("Id", first) & new Condition("Id", second);
		Assert.Equal(expected, criteria.TryGetValue("Id", out var value));
		Assert.Equal(expected ? first : null, value);
	}

	[Theory]
	[InlineData("17", true, 17U)]
	[InlineData("4294967295", true, uint.MaxValue)]
	[InlineData("4294967296", false, 0U)]
	[InlineData("-1", false, 0U)]
	[InlineData("Branch", false, 0U)]
	public void TypedValuesUseFrameworkConversion(string input, bool expected, uint identifier)
	{
		ICondition criteria = Condition.Equal("Id", input);
		Assert.Equal(expected, criteria.TryGetValue<uint>("Id", out var value));
		Assert.Equal(identifier, value);
	}

	[Fact]
	public void NullDoesNotBecomeADefaultNonNullableIdentifier()
	{
		ICondition criteria = new Condition("Id", null);
		Assert.True(criteria.TryGetValue("Id", out var value));
		Assert.Null(value);
		Assert.True(criteria.TryGetValue<uint?>("Id", out var nullable));
		Assert.Null(nullable);
		Assert.True(criteria.TryGetValue<string>("Id", out var text));
		Assert.Null(text);
		Assert.False(criteria.TryGetValue<uint>("Id", out var identifier));
		Assert.Equal(0U, identifier);
		Assert.False(criteria.TryGetValue("Missing", out value));
	}

	[Fact]
	public void BinaryValuesRetainIdentity()
	{
		var bytes = new byte[] { 1, 2, 3 };
		ICondition criteria = new Condition("Key", bytes);
		Assert.True(criteria.TryGetValue<byte[]>("Key", out var value));
		Assert.Same(bytes, value);
	}

	[Fact]
	public void YuanshanGatewayMetricPatternExtractsEachCompositeIdentifier()
	{
		var criteria = Condition.Equal("EdgeEndId", 0x80000001U) &
			Condition.Equal("GatewayId", 0x80000002U) &
			Condition.Equal("MetricId", 0x100000003UL) &
			Condition.Between("Timestamp", DateTime.Today, DateTime.Today.AddDays(1));
		Assert.True(criteria.TryGetValue<uint>("EdgeEndId", out var edgeEndId));
		Assert.True(criteria.TryGetValue<uint>("GatewayId", out var gatewayId));
		Assert.True(criteria.TryGetValue<ulong>("MetricId", out var metricId));
		Assert.Equal(0x80000001U, edgeEndId);
		Assert.Equal(0x80000002U, gatewayId);
		Assert.Equal(0x100000003UL, metricId);
		Assert.False(criteria.TryGetValue<DateTime>("Timestamp", out _));
	}

	[Fact]
	public void YuanshanOutputerCategoryPatternConvertsEnumValues()
	{
		ICondition criteria = Condition.Equal("CategoryKind", "Friday");
		Assert.True(criteria.TryGetValue<DayOfWeek>("CategoryKind", out var category));
		Assert.Equal(DayOfWeek.Friday, category);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("Id2")]
	public void MissingNamesReturnFalse(string name)
	{
		ICondition criteria = Condition.Equal("Id", 17);
		Assert.False(criteria.TryGetValue(name, out var value));
		Assert.Null(value);
	}

	[Fact]
	public void ConcurrentReadsDoNotShareCandidateState()
	{
		var criteria = Condition.Equal("BranchId", 17U) & Condition.Equal("OutputerId", 23U);
		Parallel.For(0, 100, index =>
		{
			var name = index % 2 == 0 ? "BranchId" : "OutputerId";
			Assert.True(criteria.TryGetValue<uint>(name, out var value));
			Assert.Equal(index % 2 == 0 ? 17U : 23U, value);
		});
	}

	private sealed class UnreadableCollection : IEnumerable
	{
		public IEnumerator GetEnumerator() => throw new InvalidOperationException("Condition values must not be enumerated.");
	}
}
