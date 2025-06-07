using System;

using Xunit;

namespace Zongsoft.Externals.Polly.Tests;

public class FeaturePipelineTest
{
	[Fact]
	public void TestGetPipeline()
	{
		var pipeline = FeaturePipelineManager.Instance.GetPipeline(null);
		Assert.Null(pipeline);
		pipeline = FeaturePipelineManager.Instance.GetPipeline(null, null);
		Assert.Null(pipeline);
		pipeline = FeaturePipelineManager.Instance.GetPipeline([]);
		Assert.Null(pipeline);
		pipeline = FeaturePipelineManager.Instance.GetPipeline(null, []);
		Assert.Null(pipeline);
	}
}
