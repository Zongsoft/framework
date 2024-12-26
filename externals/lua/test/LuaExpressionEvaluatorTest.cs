using System;
using System.IO;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Expressions;

namespace Zongsoft.Externals.Lua.Tests;

public class LuaExpressionEvaluatorTest
{
	[Fact]
	public void TestEvaluateOutput()
	{
		const string PRINT_MESSAGE = "Hello, World!";
		const string ERROR_MESSAGE = "This is an error message.";

		using var evaluator = new LuaExpressionEvaluator();

		using var error = new MemoryStream();
		using var output = new MemoryStream();
		var options = ExpressionEvaluatorOptions
			.Out(new StreamWriter(output))
			.Error(new StreamWriter(error));

		var script = $$"""
		print();
		print("{{PRINT_MESSAGE}}");
		error();
		error("{{ERROR_MESSAGE}}");
		""";

		evaluator.Evaluate(script, options);

		Assert.True(error.Length > 0);
		Assert.True(output.Length > 0);

		error.Seek(0, SeekOrigin.Begin);
		output.Seek(0, SeekOrigin.Begin);

		var text = options.Error.Encoding.GetString(error.ToArray());
		Assert.NotEmpty(text);
		Assert.Equal(ERROR_MESSAGE, text);

		text = options.Output.Encoding.GetString(output.ToArray());
		Assert.NotEmpty(text);
		Assert.Equal(PRINT_MESSAGE, text);
	}

	[Fact]
	public void TestEvaluateResult()
	{
		using var evaluator = new LuaExpressionEvaluator();
		var result = evaluator.Evaluate(@"
		result = { id = 123, name = ""MyName""};
		result.gender = true;
		return result;");

		Assert.NotNull(result);
		Assert.IsType<Dictionary<string, object>>(result);
		var dictionary = (Dictionary<string, object>)result;
		Assert.True(dictionary.TryGetValue("id", out var id));
		Assert.Equal(123L, id);
		Assert.True(dictionary.TryGetValue("name", out var name));
		Assert.Equal("MyName", name);
		Assert.True(dictionary.TryGetValue("gender", out var gender));
		Assert.IsType<bool>(gender);
		Assert.True((bool)gender);

		result = evaluator.Evaluate(@"
		local list = list();
		list:Add(100);
		list:Add(""ABC"");
		return list;");

		Assert.NotNull(result);
		Assert.IsType<List<object>>(result);
		var list = (List<object>)result;
		Assert.NotNull(list);
		Assert.NotEmpty(list);
		Assert.Equal(2, list.Count);
		Assert.Equal(100L, list[0]);
		Assert.Equal("ABC", list[1]);

		result = evaluator.Evaluate(@"
		local array = array(10);
		array[0] = 100;
		array[1] = ""ABC"";
		return array;");

		Assert.NotNull(result);
		Assert.IsType<object[]>(result);
		var array = (object[])result;
		Assert.NotNull(array);
		Assert.NotEmpty(array);
		Assert.Equal(10, array.Length);
		Assert.Equal(100L, array[0]);
		Assert.Equal("ABC", array[1]);

		result = evaluator.Evaluate(@"
		local dict = dictionary();
		dict[""Integer""] = 100;
		dict[""String""] = ""ABC"";
		return dict;");

		Assert.NotNull(result);
		Assert.IsType<Dictionary<string, object>>(result);
		var dict = (Dictionary<string, object>)result;
		Assert.NotNull(dict);
		Assert.NotEmpty(dict);
		Assert.Equal(2, dict.Count);
		Assert.Equal(100L, dict["Integer"]);
		Assert.Equal("ABC", dict["String"]);
	}

	[Fact]
	public void TestEvaluateSerializeJson()
	{
		using var evaluator = new LuaExpressionEvaluator();

		var result = evaluator.Evaluate(@"obj = {id = 100, name=""name""}; return Json:Serialize(obj);");
		Assert.NotNull(result);

		var variables = new Dictionary<string, object>() { { "text", result } };
		result = evaluator.Evaluate(@"obj = Json:Deserialize(text); obj.id=200; return obj;", variables);
		Assert.NotNull(result);
		Assert.IsAssignableFrom<IDictionary<string, object>>(result);

		if(result is IDictionary<string, object> dictionary)
		{
			Assert.Equal(2, dictionary.Count);
			Assert.Equal(200L, dictionary["id"]);
			Assert.Equal("name", dictionary["name"]);
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
		obj=Json:Deserialize(text);

		print(obj.id);
		print(obj.name);
		print(obj.number);
		print(obj.boolean);
		
		for i=0, obj.integers.Length-1 do
			print(i, obj.integers[i])
		end

		print(obj.object.integer);
		print(obj.object.nothing);

		print("objects length: " .. obj.objects.Length);

		for i=0, obj.objects.Length-1 do
			print(i, obj.objects[i])
		end

		return obj;
		""";

		using var evaluator = new LuaExpressionEvaluator();

		var result = evaluator.Evaluate(script, new Dictionary<string, object> { { "text", json } });
		Assert.NotNull(result);
		Assert.IsAssignableFrom<IDictionary<string, object>>(result);

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

	[Fact]
	public void TestEvaluateInvoke()
	{
		using var evaluator = new LuaExpressionEvaluator();
		evaluator.Global["add"] = Add;
		evaluator.Global["subtract"] = Subtract;

		var result = evaluator.Evaluate("return 1+2", null);
		Assert.NotNull(result);
		Assert.Equal(3, Zongsoft.Common.Convert.ConvertValue<int>(result));

		var variables = new Dictionary<string, object>
		{
			{ "x",  100 },
			{ "y",  200 },
			{ "z",  300 },
		};

		result = evaluator.Evaluate("return x+y+z", variables);
		Assert.NotNull(result);
		Assert.Equal(600, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("return add(x, 10)", variables);
		Assert.NotNull(result);
		Assert.Equal(110, Zongsoft.Common.Convert.ConvertValue<int>(result));
	}

	private static int Add(int a, int b) => a + b;
	private static int Subtract(int a, int b) => a - b;
}