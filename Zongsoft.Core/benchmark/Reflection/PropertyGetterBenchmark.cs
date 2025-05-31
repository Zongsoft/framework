using System;
using System.Reflection;

using BenchmarkDotNet;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Attributes;

using Zongsoft.Reflection;

namespace Zongsoft.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60, 1, 0, 3)]
[SimpleJob(RuntimeMoniker.Net70, 1, 0, 3)]
[SimpleJob(RuntimeMoniker.Net80, 1, 0, 3)]
[SimpleJob(RuntimeMoniker.Net90, 1, 0, 3)]
[RPlotExporter, HtmlExporter, MarkdownExporter]
public class PropertyGetterBenchmark
{
	[Params(100, 1_000, 1_0000, 10_0000)]
	public int Count = 0;

	[GlobalSetup]
	public void Setup()
	{
		var target = PersonModel.Create();
		var properties = GetProperties(target.GetType());

		for(int i = 0; i < properties.Length; i++)
			Reflector.GetValue(properties[i], ref target);
	}

	[Benchmark(Baseline = true)]
	public void GetProperty()
	{
		var count = this.Count;
		var target = PersonModel.Create();
		var properties = GetProperties(target.GetType());

		for(int i = 0; i < count; i++)
		{
			for(int j = 0; j < properties.Length; j++)
			{
				var property = properties[j];
				property.GetValue(target);
			}
		}
	}

	[Benchmark]
	public void FastGetProperty()
	{
		var count = this.Count;
		var target = PersonModel.Create();
		var properties = GetProperties(target.GetType());

		for(int i = 0; i < count; i++)
		{
			for(int j = 0; j < properties.Length; j++)
			{
				var property = properties[j];
				Reflector.GetValue(property, ref target);
			}
		}
	}

	private static PropertyInfo[] GetProperties(Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}
