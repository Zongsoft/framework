using System;
using System.Collections.Generic;

using Xunit;

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

	private static int Add(int a, int b) => a + b;
	private static int Subtract(int a, int b) => a - b;
}