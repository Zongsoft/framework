using System;
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
public class PropertySetterBenchmark
{
	[Params(100, 1_000, 1_0000, 10_0000)]
	public int Count = 0;

	[GlobalSetup]
	public void Setup()
	{
		var target = PersonModel.Create();
		(var name, var gender, var birthdate, var address) = GetProperties();

		Reflector.SetValue(name, ref target, "Popeye Zhong");
		Reflector.SetValue(gender, ref target, Gender.Male);
		Reflector.SetValue(birthdate, ref target, DateTime.Now);
		Reflector.SetValue(address, ref target, null);
	}

	[Benchmark(Baseline = true)]
	public void SetProperty()
	{
		var count = this.Count;
		var target = new PersonModel();
		(var name, var gender, var birthdate, var address) = GetProperties();

		for(int i = 0; i < count; i++)
		{
			name.SetValue(target, "Popeye Zhong");
			gender.SetValue(target, Gender.Male);
			birthdate.SetValue(target, DateTime.Now);
			address.SetValue(target, new Address
			{
				City = "Shanghai",
				Detail = "Pudong New Area, Zhangjiang High-Tech Park",
				PostalCode = "201203",
				CountryId = 86
			});
		}
	}

	[Benchmark]
	public void FastSetProperty()
	{
		var count = this.Count;
		var target = new PersonModel();
		(var name, var gender, var birthdate, var address) = GetProperties();

		for(int i = 0; i < count; i++)
		{
			Reflector.SetValue(name, ref target, "Popeye Zhong");
			Reflector.SetValue(gender, ref target, Gender.Male);
			Reflector.SetValue(birthdate, ref target, DateTime.Now);
			Reflector.SetValue(address, ref target, new Address
			{
				City = "Shanghai",
				Detail = "Pudong New Area, Zhangjiang High-Tech Park",
				PostalCode = "201203",
				CountryId = 86
			});
		}
	}

	[Benchmark]
	public void DirectSetProperty()
	{
		var count = this.Count;
		var target = new PersonModel();
		(var name, var gender, var birthdate, var address) = GetSetters();

		for(int i = 0; i < count; i++)
		{
			name.Invoke(ref target, "Popeye Zhong");
			gender.Invoke(ref target, Gender.Male);
			birthdate.Invoke(ref target, DateTime.Now);
			address.Invoke(ref target, new Address
			{
				City = "Shanghai",
				Detail = "Pudong New Area, Zhangjiang High-Tech Park",
				PostalCode = "201203",
				CountryId = 86
			});
		}
	}

	private static (PropertyInfo name, PropertyInfo gender, PropertyInfo birthdate, PropertyInfo address) GetProperties()
	{
		return (
			typeof(PersonModel).GetProperty(nameof(PersonModel.Name), BindingFlags.Public | BindingFlags.Instance),
			typeof(PersonModel).GetProperty(nameof(PersonModel.Gender), BindingFlags.Public | BindingFlags.Instance),
			typeof(PersonModel).GetProperty(nameof(PersonModel.Birthdate), BindingFlags.Public | BindingFlags.Instance),
			typeof(PersonModel).GetProperty(nameof(PersonModel.HomeAddress), BindingFlags.Public | BindingFlags.Instance)
		);
	}

	private static (PropertyInfoExtension.Setter<PersonModel> name, PropertyInfoExtension.Setter<PersonModel> gender, PropertyInfoExtension.Setter<PersonModel> birthdate, PropertyInfoExtension.Setter<PersonModel> address) GetSetters()
	{
		return (
			typeof(PersonModel).GetProperty(nameof(PersonModel.Name), BindingFlags.Public | BindingFlags.Instance).GetSetter<PersonModel>(),
			typeof(PersonModel).GetProperty(nameof(PersonModel.Gender), BindingFlags.Public | BindingFlags.Instance).GetSetter<PersonModel>(),
			typeof(PersonModel).GetProperty(nameof(PersonModel.Birthdate), BindingFlags.Public | BindingFlags.Instance).GetSetter<PersonModel>(),
			typeof(PersonModel).GetProperty(nameof(PersonModel.HomeAddress), BindingFlags.Public | BindingFlags.Instance).GetSetter<PersonModel>()
		);
	}
}
