using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

public class ConditionMatchingTest
{
	[Fact]
	public void MatchStopsAtFirstNestedCondition()
	{
		var first = Condition.In("Id", new[] { 17, 23 });
		var criteria = new ConditionCollection(ConditionCombination.Or)
		{
			new ConditionCollection(ConditionCombination.And) { first },
			new Condition("Id", 31),
		};
		var received = new List<Condition>();

		Assert.True(criteria.Match("ID", received.Add));
		Assert.Same(first, Assert.Single(received));
		Assert.Same(first, criteria.Find("id"));
		Assert.Same(first, ConditionExtension.Find(criteria, "id"));
		Assert.Equal(ConditionOperator.In, first.Operator);
		Assert.Equal(new[] { 17, 23 }, Assert.IsType<int[]>(first.Value));
	}

	[Fact]
	public void NestedOnlyMatchesAreReported()
	{
		var condition = new Condition("Id", 17);
		var criteria = new ConditionCollection(ConditionCombination.And)
		{
			new ConditionCollection(ConditionCombination.Or)
			{
				new ConditionCollection(ConditionCombination.And) { condition },
			},
		};

		Assert.True(criteria.Contains("id"));
		Assert.True(criteria.Match("id"));
		Assert.Equal(1, criteria.Matches("id"));
		Assert.Same(condition, criteria.Find("id"));
		Assert.Same(condition, Assert.Single(criteria.FindAll("id")));
		Assert.False(criteria.Match("Missing"));
		Assert.Equal(0, criteria.Matches("Missing"));
	}

	[Fact]
	public void AllMatchesPreserveTraversalOrderAndCounts()
	{
		var criteria = Create();
		var received = new List<Condition>();
		Assert.Equal(5, criteria.Matches("ID", received.Add));
		Assert.Equal(new object[] { 1, 2, 3, 4, 5 }, received.Select(condition => condition.Value));
		Assert.Equal(received, criteria.FindAll("Id"));
		Assert.Equal(received, ConditionExtension.FindAll(criteria, "Id"));

		var depths = new List<int>();
		Assert.Equal(5, criteria.Matches("Id", 0, 0, (_, depth) => depths.Add(depth)));
		Assert.Equal(new[] { 0, 0, 1, 2, 0 }, depths);
	}

	[Theory]
	[InlineData(-1, 5)]
	[InlineData(0, 5)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	[InlineData(3, 3)]
	[InlineData(4, 4)]
	[InlineData(5, 5)]
	[InlineData(6, 5)]
	public void MaximumCountAppliesToTheWholeTree(int limit, int expected)
	{
		var received = new List<Condition>();
		var count = Create().Matches("Id", limit, 0, (condition, _) => received.Add(condition));
		Assert.Equal(expected, count);
		Assert.Equal(Enumerable.Range(1, expected).Cast<object>(), received.Select(condition => condition.Value));
	}

	[Theory]
	[InlineData(-1, new[] { 1, 2, 3, 4, 5 })]
	[InlineData(0, new[] { 1, 2, 3, 4, 5 })]
	[InlineData(1, new[] { 1, 2, 5 })]
	[InlineData(2, new[] { 1, 2, 3, 5 })]
	[InlineData(3, new[] { 1, 2, 3, 4, 5 })]
	public void DepthCountsChangesInCombination(int depth, int[] expected)
	{
		var criteria = Create();
		Assert.Equal(expected.Cast<object>(), criteria.FindAll("Id", depth).Select(condition => condition.Value));
		var received = new List<Condition>();
		Assert.Equal(expected.Length, criteria.Matches("Id", 0, depth, (condition, _) => received.Add(condition)));
		Assert.Equal(expected.Cast<object>(), received.Select(condition => condition.Value));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Missing")]
	public void UnmatchedNamesDoNotInvokeCallbacks(string name)
	{
		var criteria = Create();
		Assert.False(criteria.Contains(name));
		Assert.False(criteria.Match(name, _ => Assert.Fail("Unexpected callback.")));
		Assert.Equal(0, criteria.Matches(name, _ => Assert.Fail("Unexpected callback.")));
		Assert.Null(criteria.Find(name));
		Assert.Empty(ConditionExtension.FindAll(criteria, name));
		Assert.Equal(0, criteria.Matches(name, 0, 0, (_, _) => Assert.Fail("Unexpected callback.")));
		Assert.Equal(0, ConditionExtension.Matches(null, name, 0, 0, null));
	}

	private static ConditionCollection Create() => new(ConditionCombination.And)
	{
		new Condition("Id", 1),
		new ConditionCollection(ConditionCombination.And) { new Condition("Id", 2) },
		new ConditionCollection(ConditionCombination.Or)
		{
			new Condition("Id", 3),
			new ConditionCollection(ConditionCombination.And) { new Condition("Id", 4) },
		},
		new Condition("Id", 5),
	};

	[Fact]
	public void QualifiedNamesAndSubqueriesKeepTheirOwnScope()
	{
		var direct = Condition.Equal("UserId", 17U);
		var related = Condition.Equal("Order.UserId", 23U);
		var criteria = direct & related & Condition.Exists("Details", Condition.Equal("UserId", 31U));
		Assert.Same(direct, Assert.Single(criteria.FindAll("USERID")));
		Assert.Same(related, criteria.Find("order.userid"));
		Assert.False(criteria.Contains("Id"));
		Assert.Equal(1, criteria.Matches("UserId"));
	}

	[Fact]
	public void SingleAndCollectionLookupsUseTheSameFieldName()
	{
		ICondition condition = new Condition(" Id ", 17);
		var collection = new ConditionCollection(ConditionCombination.And) { condition };
		Assert.True(condition.Match("id"));
		Assert.True(collection.Match("id"));
		Assert.Same(condition, ConditionExtension.Find(condition, "id"));
		Assert.Same(condition, collection.Find("id"));
		Assert.Same(condition, Assert.Single(collection.FindAll("id")));
		Assert.True(collection.TryGetValue<int>("id", out var value));
		Assert.Equal(17, value);
		var replacement = Condition.Equal("Id", 23);
		Assert.Same(collection, collection.Replace("id", (matched, depth) =>
		{
			Assert.Same(condition, matched);
			Assert.Equal(0, depth);
			return replacement;
		}, out var count));
		Assert.Equal(1, count);
		Assert.Same(replacement, collection.Find("Id"));
	}

	[Fact]
	public void MachineHistoryPatternEditsTheOriginalCondition()
	{
		var item = Condition.Equal("ItemId", 17UL);
		var status = Condition.Equal("Status", (byte)1);
		var criteria = Condition.Equal("TenantId", 8U) & (item | status);
		Assert.True(criteria.Contains("ItemId"));
		criteria.Find("ItemId").Operator = ConditionOperator.GreaterThan;
		criteria.Find("ItemId").Value = 0UL;
		Assert.Same(item, criteria.Find("ItemId"));
		Assert.Equal(ConditionOperator.GreaterThan, item.Operator);
		Assert.Equal(0UL, item.Value);
		Assert.Same(status, criteria.Find("Status"));
	}

	[Fact]
	public void OrderDetailPatternRetainsCollectionConditions()
	{
		var orders = Condition.In("OrderId", new ulong[] { 17, 23 });
		var criteria = orders & (Condition.Equal("Status", 1) | Condition.Equal("Status", 2));
		Assert.Same(orders, criteria.Find("OrderId", 1));
		Assert.Equal(ConditionOperator.In, orders.Operator);
		Assert.False(criteria.TryGetValue<ulong>("OrderId", out _));
		Assert.Null(criteria.Find("Status", 1));
	}

	[Fact]
	public void CallbackExceptionsStopMatchingAndPropagate()
	{
		var criteria = Create();
		var expected = new InvalidOperationException("Consumer validation failed.");
		var calls = 0;
		var actual = Assert.Throws<InvalidOperationException>(() => criteria.Matches("Id", condition =>
		{
			if(++calls == 3)
				throw expected;
		}));
		Assert.Same(expected, actual);
		Assert.Equal(3, calls);
		Assert.Equal(5, criteria.FindAll("Id").Length);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(61)]
	[InlineData(103)]
	public void NestedTreesAgreeWithDepthFirstTraversal(int seed)
	{
		var random = new Random(seed);
		for(var iteration = 0; iteration < 60; iteration++)
		{
			var criteria = CreateTree(random, 4);
			var expected = new List<(Condition Condition, int Depth)>();
			var pending = new Stack<(ICondition Node, ConditionCombination? Parent, int Depth)>();
			pending.Push((criteria, null, 0));

			while(pending.TryPop(out var entry))
			{
				if(entry.Node is Condition condition)
					expected.Add((condition, entry.Depth));
				else if(entry.Node is ConditionCollection collection)
				{
					var depth = entry.Depth + (entry.Parent.HasValue && entry.Parent != collection.Combination ? 1 : 0);
					for(var index = collection.Count - 1; index >= 0; index--)
						pending.Push((collection[index], collection.Combination, depth));
				}
			}

			foreach(var name in new[] { "Id", "TenantId", "Missing" })
			{
				var all = expected.Where(entry => entry.Condition.Name == name).Select(entry => entry.Condition).ToArray();
				Assert.Equal(all.Length > 0, criteria.Contains(name));
				Assert.Same(all.FirstOrDefault(), criteria.Find(name));
				Assert.Equal(all, criteria.FindAll(name));
				Assert.Equal(all.Length, criteria.Matches(name));

				foreach(var depth in new[] { 0, 1, 2, 3 })
				{
					var bounded = expected.Where(entry => entry.Condition.Name == name && (depth == 0 || entry.Depth < depth)).ToArray();
					Assert.Equal(bounded.Select(entry => entry.Condition), criteria.FindAll(name, depth));

					foreach(var limit in new[] { 0, 1, 2, 5 })
					{
						var selected = limit == 0 ? bounded : bounded.Take(limit).ToArray();
						var actual = new List<(Condition, int)>();
						Assert.Equal(selected.Length, criteria.Matches(name, limit, depth, (condition, level) => actual.Add((condition, level))));
						Assert.Equal(selected, actual);
					}
				}
			}
		}
	}

	private static ConditionCollection CreateTree(Random random, int remaining)
	{
		var result = new ConditionCollection(random.Next(2) == 0 ? ConditionCombination.And : ConditionCombination.Or);
		var count = random.Next(5);

		for(var index = 0; index < count; index++)
		{
			result.Add(remaining > 0 && random.Next(2) == 0 ? CreateTree(random, remaining - 1) :
				new Condition(random.Next(2) == 0 ? "Id" : "TenantId", random.Next(100), (ConditionOperator)random.Next(4)));
		}

		return result;
	}
}
