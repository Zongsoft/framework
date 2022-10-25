using System;

using BenchmarkDotNet;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Attributes;

namespace Zongsoft.Data.Benchmarks
{
#if NET5_0
	[SimpleJob(RuntimeMoniker.Net50, 1, 0, 3)]
#elif NET6_0
	[SimpleJob(RuntimeMoniker.Net60, 1, 0, 3)]
#endif
	[MemoryDiagnoser]
	[RPlotExporter, HtmlExporter, MarkdownExporter]
	public class ModelBuildBenchmark
	{
		[Params(100, 1000, 1_0000)]
		public int Count = 0;

		[GlobalSetup]
		public void Setup()
		{
			Model.Build<IPerson>();
		}

		[Benchmark(Baseline = true)]
		public object Constructor()
		{
			var count = this.Count;
			PersonModel result = null;

			for(int i = 0; i < count; i++)
			{
				result = new PersonModel();
			}

			return result;
		}

		[Benchmark()]
		public object Create()
		{
			var count = this.Count;
			object result = null;
			var creator = Model.GetCreator(typeof(IPerson));

			for(int i = 0; i < count; i++)
			{
				result = creator();
			}

			return result;
		}

		[Benchmark()]
		public object Build()
		{
			var count = this.Count;
			IPerson result = null;

			for(int i = 0; i < count; i++)
			{
				result = Model.Build<IPerson>();
			}

			return result;
		}
	}
}
