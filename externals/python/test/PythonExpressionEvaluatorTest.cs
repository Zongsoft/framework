using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Externals.Python.Tests;

public class PythonExpressionEvaluatorTest
{
	[Fact]
	public void TestEvaluate()
	{
		var evaluator = new PythonExpressionEvaluator();
		evaluator.Global["add"] = Add;
		evaluator.Global["subtract"] = Subtract;

		var result = evaluator.Evaluate("1+2", null);
		Assert.NotNull(result);
		Assert.Equal(3, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("subtract(100, 10)", null);
		Assert.NotNull(result);
		Assert.Equal(90, Zongsoft.Common.Convert.ConvertValue<int>(result));

		var variables = new Dictionary<string, object>
		{
			{ "x",  100 },
			{ "y",  200 },
			{ "z",  300 },
		};

		result = evaluator.Evaluate("x+y+z", variables);
		Assert.NotNull(result);
		Assert.Equal(600, Zongsoft.Common.Convert.ConvertValue<int>(result));

		result = evaluator.Evaluate("add(x, 10)", variables);
		Assert.NotNull(result);
		Assert.Equal(110, Zongsoft.Common.Convert.ConvertValue<int>(result));
	}

	private static int Add(int a, int b) => a + b;
	private static int Subtract(int a, int b) => a - b;
}