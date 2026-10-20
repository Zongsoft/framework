using System;

using Xunit;

namespace Zongsoft.Data.Tests;

public class DataPropertySequenceTest
{
	[Fact]
	public void TestParse()
	{
		var sequence = DataPropertySequence.Parse(null);
		Assert.Null(sequence.Name);
		sequence = DataPropertySequence.Parse(string.Empty);
		Assert.Null(sequence.Name);
		sequence = DataPropertySequence.Parse("\t");
		Assert.Null(sequence.Name);

		sequence = DataPropertySequence.Parse("*");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = DataPropertySequence.Parse("* ");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = DataPropertySequence.Parse(" * ");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = DataPropertySequence.Parse(" #");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = DataPropertySequence.Parse(" # ");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("*SequenceName");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("*SequenceName@100");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("*SequenceName/10");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("*SequenceName@1/2");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("Namespace.Sequence:Id");
		Assert.Equal("Namespace.Sequence:Id", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#SequenceName");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#SequenceName@100");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#SequenceName/10");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#SequenceName@1/2");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = DataPropertySequence.Parse("#(CorporationId)");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Single(sequence.References);
		Assert.Equal("CorporationId", sequence.References[0], true);

		sequence = DataPropertySequence.Parse("#(TenantId,BranchId)");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Equal(2, sequence.References.Length);
		Assert.Equal("TenantId", sequence.References[0], true);
		Assert.Equal("BranchId", sequence.References[1], true);
	}

	[Fact]
	public void TestConvert()
	{
		Assert.False(Common.Convert.TryConvertValue<DataPropertySequence>(string.Empty, out _));
		Assert.False(Common.Convert.TryConvertValue<DataPropertySequence>("\t", out _));

		var sequence = Common.Convert.ConvertValue<DataPropertySequence>("*");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = Common.Convert.ConvertValue<DataPropertySequence>("* ");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = Common.Convert.ConvertValue<DataPropertySequence>(" * ");
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = Common.Convert.ConvertValue<DataPropertySequence>(" #");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);
		sequence = Common.Convert.ConvertValue<DataPropertySequence>(" # ");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("*SequenceName");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("*SequenceName@100");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("*SequenceName/10");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("*SequenceName@1/2");
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("Namespace.Sequence:Id");
		Assert.Equal("Namespace.Sequence:Id", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#SequenceName");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#SequenceName@100");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#SequenceName/10");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#SequenceName@1/2");
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#(CorporationId)");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Single(sequence.References);
		Assert.Equal("CorporationId", sequence.References[0], true);

		sequence = Common.Convert.ConvertValue<DataPropertySequence>("#(TenantId,BranchId)");
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Equal(2, sequence.References.Length);
		Assert.Equal("TenantId", sequence.References[0], true);
		Assert.Equal("BranchId", sequence.References[1], true);
	}

	[Fact]
	public void TestSerialize()
	{
		var json = Serialization.Serializer.Json.Serialize(default(DataPropertySequence));
		Assert.Equal("null", json);
		var sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Null(sequence.Name);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("*"));
		Assert.Equal("*", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("*", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#"));
		Assert.Equal("#", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("*SequenceName"));
		Assert.Equal("*SequenceName", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("*SequenceName@100"));
		Assert.Equal("*SequenceName@100", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("*SequenceName/10"));
		Assert.Equal("*SequenceName/10", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("*SequenceName@1/2"));
		Assert.Equal("*SequenceName@1/2", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("*SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.True(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("Namespace.Sequence:Id"));
		Assert.Equal("Namespace.Sequence:Id", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("Namespace.Sequence:Id", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.False(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#SequenceName"));
		Assert.Equal("#SequenceName", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#SequenceName@100"));
		Assert.Equal("#SequenceName@100", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(100, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#SequenceName/10"));
		Assert.Equal("#SequenceName/10", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(10, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#SequenceName@1/2"));
		Assert.Equal("#SequenceName@1/2", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#SequenceName", sequence.Name, true);
		Assert.Equal(1, sequence.Seed);
		Assert.Equal(2, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.False(sequence.HasReferences);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#(CorporationId)"));
		Assert.Equal("#(CorporationId)", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Single(sequence.References);
		Assert.Equal("CorporationId", sequence.References[0], true);

		json = Serialization.Serializer.Json.Serialize(DataPropertySequence.Parse("#(TenantId,BranchId)"));
		Assert.Equal("#(TenantId,BranchId)", json.Trim('"'), true);
		sequence = Serialization.Serializer.Json.Deserialize<DataPropertySequence>(json);
		Assert.Equal("#", sequence.Name, true);
		Assert.Equal(0, sequence.Seed);
		Assert.Equal(1, sequence.Interval);
		Assert.False(sequence.IsBuiltin);
		Assert.True(sequence.IsExternal);
		Assert.True(sequence.HasReferences);
		Assert.Equal(2, sequence.References.Length);
		Assert.Equal("TenantId", sequence.References[0], true);
		Assert.Equal("BranchId", sequence.References[1], true);
	}
}
