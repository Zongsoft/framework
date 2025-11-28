using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Web.Routing.Tests;

public class RoutePatternTest
{
	[Fact]
	public void Resolve1()
	{
		var pattern = RoutePattern.Resolve("api/[controller]/{action}/{name=unnamed:length(3)}");

		Assert.Equal(3, pattern.Count);
		Assert.True(pattern.Contains("controller"));
		Assert.True(pattern.Contains("action"));
		Assert.True(pattern.Contains("name"));
		Assert.False(pattern.Contains(""));

		var entry = pattern["controller"];
		Assert.NotNull(entry);
		Assert.Equal("controller", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Empty(entry.Constraints);

		entry = pattern["action"];
		Assert.NotNull(entry);
		Assert.Equal("action", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Empty(entry.Constraints);

		entry = pattern["name"];
		Assert.NotNull(entry);
		Assert.Equal("name", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Value);
		Assert.NotNull(entry.Default);
		Assert.Equal("unnamed", entry.Default);
		Assert.NotEmpty(entry.Constraints);

		Assert.True(entry.Constraints.Contains("Length"));
		Assert.False(entry.Constraints.Contains(null));

		var constraint = entry.Constraints["length"];
		Assert.NotNull(constraint);
		Assert.Equal("length", constraint.Name);
		Assert.NotEmpty(constraint.Arguments);
		Assert.Single(constraint.Arguments);
		Assert.Equal("3", constraint.Arguments[0]);
	}

	[Fact]
	public void Resolve2()
	{
		var pattern = RoutePattern.Resolve("api/[controller]/{id:required:int:range(1,100)}/[action]/{**slug}");

		Assert.Equal(4, pattern.Count);
		Assert.True(pattern.Contains("controller"));
		Assert.True(pattern.Contains("action"));
		Assert.True(pattern.Contains("id"));
		Assert.False(pattern.Contains("xxx"));

		var entry = pattern["controller"];
		Assert.NotNull(entry);
		Assert.Equal("controller", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Empty(entry.Constraints);

		entry = pattern["action"];
		Assert.NotNull(entry);
		Assert.Equal("action", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Empty(entry.Constraints);

		entry = pattern["slug"];
		Assert.NotNull(entry);
		Assert.Equal("slug", entry.Name);
		Assert.False(entry.Optional);
		Assert.True(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Empty(entry.Constraints);

		entry = pattern["id"];
		Assert.NotNull(entry);
		Assert.Equal("id", entry.Name);
		Assert.False(entry.Optional);
		Assert.False(entry.Captured);
		Assert.Null(entry.Default);
		Assert.Null(entry.Value);
		Assert.Equal(3, entry.Constraints.Count);

		Assert.True(entry.Constraints.Contains("required"));
		Assert.True(entry.Constraints.Contains("int"));
		Assert.True(entry.Constraints.Contains("range"));
		Assert.False(entry.Constraints.Contains("xxx"));

		var constraint = entry.Constraints["required"];
		Assert.NotNull(constraint);
		Assert.Equal("required", constraint.Name);
		Assert.Empty(constraint.Arguments);

		constraint = entry.Constraints["int"];
		Assert.NotNull(constraint);
		Assert.Equal("int", constraint.Name);
		Assert.Empty(constraint.Arguments);

		constraint = entry.Constraints["range"];
		Assert.NotNull(constraint);
		Assert.Equal("range", constraint.Name);
		Assert.NotEmpty(constraint.Arguments);
		Assert.Equal(2, constraint.Arguments.Length);
		Assert.Equal("1", constraint.Arguments[0]);
		Assert.Equal("100", constraint.Arguments[1]);
	}

	[Fact]
	public void GetUrl()
	{
		var pattern = RoutePattern.Resolve("[controller]/{action}/{id?}");
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id=404}");
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id}");
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id:required}");
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id?}");
		pattern["controller"].Value = "home";
		Assert.Equal("home/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id=404}");
		pattern["controller"].Value = "home";
		Assert.Equal("home/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id}");
		pattern["controller"].Value = "home";
		Assert.Equal("home/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id:required}");
		pattern["controller"].Value = "home";
		Assert.Equal("home/{action}/{id}", pattern.GetUrl());

		pattern = RoutePattern.Resolve("[controller]/{action}/{id?}");
		Assert.Equal("home/{action}/{id}", pattern.GetUrl(entry => entry.Name == "controller" ? "home" : null));
		pattern["action"].Value = "index";
		Assert.Equal("{controller}/index/{id}", pattern.GetUrl());
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl(entry => null));

		pattern = RoutePattern.Resolve("[controller]/{action}/{id=404}");
		Assert.Equal("home/{action}/{id}", pattern.GetUrl(entry => entry.Name == "controller" ? "home" : null));
		pattern["action"].Value = "index";
		Assert.Equal("{controller}/index/{id}", pattern.GetUrl());
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl(entry => null));

		pattern = RoutePattern.Resolve("[controller]/{action}/{id}");
		Assert.Equal("home/{action}/{id}", pattern.GetUrl(entry => entry.Name == "controller" ? "home" : null));
		pattern["action"].Value = "index";
		Assert.Equal("{controller}/index/{id}", pattern.GetUrl());
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl(entry => null));
		Assert.Equal("{controller}/{action}/@id", pattern.GetUrl(entry => entry.Name == "id" ? $"@{entry.Name}" : null));

		pattern = RoutePattern.Resolve("[controller]/{action}/{id:required}");
		Assert.Equal("home/{action}/{id}", pattern.GetUrl(entry => entry.Name == "controller" ? "home" : null));
		pattern["action"].Value = "index";
		Assert.Equal("{controller}/index/{id}", pattern.GetUrl());
		Assert.Equal("{controller}/{action}/{id}", pattern.GetUrl(entry => null));
		Assert.Equal("{controller}/{action}/@id", pattern.GetUrl(entry => entry.Name == "id" ? $"@{entry.Name}" : null));
	}
}
