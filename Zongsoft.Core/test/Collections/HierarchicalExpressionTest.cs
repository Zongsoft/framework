using System;
using System.Collections.Generic;

using Xunit;

using Zongsoft.IO;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Collections.Tests;

public class HierarchicalExpressionTest
{
	[Fact]
	public void Test1()
	{
		var text = @"/";
		var expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Root, expression.Anchor);
		Assert.Equal("/", expression.Path);
		Assert.True(expression.Segments == null || expression.Segments.Length == 0);

		text = @"./";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Current, expression.Anchor);
		Assert.Equal(".", expression.Path);
		Assert.True(expression.Segments == null || expression.Segments.Length == 0);

		text = @".. /";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Parent, expression.Anchor);
		Assert.Equal("..", expression.Path);
		Assert.True(expression.Segments == null || expression.Segments.Length == 0);

		text = @"segment1/segment2/segment3";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.None, expression.Anchor);
		Assert.Equal("segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		text = @" / segment1 / segment2/segment3";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Root, expression.Anchor);
		Assert.Equal("/segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		text = @". /segment1/segment2 / segment3 ";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Current, expression.Anchor);
		Assert.Equal("./segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		text = @"../segment1/segment2/segment3";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Null(expression.Accessor);
		Assert.Equal(PathAnchor.Parent, expression.Anchor);
		Assert.Equal("../segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);
	}

	[Fact]
	public void Test2()
	{
		var text = @"   / segment1  /segment2/ segment3  @ property1  ";
		var expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Equal(PathAnchor.Root, expression.Anchor);
		Assert.Equal("/segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		Assert.NotNull(expression.Accessor);
		Assert.Equal(MemberExpressionType.Identifier, expression.Accessor.ExpressionType);
		Assert.Equal("property1", ((IdentifierExpression)expression.Accessor).Name);
		Assert.Null(expression.Accessor.Next);

		text = @"  .. / segment1  /segment2/ segment3  [100]  ";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Equal(PathAnchor.Parent, expression.Anchor);
		Assert.Equal("../segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		Assert.NotNull(expression.Accessor);
		Assert.Equal(MemberExpressionType.Indexer, expression.Accessor.ExpressionType);
		var parameters = ((IndexerExpression)expression.Accessor).Arguments;
		Assert.Single(parameters);
		Assert.Equal(MemberExpressionType.Constant, parameters[0].ExpressionType);
		Assert.Equal(100, (int)((ConstantExpression)parameters[0]).Value);

		text = @"   segment1  /segment2/ segment3 @property1 [100] . property2  ";
		expression = HierarchicalExpressionParser.Parse(text);

		Assert.NotNull(expression);
		Assert.Equal(PathAnchor.None, expression.Anchor);
		Assert.Equal("segment1/segment2/segment3", expression.Path);

		Assert.Equal(3, expression.Segments.Length);
		Assert.Equal("segment1", expression.Segments[0]);
		Assert.Equal("segment2", expression.Segments[1]);
		Assert.Equal("segment3", expression.Segments[2]);

		Assert.NotNull(expression.Accessor);
		Assert.Equal(MemberExpressionType.Identifier, expression.Accessor.ExpressionType);
		Assert.Equal("property1", ((IdentifierExpression)expression.Accessor).Name);
		Assert.NotNull(expression.Accessor.Next);

		var member = expression.Accessor.Next;
		Assert.Equal(MemberExpressionType.Indexer, member.ExpressionType);
		parameters = ((IndexerExpression)member).Arguments;
		Assert.Single(parameters);
		Assert.Equal(MemberExpressionType.Constant, parameters[0].ExpressionType);
		Assert.Equal(100, (int)((ConstantExpression)parameters[0]).Value);
		Assert.NotNull(member.Next);

		member = member.Next;
		Assert.Equal(MemberExpressionType.Identifier, member.ExpressionType);
		Assert.Equal("property2", ((IdentifierExpression)member).Name);
		Assert.Null(member.Next);
	}
}
