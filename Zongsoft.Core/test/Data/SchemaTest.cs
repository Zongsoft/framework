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
	[InlineData("*,!*", "")]
	public void Parse_TolerantCompatibilityExpressions_ReturnsExpectedMembers(string expression, string expected)
	{
		var members = Parser.Instance.ParseExpression(expression);
		Assert.Equal(expected, string.Join(',', members?.Select(member => member.Name) ?? []));
	}

	[Fact]
	public void Parse_NestedPagingAndSorting_PreservesModifiersAndLastSorting()
	{
		var members = Parser.Instance.ParseExpression("Forums:20,Users:2/10(~Created,Name,!Name,Code){Profile(Name){Avatar}}");

		Assert.Equal(2, members.Count);
		Assert.Equal(1, members["Forums"].Paging.Index);
		Assert.Equal(20, members["Forums"].Paging.Size);

		var users = members["Users"];
		Assert.Equal(2, users.Paging.Index);
		Assert.Equal(10, users.Paging.Size);
		Assert.Collection(users.Sortings,
			sorting => { Assert.Equal("Created", sorting.Name); Assert.Equal(SortingMode.Descending, sorting.Mode); },
			sorting => { Assert.Equal("Name", sorting.Name); Assert.Equal(SortingMode.Descending, sorting.Mode); },
			sorting => { Assert.Equal("Code", sorting.Name); Assert.Equal(SortingMode.Ascending, sorting.Mode); });
		Assert.Equal("Users.Profile.Avatar", users.Children["Profile"].Children["Avatar"].FullPath);
	}

	[Fact]
	public void Parse_PagingBeforeClosingBrace_PreservesPageAndSize()
	{
		var members = Parser.Instance.ParseExpression("Root{Children:2/10}");
		var paging = members["Root"].Children["Children"].Paging;

		Assert.Equal(2, paging.Index);
		Assert.Equal(10, paging.Size);
	}

	[Theory]
	[InlineData("Root:9", 1, 9)]
	[InlineData("Root:2/7", 2, 7)]
	[InlineData("Root:2/?", 2, 20)]
	[InlineData("Root:*", 0, 0)]
	public void Parse_PagingVariants_PreserveTheirMeaning(string expression, int index, int size)
	{
		var paging = Parser.Instance.ParseExpression(expression)["Root"].Paging;
		Assert.NotNull(paging);
		Assert.Equal(index, paging.Index);
		Assert.Equal(size, paging.Size);
	}

	[Fact]
	public void Parse_PagingQuestion_ClearsExistingPaging()
	{
		var member = Parser.Instance.ParseExpression("Root:9,Root:?")["Root"];
		Assert.Null(member.Paging);
	}

	[Theory]
	[InlineData("Users(1Name)")]
	[InlineData("Users(~)")]
	[InlineData("Users:999999999999999999999")]
	[InlineData("1Users")]
	[InlineData("Users:")]
	[InlineData("Users:2/")]
	[InlineData("Users()")]
	[InlineData("Users(Name,)")]
	[InlineData("Users{Profile")]
	[InlineData("Users{Name}}")]
	public void Parse_InvalidExpression_ThrowsSchemaArgument(string expression)
	{
		var exception = Assert.Throws<DataArgumentException>(() => Parser.Instance.ParseExpression(expression));
		Assert.Contains("position", exception.Message, StringComparison.OrdinalIgnoreCase);
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
		var schema = new TestSchema("Root:2/10(Name,~Created){Child:*{Leaf}},Other", " original ");
		var text = schema.ToString();

		Assert.Equal("Root:2/10(Name,~Created){Child:*{Leaf}},Other", text);
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
