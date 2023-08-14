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
#elif NET7_0
	[SimpleJob(RuntimeMoniker.Net70, 1, 0, 3)]
#endif
	[MemoryDiagnoser]
	[RPlotExporter, HtmlExporter, MarkdownExporter]
	public class ModelPropertySetterBenchmark
	{
		private Person _dynamic;
		private PersonModel _native;

		[Params(100, 1000, 1_0000)]
		public int Count = 0;

		[GlobalSetup]
		public void Setup()
		{
			_dynamic = Model.Build<Person>();
			_dynamic.Name = "Popeye Zhong";
			_dynamic.Gender = Gender.Male;
			_dynamic.Birthdate = DateTime.Now;
			_dynamic.BloodType = "AB";
			_dynamic.HomeAddress = null;

			_native = new PersonModel();
			_native.Name = "Popeye Zhong";
			_native.Gender = Gender.Male;
			_native.Birthdate = DateTime.Now;
			_native.BloodType = "AB";
			_native.HomeAddress = null;
		}

		[Benchmark(Baseline = true)]
		public void NativeSet()
		{
			var count = this.Count;
			var model = _native;

			for(int i = 0; i < count; i++)
			{
				model.Name = "Popeye Zhong";
				model.Gender = Gender.Male;
				model.Birthdate = DateTime.Now;
				model.BloodType = "AB";
				model.HomeAddress = null;
			}
		}

		[Benchmark]
		public void DynamicSet()
		{
			var count = this.Count;
			var model = _dynamic;

			for(int i = 0; i < count; i++)
			{
				model.Name = "Popeye Zhong";
				model.Gender = Gender.Male;
				model.Birthdate = DateTime.Now;
				model.BloodType = "AB";
				model.HomeAddress = null;
			}
		}

		[Benchmark]
		public void ModelTrySet()
		{
			var count = this.Count;
			var model = _dynamic as IModel;

			for(int i = 0; i < count; i++)
			{
				model.TrySetValue(nameof(Person.Name), "Popeye Zhong");
				model.TrySetValue(nameof(Person.Gender), Gender.Male);
				model.TrySetValue(nameof(Person.Birthdate), DateTime.Now);
				model.TrySetValue(nameof(Person.BloodType), "AB");
				model.TrySetValue(nameof(Person.HomeAddress), null);
			}
		}
	}
}
