using System;
using System.IO;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Expressions;

namespace Zongsoft.Externals.Python.Tests;

public class PythonExpressionEvaluatorTest
{
	[Fact]
	public void TestEvaluate1()
	{
		var evaluator = new PythonExpressionEvaluator();
		var variables = new Dictionary<string, object>();

		var result = evaluator.Evaluate("1+2", null);
		Assert.NotNull(result);
		Assert.Equal(3, Zongsoft.Common.Convert.ConvertValue<int>(result));

		variables["subtract"] = (Delegate)Subtract;
		result = evaluator.Evaluate("subtract(100, 20)", variables);
		Assert.NotNull(result);
		Assert.Equal(80, Zongsoft.Common.Convert.ConvertValue<int>(result));

		evaluator.Evaluate("a=1;b=2;result=a+b;", variables);
		Assert.NotEmpty(variables);
		Assert.True(variables.TryGetValue("result", out result));
		Assert.Equal(3, Zongsoft.Common.Convert.ConvertValue<int>(result));
	}

	[Fact]
	public void TestEvaluate2()
	{
		var evaluator = new PythonExpressionEvaluator();
		evaluator.Global["add"] = (Delegate)Add;
		evaluator.Global["subtract"] = (Delegate)Subtract;

		var variables = new Dictionary<string, object>
		{
			{ "x",  100 },
			{ "y",  200 },
			{ "z",  300 },
		};

		var result = evaluator.Evaluate("x+y+z", variables);
		Assert.NotNull(result);
		Assert.Equal(600, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("add(x, 10)", variables);
		Assert.NotNull(result);
		Assert.Equal(110, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("a=70; result=subtract(x, a);", variables);
		Assert.True(variables.TryGetValue("result", out result));
		Assert.Equal(30, Zongsoft.Common.Convert.ConvertValue<int>(result));
	}

	[Fact]
	public void TestEvaluateOutput()
	{
		const string PRINT_MESSAGE = "Hello, World!";
		const string ERROR_MESSAGE = "This is an error message.";

		using var evaluator = new PythonExpressionEvaluator();

		using var error = new MemoryStream();
		using var output = new MemoryStream();
		var options = ExpressionEvaluatorOptions
			.Out(new StreamWriter(output, System.Text.Encoding.UTF8))
			.Error(new StreamWriter(error, System.Text.Encoding.UTF8));

		var script = $$"""
		# print()
		print("{{PRINT_MESSAGE}}")

		# error()
		error("{{ERROR_MESSAGE}}")
		""";

		evaluator.Evaluate(script, options);

		Assert.True(error.Length > 0);
		Assert.True(output.Length > 0);

		error.Seek(0, SeekOrigin.Begin);
		output.Seek(0, SeekOrigin.Begin);

		var text = options.Error.Encoding.GetString(error.ToArray());
		Assert.NotEmpty(text);
		Assert.Equal(ERROR_MESSAGE, text.Trim());

		text = options.Output.Encoding.GetString(output.ToArray());
		Assert.NotEmpty(text);
		Assert.Equal(PRINT_MESSAGE, text.Trim());
	}

	[Fact]
	public void TestEvaluateSerializeJson()
	{
		using var evaluator = new PythonExpressionEvaluator();

		var script = """
			obj = { 'id' : 100, 'name' : 'Popeye'}
			result = Json.Serialize(obj)
			""";

		var result = evaluator.Evaluate(script);
		Assert.NotNull(result);

		var variables = new Dictionary<string, object>() { { "text", result } };
		result = evaluator.Evaluate(@"obj = Json.Deserialize(text); obj['id']=200; result = obj;", variables);
		Assert.NotNull(result);
		Assert.IsType<IDictionary<string, object>>(result, false);

		if(result is IDictionary<string, object> dictionary)
		{
			Assert.Equal(2, dictionary.Count);
			Assert.Equal(200, dictionary["id"]);
			Assert.Equal("Popeye", dictionary["name"]);
		}
	}

	[Fact]
	public void TestEvaluateDeserializeJson()
	{
		var json = $$"""
		{
			"id":100,
			"name":"MyName",
			"number":1.23,
			"boolean":true,
			"integers":[10,20,30],
			"object":{
				"integer":200,
				"nothing":null
			},
			"objects":[
				{ "boolean":false, "number":12.3 },
				{ "string":"text", "integer":100 }
			]
		}
		""";

		var script = """
		obj=Json.Deserialize(text)

		print(obj)

		print(obj['id'])
		print(obj['name'])
		print(obj['number'])
		print(obj['boolean'])

		for i in range(len(obj['integers'])):
			print(i, obj['integers'][i])

		print(obj['object']['integer'])
		print(obj['object']['nothing'])

		print("objects length: ", len(obj['objects']))

		for i in range(len(obj['objects'])):
			print(i, obj['objects'][i])

		result = obj
		""";

		using var evaluator = new PythonExpressionEvaluator();

		var result = evaluator.Evaluate(script, new Dictionary<string, object> { { "text", json } });
		Assert.NotNull(result);
		Assert.IsType<IDictionary<string, object>>(result, false);

		if(result is IDictionary<string, object> dictionary)
		{
			Assert.Equal(7, dictionary.Count);
			Assert.Equal(100, dictionary["id"]);
			Assert.Equal("MyName", dictionary["name"]);
			Assert.Equal(1.23, dictionary["number"]);
			Assert.Equal(true, dictionary["boolean"]);
			Assert.IsType<object[]>(dictionary["integers"]);
			Assert.Equal([10, 20, 30], (object[])dictionary["integers"]);

			Assert.IsType<Dictionary<string, object>>(dictionary["object"]);
			var children = (IDictionary<string, object>)dictionary["object"];
			Assert.Equal(2, children.Count);
			Assert.Equal(200, children["integer"]);
			Assert.Null(children["nothing"]);

			Assert.IsType<object[]>(dictionary["objects"]);
			var array = (object[])dictionary["objects"];
			Assert.Equal(2, array.Length);
			Assert.NotEqual(0, array[0]);
			Assert.IsType<Dictionary<string, object>>(array[0]);
			children = (IDictionary<string, object>)array[0];
			Assert.Equal(2, children.Count);
			Assert.Equal(false, children["boolean"]);
			Assert.Equal(12.3, children["number"]);

			Assert.NotEqual(0, array[1]);
			Assert.IsType<Dictionary<string, object>>(array[1]);
			children = (IDictionary<string, object>)array[1];
			Assert.Equal(2, children.Count);
			Assert.Equal("text", children["string"]);
			Assert.Equal(100, children["integer"]);
		}
	}

	private static int Add(int a, int b) => a + b;
	private static int Subtract(int a, int b) => a - b;
}