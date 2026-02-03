using System;

using Xunit;

namespace Zongsoft.Externals.Polly.Tests;

public class FeaturePipelineTest
{
	[Fact]
	public void TestGetPipeline()
	{
		var pipeline = FeaturePipelineBuilder.Instance.Build(null);
		Assert.Null(pipeline);
	}
}
