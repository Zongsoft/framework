using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

public class SchemaTest
{
	[Theory]
	[InlineData(" ,  ,  *, ,,!a, !c, !f, c", "b,d,e,c")]
	[InlineData("*, !, a, !b, c, a", "a,c")]
	[InlineData("a,,,b,", "a,b")]
	public void Parse_TolerantCompatibilityExpressions_ReturnsExpectedMembers(string expression, string expected)
	{
		var members = Parser.Instance.ParseExpression(expression);
		Assert.Equal(expected, string.Join(',', members?.Select(member => member.Name) ?? []));
	}

	[Fact]
	public void Parse_DottedAndBracedSyntax_ProduceEquivalentTree()
	{
		var dotted = new TestSchema("Department.Manager.Name,Department.Manager.Gender", "dotted");
		var braced = new TestSchema("Department{Manager{Name,Gender}}", "braced");
		var mixed = new TestSchema("Department.Manager{Name,Gender}", "mixed");
		var inverseMixed = new TestSchema("Department{Manager.Name,Manager.Gender}", "inverse-mixed");

		Assert.Equal(braced.ToString(), dotted.ToString());
		Assert.Equal(braced.ToString(), mixed.ToString());
		Assert.Equal(braced.ToString(), inverseMixed.ToString());
		Assert.Equal("Department{Manager{Name,Gender}}", dotted.ToString());
	}

	[Fact]
	public void Parse_DottedSyntax_MergesAndExcludesInOrder()
	{
		var schema = new TestSchema(
			"Department.Manager.Name,Department.Manager.Secret,!Department.Manager.Secret,Department.Manager.Gender,!Department.Manager.Name,Department.Manager.Name",
			"ordered");

		Assert.True(schema.Contains("Department.Manager.Gender"));
		Assert.True(schema.Contains("Department.Manager.Name"));
		Assert.False(schema.Contains("Department.Manager.Secret"));
		Assert.Equal("Department{Manager{Gender,Name}}", schema.ToString());
	}

	[Fact]
	public void Parse_DottedSyntax_AllowsWhitespaceAroundPeriod()
	{
		var schema = new TestSchema(" Department . Manager . Name ", "spaced");

		Assert.True(schema.Contains("Department.Manager.Name"));
		Assert.Equal("Department{Manager{Name}}", schema.ToString());
	}

	[Theory]
	[InlineData(".Root")]
	[InlineData("Root.")]
	[InlineData("Root..Child")]
	[InlineData("Root.*.Leaf")]
	[InlineData("Root.*:10")]
	[InlineData("!Root.*")]
	[InlineData("Root.!Child")]
	[InlineData("Root:10.Child")]
	[InlineData("Root(Name).Child")]
	[InlineData("!*")]
	[InlineData("Root{!*}")]
	public void Parse_InvalidDottedExpression_ThrowsSchemaArgument(string expression)
	{
		var exception = Assert.Throws<DataArgumentException>(() => Parser.Instance.ParseExpression(expression));
		Assert.Equal("$schema", exception.Name);
		Assert.Matches(@"\d+[)）][.。]$", exception.Message);
	}

	[Fact]
	public void Parse_NestedLimitsAndSorting_PreservesModifiersAndLastSorting()
	{
		var members = Parser.Instance.ParseExpression("Forums:20,Users:10(~Created,+Name,-Name,Code){Profile(Name){Avatar}}");

		Assert.Equal(2, members.Count);
		Assert.Equal(20, members["Forums"].Limit);

		var users = members["Users"];
		Assert.Equal(10, users.Limit);
		Assert.Collection(users.Sortings,
			sorting => { Assert.Equal("Created", sorting.Name); Assert.Equal(SortingMode.Descending, sorting.Mode); },
			sorting => { Assert.Equal("Name", sorting.Name); Assert.Equal(SortingMode.Descending, sorting.Mode); },
			sorting => { Assert.Equal("Code", sorting.Name); Assert.Equal(SortingMode.Ascending, sorting.Mode); });
		Assert.Equal("Users.Profile.Avatar", users.Children["Profile"].Children["Avatar"].FullPath);
	}

	[Fact]
	public void Parse_ExplicitAscendingSortingPrefix_EqualsUnprefixedSorting()
	{
		var prefixed = new TestSchema("Users(+Created,+Name)", "prefixed");
		var unprefixed = new TestSchema("Users(Created,Name)", "unprefixed");

		Assert.Equal(unprefixed.ToString(), prefixed.ToString());
		Assert.Equal("Users(Created,Name)", prefixed.ToString());
		Assert.All(prefixed.Members["Users"].Sortings, sorting => Assert.Equal(SortingMode.Ascending, sorting.Mode));
	}

	[Theory]
	[InlineData("Users(!Name)")]
	[InlineData("Users(Name,!Created)")]
	public void Parse_LegacyExclamationSortingPrefix_ThrowsSchemaArgument(string expression)
	{
		var exception = Assert.Throws<DataArgumentException>(() => Parser.Instance.ParseExpression(expression));
		Assert.Equal("$schema", exception.Name);
		Assert.Matches(@"\d+[)）][.。]$", exception.Message);
	}

	[Fact]
	public void Parse_LimitBeforeClosingBrace_PreservesLimit()
	{
		var members = Parser.Instance.ParseExpression("Root{Children:10}");
		var limit = members["Root"].Children["Children"].Limit;

		Assert.Equal(10, limit);
	}

	[Theory]
	[InlineData("Root", 0)]
	[InlineData("Root:9", 9)]
	[InlineData("Root:0", 0)]
	[InlineData("Root:*", 0)]
	public void Parse_LimitVariants_PreserveTheirMeaning(string expression, int expected)
	{
		Assert.Equal(expected, Parser.Instance.ParseExpression(expression)["Root"].Limit);
	}

	[Fact]
	public void Parse_UnlimitedLimit_ClearsExistingLimit()
	{
		var member = Parser.Instance.ParseExpression("Root:9,Root:*")["Root"];
		Assert.Equal(0, member.Limit);
	}

	[Theory]
	[InlineData("Users(1Name)")]
	[InlineData("Users(~)")]
	[InlineData("Users(-)")]
	[InlineData("Users(+)")]
	[InlineData("Users:999999999999999999999")]
	[InlineData("1Users")]
	[InlineData("Users:")]
	[InlineData("Users:?")]
	[InlineData("Users:-1")]
	[InlineData("Users:2/")]
	[InlineData("Users:2/10")]
	[InlineData("Users:2/?")]
	[InlineData("Users()")]
	[InlineData("Users(Name,)")]
	[InlineData("Users{Profile")]
	[InlineData("Users{Name}}")]
	public void Parse_InvalidExpression_ThrowsSchemaArgument(string expression)
	{
		var exception = Assert.Throws<DataArgumentException>(() => Parser.Instance.ParseExpression(expression));
		Assert.Matches(@"\d+[)）][.。]$", exception.Message);
	}

	[Fact]
	public void Schema_DeepPaths_IncludeFindExcludeAndPruneConsistently()
	{
		var schema = new TestSchema("Root{Child{Leaf}},Other", "Root{Child{Leaf}},Other");

		Assert.True(schema.Contains("Root.Child.Leaf"));
		Assert.Same(schema.Find("Root/Child/Leaf"), schema.Find("Root.Child.Leaf"));
		Assert.False(schema.Contains("."));
		Assert.Null(schema.Find("Root..Leaf"));

		Assert.True(schema.Exclude("Root.Child.Leaf", out var removed));
		Assert.Equal("Leaf", removed.Name);
		Assert.False(schema.Contains("Root"));
		Assert.True(schema.Contains("Other"));

		schema.Include("Root/Child/Leaf");
		Assert.Equal("Root.Child.Leaf", schema.Find("Root.Child.Leaf").FullPath);
		Assert.Equal("Root{Child{Leaf}},Other", schema.Text);
	}

	[Fact]
	public void Schema_ToString_WritesCanonicalCurrentTree()
	{
		var schema = new TestSchema("Root:10(Name,~Created){Child:*{Leaf}},Other", " original ");
		var text = schema.ToString();

		Assert.Equal("Root:10(Name,~Created){Child{Leaf}},Other", text);
		Assert.Equal(text, new TestSchema(text, text).ToString());
		Assert.Equal(" original ", schema.Text);
	}

	[Fact]
	public void Schema_ReadOnly_BlocksPublicMutations()
	{
		var schema = new TestSchema("Root{Child},Other", "Root{Child},Other") { IsReadOnly = true };

		schema.Clear();
		schema.Include("Added");
		Assert.False(schema.Exclude("Root.Child", out _));

		Assert.True(schema.Contains("Root.Child"));
		Assert.True(schema.Contains("Other"));
		Assert.False(schema.Contains("Added"));
	}

	[Fact]
	public async Task Parse_ConcurrentCalls_DoNotShareMemberState()
	{
		var tasks = Enumerable.Range(0, 1000).Select(index => Task.Run(() =>
		{
			var members = Parser.Instance.ParseExpression(index % 2 == 0 ? "Root{A,B}" : "Root{C,D}");
			return string.Join(',', members["Root"].Children.Select(member => member.Name));
		}));

		var results = await Task.WhenAll(tasks);
		Assert.Equal(500, results.Count(result => result == "A,B"));
		Assert.Equal(500, results.Count(result => result == "C,D"));
	}

	private sealed class TestSchema : Schema<Member>
	{
		public TestSchema(string expression, string text) : base("Test", text, typeof(object), Parser.Instance.ParseExpression(expression)) { }
		protected override IEnumerable<Member> OnInclude(string expression) => Parser.Instance.Append(expression, this.Members);
	}

	private sealed class Member(string name) : SchemaMemberBase(name)
	{
		private Member _parent;
		private SchemaMemberCollection<Member> _children;

		public new Member Parent => _parent;
		public override bool HasChildren => _children != null && _children.Count > 0;
		public new SchemaMemberCollection<Member> Children => _children;

		protected override SchemaMemberBase GetParent() => _parent;
		protected override void SetParent(SchemaMemberBase parent) => _parent = (Member)parent;
		protected override IEnumerable<SchemaMemberBase> GetChildren() => _children ?? [];
		protected override bool TryGetChild(string name, out SchemaMemberBase child)
		{
			if(_children != null && _children.TryGetValue(name, out var member))
			{
				child = member;
				return true;
			}

			child = null;
			return false;
		}

		protected override void AddChild(SchemaMemberBase child)
		{
			_children ??= [];
			_children.Add((Member)child);
			((Member)child)._parent = this;
		}

		protected override void RemoveChild(string name) => _children?.Remove(name);
		protected override void ClearChildren() => _children?.Clear();
	}

	private sealed class Parser : SchemaParserBase<Member>
	{
		public static readonly Parser Instance = new();

		public SchemaMemberCollection<Member> ParseExpression(string expression, IEnumerable<Member> members = null) =>
			new(base.Parse(expression, null, members));

		public IEnumerable<Member> Append(string expression, IEnumerable<Member> members) => base.Parse(expression, null, members);
		public override ISchema<Member> Parse(string name, string expression, Type entityType) => throw new NotSupportedException();

		protected override IEnumerable<Member> Resolve(SchemaEntryToken token) => token.Name == "*" ?
			[new("a"), new("b"), new("c"), new("d"), new("e"), new("f")] :
			[new(token.Name)];
	}
}
