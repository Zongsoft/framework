﻿using System;
using System.Linq;
using System.Reflection;

using BenchmarkDotNet;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Attributes;

using Zongsoft.Reflection;

namespace Zongsoft.Benchmarks;

#if NET5_0
[SimpleJob(RuntimeMoniker.Net50)]
#elif NET6_0
[SimpleJob(RuntimeMoniker.Net60)]
#elif NET7_0
[SimpleJob(RuntimeMoniker.Net70)]
#elif NET8_0
[SimpleJob(RuntimeMoniker.Net80)]
#elif NET9_0
[SimpleJob(RuntimeMoniker.Net90)]
#endif
[MemoryDiagnoser]
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

	[Benchmark]
	public void DirectGetProperty()
	{
		var count = this.Count;
		var target = PersonModel.Create();
		var getters = GetProperties(target.GetType()).Select(property => property.GetGetter<PersonModel>()).ToArray();

		for(int i = 0; i < count; i++)
		{
			for(int j = 0; j < getters.Length; j++)
			{
				getters[j].Invoke(ref target);
			}
		}
	}

	private static PropertyInfo[] GetProperties(Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}
