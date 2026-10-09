using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using Xunit;
using Zongsoft.Expressions;

namespace Zongsoft.Text.Templating.Tests;

public class TemplateEvaluatorTest
{
	[Fact]
	public void SpanInputs()
	{
		var evaluator = Create(new() { ["name"] = "Zongsoft" });
		const string SOURCE = "ignore ${name} ignore";
		var slice = SOURCE.AsSpan(7, 7);

		Assert.Equal("Zongsoft", evaluator.Evaluate("${name}"));
		Assert.Equal("Zongsoft", evaluator.Evaluate(slice));
		Assert.True(evaluator.TryEvaluate(slice, out var result, out var error));
		Assert.Equal("Zongsoft", result);
		Assert.Null(error);

		Span<char> buffer = stackalloc char[32];
		const string TEMPLATE = "x ${name} \\${literal}";
		TEMPLATE.AsSpan().CopyTo(buffer);
		Assert.Equal("x Zongsoft ${literal}", evaluator.Evaluate(buffer[..TEMPLATE.Length]));
		Assert.Equal("literal", evaluator.Evaluate("--literal--".AsSpan(2, 7)));
	}

	[Fact]
	public void SpanDiagnostics()
	{
		var evaluator = Create(new() { ["name"] = "value" });
		var calls = 0;
		evaluator.Resolving += (_, _) => calls++;
		var buffer = "skip ${name}-${bad skip".ToCharArray();

		Assert.False(evaluator.TryEvaluate(buffer.AsSpan(5, 13), out var result, out var error));
		Array.Fill(buffer, '?');
		Assert.Null(result);
		Assert.Equal("${name}-${bad", error.Template);
		Assert.Equal("UnclosedPlaceholder", error.Code);
		Assert.Equal(TemplateEvaluationStage.Parsing, error.Stage);
		Assert.Equal(8, error.Position);
		Assert.Equal(5, error.Length);
		Assert.Equal(1, error.Depth);
		Assert.Equal(0, calls);

		Assert.False(evaluator.TryEvaluate("skip 😀${missing} end".AsSpan(5, 12), out result, out error));
		Assert.Null(result);
		Assert.Equal("😀${missing}", error.Template);
		Assert.Equal("missing", error.Expression);
		Assert.Equal(4, error.Position);
		Assert.Equal(7, error.Length);
		Assert.Equal("MissingVariable", error.Code);
		Assert.Equal(1, calls);
	}

	[Fact]
	public void SpanContextsOwnSource()
	{
		var evaluator = Create(new() { ["name"] = "value" });
		var buffer = "skip ${name}/${name} end".ToCharArray();
		var contexts = new List<TemplateEvaluator.ResolutionContext>();
		var formats = new List<TemplateEvaluator.FormattingContext>();

		evaluator.Resolving += (_, context) =>
		{
			contexts.Add(context);
			Array.Fill(buffer, '?');
		};
		evaluator.Formatted += (_, context) => formats.Add(context);

		Assert.Equal("value/value", evaluator.Evaluate(buffer.AsSpan(5, 15)));
		Assert.Equal(2, contexts.Count);
		Assert.Equal(2, formats.Count);
		Assert.All(contexts, context =>
		{
			Assert.Equal("${name}/${name}", context.Template);
			Assert.Equal("name", context.Expression);
			Assert.Equal("value", context.Value);
			Assert.Equal(4, context.Length);
			Assert.Equal(1, context.Depth);
		});
		Assert.Equal(2, contexts[0].Position);
		Assert.Equal(10, contexts[1].Position);
		Assert.All(formats, context =>
		{
			Assert.Equal("${name}/${name}", context.Template);
			Assert.Equal("name", context.Expression);
			Assert.Equal("value", context.Text);
		});
	}

	[Fact]
	public void SourcesAndNamespaces()
	{
		var calls = new List<string>();
		var evaluator = Create(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = "first",
			["app.runtime:name"] = "scoped",
			["nothing"] = null,
		});
		evaluator.Providers.Add(new Provider((name, scope) =>
		{
			calls.Add(name);
			return (true, "fallback");
		}));

		Assert.Equal("first/scoped//fallback", evaluator.Evaluate("${NAME}/${APP.Runtime:Name}/${nothing}/${other}"));
		Assert.Equal(["other"], calls);
		evaluator.Providers.RemoveAt(1);
		Assert.Equal("MissingVariable", Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${app:name}")).Code);
	}

	[Fact]
	public void MembersAndIndices()
	{
		var evaluator = Create(new()
		{
			["person"] = new Person { Name = "Zongsoft", Home = new Person { Name = "Shanghai" } },
			["arr"] = new List<Person> { new() { Name = "zero" }, new() { Name = "one" } },
			["indices"] = new List<int> { 1 },
			["map"] = new Dictionary<string, object> { ["a.b:#}"] = "key", ["empty"] = null },
			["list"] = new List<string> { "first" },
		});

		Assert.Equal("Shanghai/one/key//first",
			evaluator.Evaluate("${person.home.name}/${arr[indices[0]].Name}/${map['a.b:#}']}/${map[\"empty\"]}/${list[0]}"));
		Assert.Equal("field", evaluator.Evaluate("${person.Field}"));
		Assert.Equal("", evaluator.Evaluate("${person.Home.Home}"));
		Assert.Equal("NullTarget", Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${person.Home.Home.Name}")).Code);
		Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${arr[99]}")).InnerException);
		Assert.IsType<KeyNotFoundException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${map['missing']}")).InnerException);
	}

	[Fact]
	public void ReflectorMemberContracts()
	{
		var evaluator = Create(new()
		{
			["person"] = new Person(),
			["type"] = typeof(Person),
			["readonly"] = new ReadOnlyMap<Person>(new() { [2] = new Person { Name = "read" } }),
			["table"] = new Hashtable { ["empty"] = null },
			["array"] = new[] { 1, 2 },
		});

		Assert.Equal("static/static/", evaluator.Evaluate("${person.Static}/${type.Static}/${person.PrivateGetter}"));
		Assert.Equal("/", evaluator.Evaluate("${table['empty']}/${table['absent']}"));
		Assert.Equal("2", evaluator.Evaluate("${array.Length}"));

		foreach(var text in new[] { "${person.Secret}", "${readonly[2]}", "${readonly.Count}", "${array[0]}" })
		{
			var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate(text));
			Assert.Equal("NavigationFailed", error.Code);
			Assert.Equal(TemplateEvaluationStage.Resolution, error.Stage);
			Assert.IsType<ArgumentException>(error.InnerException);
		}

		Assert.IsType<InvalidOperationException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${person.WriteOnly}")).InnerException);

		Assert.Equal(TemplateEvaluationStage.Parsing, Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${person.ToString()}")).Stage);
	}

	[Fact]
	public void IndexBindingUsesReflector()
	{
		var evaluator = Create(new()
		{
			["indexer"] = new IntegerIndexer(),
			["value"] = 2,
		});

		Assert.Equal("2/0/3", evaluator.Evaluate("${indexer[value]}/${indexer[0]}/${indexer[3]}"));
		Assert.IsType<InvalidCastException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${indexer['2']}")).InnerException);
		Assert.IsType<InvalidCastException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${indexer[3L]}")).InnerException);
		Assert.IsType<NullReferenceException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${indexer[null]}")).InnerException);
	}

	[Fact]
	public void OverloadedNavigationMatchesReflector()
	{
		foreach(var target in new object[] { new OverloadedIndexer(), new AmbiguousIndexer(), new AssignableIndexer() })
		{
			AssertReflectorNavigation(target, null, "value[1]", 1);
			AssertReflectorNavigation(target, null, "value['1']", "1");
		}
	}

	[Theory]
	[InlineData("0", typeof(int), "0")]
	[InlineData("-2147483648", typeof(int), "-2147483648")]
	[InlineData("2147483648", typeof(long), "2147483648")]
	[InlineData("-9223372036854775808", typeof(long), "-9223372036854775808")]
	[InlineData("1l", typeof(long), "1")]
	[InlineData("1L", typeof(long), "1")]
	[InlineData("1.5", typeof(double), "1.5")]
	[InlineData("1.5F", typeof(float), "1.5")]
	[InlineData("1.5d", typeof(double), "1.5")]
	[InlineData("1.5M", typeof(decimal), "1.5")]
	[InlineData("TRUE", typeof(bool), "True")]
	[InlineData("null", null, "")]
	public void IndexLiterals(string literal, Type type, string expected)
	{
		var indexer = new EchoIndexer();
		var evaluator = Create(new() { ["echo"] = indexer });
		evaluator.Options.Culture = CultureInfo.InvariantCulture;
		var previous = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
			Assert.Equal(expected, evaluator.Evaluate("${echo[" + literal + "]}"));
			Assert.Equal(type, indexer.Last?.GetType());
		}
		finally { CultureInfo.CurrentCulture = previous; }
	}

	[Fact]
	public void ContextualKeywords()
	{
		var evaluator = Create(new()
		{
			["true"] = new Person { Name = "named" },
			["trueValue"] = "prefixed",
			["app:null"] = "scoped",
			["echo"] = new EchoIndexer(),
			["null"] = "root",
		});

		Assert.Equal("named/prefixed/scoped/root/named",
			evaluator.Evaluate("${true.Name}/${trueValue}/${app:null}/${null}/${echo[true.Name]}"));
	}

	[Fact]
	public void Escaping()
	{
		var evaluator = Create(new()
		{
			["name"] = "value",
			["map"] = new Dictionary<string, string> { ["a\n'b\"c\\$"] = "escaped", ["${name}"] = "literal" },
		});

		Assert.Equal("${name}/value\n\r\t\\'\"$", evaluator.Evaluate("""\${name}/${name}\n\r\t\\\'\"\$"""));
		Assert.Equal("${value}", evaluator.Evaluate("""\${${name}}"""));
		Assert.Equal("escaped/literal", evaluator.Evaluate("""${map['a\n\'b\"c\\\$']}/${map['${name}']}"""));
		Assert.Equal("%name%/$(name)", evaluator.Evaluate("%name%/$(name)"));
	}

	[Theory]
	[InlineData("""\q""", "InvalidEscape")]
	[InlineData("\\", "InvalidEscape")]
	[InlineData("${name", "UnclosedPlaceholder")]
	[InlineData("${}", "InvalidSyntax")]
	[InlineData("${ name}", "InvalidWhitespace")]
	[InlineData("${name }", "InvalidWhitespace")]
	[InlineData("${a[ 0]}", "InvalidWhitespace")]
	[InlineData("${a#  }", "EmptyFormat")]
	[InlineData("${a#}", "EmptyFormat")]
	[InlineData("${=1+2}", "UnsupportedExpression")]
	[InlineData("${(1+2)}", "InvalidSyntax")]
	[InlineData("${a[]}", "InvalidSyntax")]
	[InlineData("${a[1,]}", "InvalidSyntax")]
	[InlineData("${a[1+2]}", "InvalidSyntax")]
	[InlineData("${a[9223372036854775808]}", "InvalidSyntax")]
	[InlineData("${a[1.2L]}", "InvalidSyntax")]
	[InlineData("${a.b()}", "InvalidSyntax")]
	[InlineData("${变量}", "InvalidSyntax")]
	[InlineData("${a-b}", "InvalidSyntax")]
	public void SyntaxFailures(string text, string code)
	{
		var evaluator = new TemplateEvaluator();
		var calls = 0;

		evaluator.Resolving += (_, _) => calls++;
		Assert.False(evaluator.TryEvaluate(text, out var result, out var error));
		Assert.Null(result);
		Assert.Equal(code, error.Code);
		Assert.Equal(TemplateEvaluationStage.Parsing, error.Stage);
		Assert.Equal(text, error.Template);
		Assert.Equal(1, error.Depth);
		Assert.InRange(error.Position, 0, text.Length);
		Assert.Equal(0, calls);
	}

	[Fact]
	public void ParseBeforeEvaluationAndSourcePositions()
	{
		var evaluator = Create(new() { ["name"] = "value" });
		var calls = 0;
		evaluator.Resolving += (_, _) => calls++;
		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${name}-${bad"));

		Assert.Equal(8, error.Position);
		Assert.Equal(5, error.Length);
		Assert.Equal(0, calls);

		evaluator.Resolving += (_, context) =>
		{
			Assert.Equal("name", context.Expression);
			Assert.Equal(4, context.Position);
			Assert.Equal(4, context.Length);
			Assert.Equal(1, context.Depth);
			Assert.False(context.IsIndex);
		};
		Assert.Equal("😀value", evaluator.Evaluate("😀${name}"));
	}

	[Fact]
	public void Formatting()
	{
		var evaluator = Create(new()
		{
			["number"] = 12.5m,
			["date"] = new DateTime(2026, 10, 9, 12, 34, 56),
			["nothing"] = null,
			["text"] = "plain",
		});
		var previous = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
			Assert.Equal("12,50", evaluator.Evaluate("${number#0.00}"));

			evaluator.Options.Culture = CultureInfo.InvariantCulture;
			Assert.Equal("12.50|2026-10-09 12:34:56||plain",
				evaluator.Evaluate("${number#  0.00  }|${date#yyyy-MM-dd HH:mm:ss}|${nothing#0.00}|${text#0.00}"));

			Assert.Equal("13}", evaluator.Evaluate("""${number#0'}'}"""));
			Assert.Equal("13}", evaluator.Evaluate("""${number#0\}}"""));
			Assert.Equal("13 ", evaluator.Evaluate("""${number#0\ }"""));
		}
		finally { CultureInfo.CurrentCulture = previous; }
	}

	[Fact]
	public void EventOrderAndNestedIndexMetadata()
	{
		var evaluator = Create(new() { ["arr"] = new List<Person> { new() { Name = "zero" } }, ["index"] = 0 });
		var events = new List<string>();

		evaluator.Resolving += (_, context) =>
		{
			events.Add("before:" + context.Expression);
			if(context.IsIndex)
			{
				Assert.Equal("index", context.Name);
				Assert.Equal(6, context.Position);
				Assert.Equal(5, context.Length);
			}
		};
		evaluator.Resolved += (_, context) => events.Add("after:" + context.Expression);
		evaluator.Formatting += (_, context) => events.Add("format:" + context.Expression);
		evaluator.Formatted += (_, context) => events.Add("formatted:" + context.Expression);

		Assert.Equal("zero", evaluator.Evaluate("${arr[index].Name}"));
		Assert.Equal(["before:arr[index].Name", "before:index", "after:index", "after:arr[index].Name", "format:arr[index].Name", "formatted:arr[index].Name"], events);
	}

	[Fact]
	public void CallbacksCanReplaceValuesAndFormatting()
	{
		var evaluator = new TemplateEvaluator();
		var events = new List<string>();
		evaluator.Resolving += (_, context) =>
		{
			context.Handled = true;
			context.Value = 1;
			events.Add("first");
		};
		evaluator.Resolving += (_, context) => { context.Value = 2; events.Add("second"); };
		evaluator.Resolved += (_, context) => { Assert.Equal(2, context.Value); context.Value = 3; };
		evaluator.Formatting += (_, context) =>
		{
			Assert.Equal(3, context.Value);
			context.Value = 4.5m;
			context.Format = "0.00";
			context.Culture = CultureInfo.GetCultureInfo("fr-FR");
		};
		evaluator.Formatted += (_, context) => context.Text = "[" + context.Text + "]";

		Assert.Equal("[4,50]", evaluator.Evaluate("${missing[unknown].Name}"));
		Assert.Equal(["first", "second"], events);

		evaluator.Formatting += (_, context) => { context.Handled = true; context.Text = "${literal}"; };
		Assert.Equal("[${literal}]", evaluator.Evaluate("${anything}"));
	}

	[Fact]
	public void LastHandledValueControlsDefaultProcessing()
	{
		var evaluator = Create(new() { ["value"] = "provided" });
		evaluator.Resolving += (_, context) => { context.Handled = true; context.Value = "override"; };
		evaluator.Resolving += (_, context) => context.Handled = false;
		evaluator.Formatting += (_, context) => { context.Handled = true; context.Text = "override"; };
		evaluator.Formatting += (_, context) => context.Handled = false;
		Assert.Equal("provided", evaluator.Evaluate("${value}"));
	}

	[Fact]
	public void NoValueCachingAndTryDoesNotRetry()
	{
		var evaluator = new TemplateEvaluator();
		var calls = 0;
		evaluator.Providers.Add(new Provider((_, _) => (true, ++calls)));

		Assert.True(evaluator.TryEvaluate("${value}/${value}", out var result, out var error));
		Assert.Equal("1/2", result);
		Assert.Null(error);
		Assert.Equal(2, calls);

		var person = new Person();
		evaluator.Providers.Clear();
		evaluator.Providers.Add(new Provider((_, _) => (true, person)));
		Assert.Equal("1/2", evaluator.Evaluate("${person.Next}/${person.Next}"));
	}

	[Theory]
	[InlineData(TemplateEvaluationStage.Resolving)]
	[InlineData(TemplateEvaluationStage.Resolved)]
	[InlineData(TemplateEvaluationStage.Formatting)]
	[InlineData(TemplateEvaluationStage.Formatted)]
	public void CallbackFailures(TemplateEvaluationStage stage)
	{
		var evaluator = Create(new() { ["value"] = 42 });
		var cause = new InvalidOperationException("callback");
		var events = new List<TemplateEvaluationStage>();

		evaluator.Resolving += (_, _) => Visit(TemplateEvaluationStage.Resolving);
		evaluator.Resolved += (_, _) => Visit(TemplateEvaluationStage.Resolved);
		evaluator.Formatting += (_, _) => Visit(TemplateEvaluationStage.Formatting);
		evaluator.Formatted += (_, _) => Visit(TemplateEvaluationStage.Formatted);

		Assert.False(evaluator.TryEvaluate("${value}", out var result, out var error));
		Assert.Null(result);
		Assert.Equal(stage, error.Stage);
		Assert.Equal("CallbackFailed", error.Code);
		Assert.Same(cause, error.InnerException);
		Assert.Equal(stage, events[^1]);

		void Visit(TemplateEvaluationStage current)
		{
			events.Add(current);

			if(current == stage)
				throw cause;
		}
	}

	[Fact]
	public void ProviderAndGetterFailures()
	{
		var evaluator = new TemplateEvaluator();
		var cause = new InvalidOperationException("provider");
		evaluator.Providers.Add(new Provider((_, _) => throw cause));
		evaluator.Providers.Add(new Provider((_, _) => throw new Exception("must not run")));

		var after = 0;
		evaluator.Resolved += (_, _) => after++;

		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value}"));
		Assert.Same(cause, error.InnerException);
		Assert.Equal("ProviderFailed", error.Code);
		Assert.Equal(0, after);

		evaluator.Providers.Clear();
		evaluator.Providers.Add(new Provider((_, _) => (true, new Person())));
		error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value.Broken}"));
		Assert.IsType<NotSupportedException>(error.InnerException);
		Assert.Equal(0, after);
	}

	[Fact]
	public void FormattingFailureStopsAfterCallback()
	{
		var evaluator = Create(new() { ["value"] = new ThrowingFormattable() });
		var after = false;
		evaluator.Formatted += (_, _) => after = true;

		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value#x}"));
		Assert.Equal(TemplateEvaluationStage.Format, error.Stage);
		Assert.IsType<NotSupportedException>(error.InnerException);
		Assert.False(after);
	}

	[Fact]
	public void RecursiveTemplatesAndIndices()
	{
		var evaluator = Create(new()
		{
			["value"] = "${name}",
			["name"] = "Zongsoft",
			["index"] = "${number}",
			["number"] = 1,
			["arr"] = new Dictionary<string, string> { ["0"] = "zero", ["1"] = "one" },
			["map"] = new Dictionary<string, string> { ["${name}"] = "literal" },
		});

		Assert.Equal("${name}", evaluator.Evaluate("${value}"));

		evaluator.Options.Recursive = true;
		Assert.Equal("Zongsoft/one/literal", evaluator.Evaluate("${value}/${arr[index]}/${map['${name}']}"));

		evaluator.Resolved += (_, context) =>
		{
			if(context.Name == "value")
				context.Value = "${number}";
		};
		Assert.Equal("1", evaluator.Evaluate("${value}"));
	}

	[Fact]
	public void RecursiveDepthAndChildDiagnostics()
	{
		var values = new Dictionary<string, object> { ["value"] = "literal" };
		var evaluator = Create(values);
		evaluator.Options.Recursive = true;
		evaluator.Options.MaximumDepth = 1;
		Assert.Equal("DepthExceeded", Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value}")).Code);

		evaluator.Options.MaximumDepth = 2;
		Assert.Equal("literal/literal", evaluator.Evaluate("${value}/${value}"));

		values["value"] = "${missing}";
		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("skip outer ${value} end".AsSpan(5, 14)));

		Assert.Equal("${missing}", error.Template);
		Assert.Equal(2, error.Depth);
		Assert.Equal(2, error.Position);
		Assert.Equal("missing", error.Expression);
		values["value"] = "${broken";
		error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value}"));
		Assert.Equal("${broken", error.Template);
		Assert.Equal(0, error.Position);
		Assert.Equal(2, error.Depth);
		values["value"] = "${value}";
		Assert.Equal("DepthExceeded", Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${value}")).Code);
	}

	[Fact]
	public void RepeatedRecursiveNameCanTerminate()
	{
		var evaluator = new TemplateEvaluator(new() { Recursive = true, MaximumDepth = 3 });
		var calls = 0;
		evaluator.Providers.Add(new Provider((_, _) => (true, ++calls == 1 ? "${value}" : 42)));
		Assert.Equal("42", evaluator.Evaluate("${value}"));
		Assert.Equal(2, calls);
	}

	[Fact]
	public void OptionsAndInvalidApiArguments()
	{
		var options = new TemplateEvaluatorOptions();
		var evaluator = new TemplateEvaluator(options);

		Assert.Same(options, evaluator.Options);
		Assert.Null(options.Culture);
		Assert.False(options.Recursive);
		Assert.Equal(64, options.MaximumDepth);
		Assert.NotSame(options, new TemplateEvaluator().Options);
		Assert.Throws<ArgumentOutOfRangeException>(() => options.MaximumDepth = 0);
		Assert.Equal("", evaluator.Evaluate(null));
		Assert.Equal("", evaluator.Evaluate(default));
		Assert.Equal("", evaluator.Evaluate(ReadOnlySpan<char>.Empty));
		Assert.True(evaluator.TryEvaluate(null, out var result, out var error));
		Assert.Equal("", result);
		Assert.Null(error);
		Assert.True(evaluator.TryEvaluate(default, out result, out error));
		Assert.Equal("", result);
		Assert.Null(error);
		Assert.Equal("", evaluator.Evaluate(""));
		Assert.Equal(" \t ", evaluator.Evaluate(" \t "));

		evaluator.Providers.Add(null);
		Assert.Equal("Providers", Assert.Throws<ArgumentException>(() => evaluator.TryEvaluate(default, out _, out _)).ParamName);
		Assert.Equal("Providers", Assert.Throws<ArgumentException>(() => evaluator.Evaluate(null)).ParamName);
	}

	[Fact]
	public void DictionaryKeysAndMultipleIndexArguments()
	{
		var evaluator = Create(new()
		{
			["map"] = new Dictionary<long, string> { [1] = "long" },
			["pair"] = new PairIndexer(),
			["app:arr"] = new List<string> { "zero", "one" },
			["index"] = 1,
			["app:index"] = 0,
		});

		var indexEvents = 0;
		evaluator.Resolved += (_, context) =>
		{
			if(context.IsIndex)
			{
				indexEvents++;
				Assert.IsType<int>(context.Value);
				Assert.Null(context.Namespace);
			}
		};

		Assert.Equal("long/2:key/one", evaluator.Evaluate("${map[1L]}/${pair[2,'key']}/${app:arr[index]}"));
		Assert.Equal(1, indexEvents);
		Assert.IsType<InvalidCastException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${map[1]}")).InnerException);
		Assert.IsType<InvalidCastException>(Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${map['1']}")).InnerException);
	}

	[Fact]
	public void MemberSelectionMatchesReflector()
	{
		AssertReflectorNavigation(new AmbiguousMember(), "name", "value.name");
		var evaluator = Create(new() { ["derived"] = new DerivedPerson() });
		Assert.Equal("derived", evaluator.Evaluate("${derived.Name}"));
	}

	[Fact]
	public void IndexFailuresKeepInnerReferenceAndStopOuterEvents()
	{
		var evaluator = Create(new() { ["arr"] = new List<string> { "zero" } });
		var after = 0;
		evaluator.Resolved += (_, _) => after++;

		var error = Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("skip ${arr[unknown]} end".AsSpan(5, 15)));
		Assert.Equal("${arr[unknown]}", error.Template);
		Assert.Equal("unknown", error.Expression);
		Assert.Equal(6, error.Position);
		Assert.Equal(7, error.Length);
		Assert.Equal(1, error.Depth);
		Assert.Equal(0, after);
	}

	[Fact]
	public void FormatTextIsPassedVerbatimAndCallbackOutputIsNotExpanded()
	{
		var evaluator = Create(new() { ["value"] = new FormatEcho() });
		Assert.Equal("a b", evaluator.Evaluate("${value#  a b  }"));
		Assert.Equal("' a '", evaluator.Evaluate("${value# ' a ' }"));
		Assert.Equal(@"a\ ", evaluator.Evaluate(@"${value# a\  }"));

		evaluator.Options.Recursive = true;
		evaluator.Formatted += (_, context) => context.Text = "${missing}";
		Assert.Equal("${missing}", evaluator.Evaluate("${value}"));

		evaluator.Formatted += (_, context) => context.Text = null;
		Assert.Equal("", evaluator.Evaluate("${value}"));
	}

	[Fact]
	public void IndexSyntaxDepthDoesNotConsumeTemplateDepth()
	{
		var evaluator = Create(new() { ["arr"] = new List<int> { 0 } });
		evaluator.Options.MaximumDepth = 1;
		Assert.Equal("0", evaluator.Evaluate("${arr[arr[arr[0]]]}"));
		var expression = "0";

		for(int i = 0; i < 260; i++)
			expression = "arr[" + expression + "]";

		Assert.Equal("SyntaxDepthExceeded", Assert.Throws<TemplateEvaluationException>(() => evaluator.Evaluate("${" + expression + "}")).Code);
	}

	private static TemplateEvaluator Create(Dictionary<string, object> values)
	{
		var evaluator = new TemplateEvaluator();
		evaluator.Providers.Add(new Provider((name, scope) =>
			values.TryGetValue(scope == null ? name : scope + ":" + name, out var value) ? (true, value) : (false, null)));
		return evaluator;
	}

	private static void AssertReflectorNavigation(object target, string name, string expression, params object[] arguments)
	{
		var evaluator = Create(new() { ["value"] = target });
		object expected = null;
		var failure = Record.Exception(() => expected = Reflection.Reflector.GetValue(ref target, name, arguments));
		var success = evaluator.TryEvaluate("${" + expression + "}", out var actual, out var error);

		if(failure == null)
		{
			Assert.True(success);
			Assert.Null(error);
			Assert.Equal(expected?.ToString() ?? string.Empty, actual);
		}
		else
		{
			Assert.False(success);
			Assert.Null(actual);
			Assert.Equal("NavigationFailed", error.Code);
			Assert.Equal(TemplateEvaluationStage.Resolution, error.Stage);
			Assert.Equal(expression, error.Expression);
			Assert.Equal(failure.GetType(), error.InnerException?.GetType());
		}
	}

	private sealed class Provider(Func<string, string, (bool Found, object Value)> get) : IVariables
	{
		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string @namespace, string name, out object value)
		{
			var result = get(name, string.IsNullOrEmpty(@namespace) ? null : @namespace);
			value = result.Value;
			return result.Found;
		}
	}

	private sealed class Person
	{
		private int _counter;
		public string Name { get; set; }
		public Person Home { get; set; }
		public string Field = "field";
		public int Next => ++_counter;
		public object Broken => throw new NotSupportedException();
		public static string Static => "static";
		private string Secret => "secret";
		public string WriteOnly { set { } }
		public string PrivateGetter { private get; set; }
	}

	private sealed class IntegerIndexer { public int this[int index] => index; }
	private sealed class PairIndexer { public string this[int number, string text] => number + ":" + text; }
	private sealed class AmbiguousMember
	{
		public string Name => "one";
		public string NAME => "two";
	}
	private class BasePerson { public virtual string Name => "base"; }
	private sealed class DerivedPerson : BasePerson { public override string Name => "derived"; }
	private sealed class FormatEcho : IFormattable { public string ToString(string format, IFormatProvider provider) => format; }
	private sealed class EchoIndexer
	{
		public object Last { get; private set; }
		public object this[object index] => this.Last = index;
	}
	private sealed class OverloadedIndexer
	{
		public string this[int index] => "int";
		public string this[string index] => "string";
		public string this[object index] => "object";
	}
	private sealed class AmbiguousIndexer
	{
		public string this[long index] => throw new Exception("must not run");
		public string this[decimal index] => throw new Exception("must not run");
	}
	private sealed class AssignableIndexer
	{
		public string this[IComparable index] => "comparable";
		public string this[object index] => "object";
	}
	private sealed class ReadOnlyMap<T>(Dictionary<int, T> values) : IReadOnlyDictionary<int, T>
	{
		T IReadOnlyDictionary<int, T>.this[int key] => values[key];
		IEnumerable<int> IReadOnlyDictionary<int, T>.Keys => values.Keys;
		IEnumerable<T> IReadOnlyDictionary<int, T>.Values => values.Values;
		int IReadOnlyCollection<KeyValuePair<int, T>>.Count => values.Count;
		bool IReadOnlyDictionary<int, T>.ContainsKey(int key) => values.ContainsKey(key);
		bool IReadOnlyDictionary<int, T>.TryGetValue(int key, out T value) => values.TryGetValue(key, out value);
		IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator() => values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
	}
	private sealed class ThrowingFormattable : IFormattable
	{
		public string ToString(string format, IFormatProvider provider) => throw new NotSupportedException();
	}
}
