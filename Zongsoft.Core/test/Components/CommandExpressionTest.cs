﻿using System;
using System.Collections.Generic;

using Zongsoft.IO;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandExpressionTest
{
	[Fact]
	public void TestCommandExpressionParse()
	{
		var expression = CommandExpression.Parse("");
		Assert.Null(expression);

		expression = CommandExpression.Parse("/");
		Assert.Equal("/", expression.Name);
		Assert.Equal("", expression.Path);
		Assert.Equal("/", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);

		expression = CommandExpression.Parse(".");
		Assert.Equal(".", expression.Name);
		Assert.Equal("", expression.Path);
		Assert.Equal(".", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);

		expression = CommandExpression.Parse("..");
		Assert.Equal("..", expression.Name);
		Assert.Equal("", expression.Path);
		Assert.Equal("..", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);

		expression = CommandExpression.Parse("send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("", expression.Path);
		Assert.Equal("send", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);

		expression = CommandExpression.Parse("/send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("/", expression.Path);
		Assert.Equal("/send", expression.FullPath);
		Assert.Equal(PathAnchor.Root, expression.Anchor);

		expression = CommandExpression.Parse("./send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("./", expression.Path);
		Assert.Equal("./send", expression.FullPath);
		Assert.Equal(PathAnchor.Current, expression.Anchor);

		expression = CommandExpression.Parse("../send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("../", expression.Path);
		Assert.Equal("../send", expression.FullPath);
		Assert.Equal(PathAnchor.Parent, expression.Anchor);

		expression = CommandExpression.Parse("sms.send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("sms/", expression.Path);
		Assert.Equal("sms/send", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);

		expression = CommandExpression.Parse("/sms.send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("/sms/", expression.Path);
		Assert.Equal("/sms/send", expression.FullPath);
		Assert.Equal(PathAnchor.Root, expression.Anchor);

		expression = CommandExpression.Parse("./sms.send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("./sms/", expression.Path);
		Assert.Equal("./sms/send", expression.FullPath);
		Assert.Equal(PathAnchor.Current, expression.Anchor);

		expression = CommandExpression.Parse("../sms.send");
		Assert.Equal("send", expression.Name);
		Assert.Equal("../sms/", expression.Path);
		Assert.Equal("../sms/send", expression.FullPath);
		Assert.Equal(PathAnchor.Parent, expression.Anchor);

		expression = CommandExpression.Parse("notification.send -name:Zongsoft.CRM -type:Message -mode:Alias -destination:User.100 -title:'task:create'");
		Assert.Equal("send", expression.Name);
		Assert.Equal(5, expression.Options.Count);
		Assert.Equal("Zongsoft.CRM", expression.Options["name"]);
		Assert.Equal("Message", expression.Options["type"]);
		Assert.Equal("Alias", expression.Options["mode"]);
		Assert.Equal("User.100", expression.Options["destination"]);
		Assert.Equal("task:create", expression.Options["title"]);
	}

	[Fact]
	public void TestCommandExpressionParseException()
	{
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("./"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("../"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("help/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("/help/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("./help/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("../help/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("queue.in/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("/queue.in/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("./queue.in/"));
		Assert.Throws<CommandExpressionException>(() => CommandExpression.Parse("../queue.in/"));
	}

	[Fact]
	public void TestCommandExpressionParseWithPipeline()
	{
		CommandExpression expression;

		expression = CommandExpression.Parse("plugins.find '/Workbench'");

		Assert.Equal("find", expression.Name);
		Assert.Equal("plugins/", expression.Path);
		Assert.Equal("plugins/find", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);
		Assert.Equal(0, expression.Options.Count);
		Assert.Single(expression.Arguments);
		Assert.Equal("/Workbench", expression.Arguments[0]);

		expression = CommandExpression.Parse("sms.generate -template:'user.register' -authenticode:12345 phonenumber'1 phonenumber:2 phonenumber-3 'phonenumber|4' | queue.in sms");

		Assert.Equal("generate", expression.Name);
		Assert.Equal("sms/", expression.Path);
		Assert.Equal("sms/generate", expression.FullPath);
		Assert.Equal(PathAnchor.None, expression.Anchor);
		Assert.Equal(2, expression.Options.Count);
		Assert.Equal(4, expression.Arguments.Length);
		Assert.Equal("user.register", expression.Options["template"]);
		Assert.Equal("12345", expression.Options["authenticode"]);
		Assert.Equal("phonenumber'1", expression.Arguments[0]);
		Assert.Equal("phonenumber:2", expression.Arguments[1]);
		Assert.Equal("phonenumber-3", expression.Arguments[2]);
		Assert.Equal("phonenumber|4", expression.Arguments[3]);

		Assert.NotNull(expression.Next);
		Assert.Equal("in", expression.Next.Name);
		Assert.Equal("queue/", expression.Next.Path);
		Assert.Equal("queue/in", expression.Next.FullPath);
		Assert.Equal(PathAnchor.None, expression.Next.Anchor);
		Assert.Equal("sms", expression.Next.Arguments[0]);
	}
}
