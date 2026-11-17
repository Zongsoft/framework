using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Zongsoft.Data;

namespace Zongsoft.Web.Binders.Tests;

public class ValueModelBinderTest
{
	[Theory]
	[InlineData("18~40", 18, 40)]
	[InlineData("", null, null)]
	public async Task BindsRangeStringOverload(string text, int? minimum, int? maximum)
	{
		var context = CreateContext(typeof(Range<int>), text);
		await new RangeModelBinder<int>().BindModelAsync(context);
		Assert.True(context.Result.IsModelSet);
		Assert.True(context.ModelState.IsValid);
		var range = Assert.IsType<Range<int>>(context.Result.Model);
		Assert.Equal(minimum, range.Minimum);
		Assert.Equal(maximum, range.Maximum);
	}

	[Theory]
	[InlineData("18~40", false)]
	[InlineData("18,20,40", true)]
	public async Task BindsMixtureStringOverload(string text, bool multiple)
	{
		var context = CreateContext(typeof(Mixture<int>), text);
		await new MixtureModelBinder<int>().BindModelAsync(context);
		Assert.True(context.Result.IsModelSet);
		Assert.True(context.ModelState.IsValid);
		var mixture = Assert.IsType<Mixture<int>>(context.Result.Model);

		if(multiple)
			Assert.Equal(new[] { 18, 20, 40 }, mixture.Array);
		else
		{
			Assert.Equal(18, mixture.Range.Minimum);
			Assert.Equal(40, mixture.Range.Maximum);
		}
	}

	[Fact]
	public async Task BindsTemporalExpressionsWithArgumentCommas()
	{
		var dates = CreateContext(typeof(Range<DateOnly>), "month(2024,2)");
		await new RangeModelBinder<DateOnly>().BindModelAsync(dates);
		Assert.True(dates.Result.IsModelSet);
		var range = Assert.IsType<Range<DateOnly>>(dates.Result.Model);
		Assert.Equal(new DateOnly(2024, 2, 1), range.Minimum);
		Assert.Equal(new DateOnly(2024, 2, 29), range.Maximum);

		var offsets = CreateContext(typeof(Mixture<DateTimeOffset>), "month(2024,2)");
		await new MixtureModelBinder<DateTimeOffset>().BindModelAsync(offsets);
		Assert.True(offsets.Result.IsModelSet);
		var mixture = Assert.IsType<Mixture<DateTimeOffset>>(offsets.Result.Model);
		Assert.Null(mixture.Array);
		Assert.Equal(new DateTimeOffset(new DateTime(2024, 2, 1)), mixture.Range.Minimum);
		Assert.Equal(new DateTimeOffset(new DateTime(2024, 2, 29, 23, 59, 59, 999)), mixture.Range.Maximum);
	}

	[Fact]
	public async Task ReportsInvalidValuesInModelState()
	{
		var range = CreateContext(typeof(Range<int>), "invalid");
		await new RangeModelBinder<int>().BindModelAsync(range);
		Assert.False(range.Result.IsModelSet);
		Assert.False(range.ModelState.IsValid);
		Assert.Single(range.ModelState["value"].Errors);

		var mixture = CreateContext(typeof(Mixture<int>), "18,invalid,40");
		await new MixtureModelBinder<int>().BindModelAsync(mixture);
		Assert.False(mixture.Result.IsModelSet);
		Assert.False(mixture.ModelState.IsValid);
		Assert.Single(mixture.ModelState["value"].Errors);
	}

	[Theory]
	[InlineData("A~C", false)]
	[InlineData("A,B,D,F", true)]
	public async Task BindsEnumMixtures(string text, bool multiple)
	{
		var context = CreateContext(typeof(Mixture<Kind>), text);
		await new MixtureModelBinder<Kind>().BindModelAsync(context);
		Assert.True(context.Result.IsModelSet);
		Assert.True(context.ModelState.IsValid);
		var mixture = Assert.IsType<Mixture<Kind>>(context.Result.Model);

		if(multiple)
			Assert.Equal(new[] { Kind.A, Kind.B, Kind.D, Kind.F }, mixture.Array);
		else
		{
			Assert.Equal(Kind.A, mixture.Range.Minimum);
			Assert.Equal(Kind.C, mixture.Range.Maximum);
		}
	}

	[Theory]
	[InlineData("A~C")]
	[InlineData("(C~A)")]
	public async Task BindsEnumRanges(string text)
	{
		var context = CreateContext(typeof(Range<Kind>), text);
		await new RangeModelBinder<Kind>().BindModelAsync(context);
		Assert.True(context.Result.IsModelSet);
		Assert.True(context.ModelState.IsValid);
		var range = Assert.IsType<Range<Kind>>(context.Result.Model);
		Assert.Equal(Kind.A, range.Minimum);
		Assert.Equal(Kind.C, range.Maximum);
	}

	private static ModelBindingContext CreateContext(Type type, string text)
	{
		var query = new QueryCollection(new Dictionary<string, StringValues> { ["value"] = text });
		var provider = new QueryStringValueProvider(BindingSource.Query, query, CultureInfo.InvariantCulture);
		var metadata = new EmptyModelMetadataProvider().GetMetadataForType(type);
		return new DefaultModelBindingContext
		{
			ModelName = "value",
			ModelMetadata = metadata,
			ValueProvider = provider,
			ModelState = new ModelStateDictionary(),
		};
	}

	private enum Kind : byte
	{
		A,
		B,
		C,
		D,
		E,
		F,
	}
}
