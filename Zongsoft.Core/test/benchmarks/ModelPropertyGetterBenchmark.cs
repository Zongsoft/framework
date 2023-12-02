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
#elif NET8_0
	[SimpleJob(RuntimeMoniker.Net80, 1, 0, 3)]
#endif
	[MemoryDiagnoser]
	[RPlotExporter, HtmlExporter, MarkdownExporter]
	public class ModelPropertyGetterBenchmark
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
		public object NativeGet()
		{
			var count = this.Count;
			var model = _native;

			string name = null, bloodtype = null;
			Gender? gender = null;
			DateTime? birthdate = null;
			Address address = null;

			for(int i = 0; i < count; i++)
			{
				name = model.Name;
				gender = model.Gender;
				birthdate = model.Birthdate;
				bloodtype = model.BloodType;
				address = model.HomeAddress;
			}

			return (name, gender, birthdate, bloodtype, address);
		}

		[Benchmark]
		public object DynamicGet()
		{
			var count = this.Count;
			var model = _dynamic;

			string name = null, bloodtype = null;
			Gender? gender = null;
			DateTime? birthdate = null;
			Address address = null;

			for(int i = 0; i < count; i++)
			{
				name = model.Name;
				gender = model.Gender;
				birthdate = model.Birthdate;
				bloodtype = model.BloodType;
				address = model.HomeAddress;
			}

			return (name, gender, birthdate, bloodtype, address);
		}

		[Benchmark]
		public object ModelTryGet()
		{
			var count = this.Count;
			var model = _dynamic as IModel;
			object name = null, gender = null, birthdate = null, bloodtype = null, address = null;

			for(int i = 0; i < count; i++)
			{
				model.TryGetValue(nameof(Person.Name), out name);
				model.TryGetValue(nameof(Person.Gender), out gender);
				model.TryGetValue(nameof(Person.Birthdate), out birthdate);
				model.TryGetValue(nameof(Person.BloodType), out bloodtype);
				model.TryGetValue(nameof(Person.HomeAddress), out address);
			}

			return (name, gender, birthdate, bloodtype, address);
		}
	}
}
