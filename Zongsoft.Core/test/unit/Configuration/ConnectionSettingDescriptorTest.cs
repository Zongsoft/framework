using System;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class ConnectionSettingDescriptorTest
{
	[Fact]
	public void TestDescriptorCollection()
	{
		var descriptors = EmptyConnectionSettingsDriver.Instance.Descriptors;
		Assert.Empty(descriptors);

		descriptors.Add("P1", typeof(int), 100);
		Assert.Single(descriptors);

		descriptors.Add("P2", ["A2"], typeof(string));
		Assert.Equal(2, descriptors.Count);

		Assert.True(descriptors.Contains("P1"));
		Assert.False(descriptors.Contains("A1"));
		Assert.True(descriptors.Contains("P2"));
		Assert.True(descriptors.Contains("A2"));

		Assert.True(descriptors.TryGetValue("P1", out var descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P1", descriptor.Name);
		Assert.Null(descriptor.Aliases);
		Assert.Equal(typeof(int), descriptor.Type);
		Assert.Equal(100, descriptor.DefaultValue);

		Assert.True(descriptors.TryGetValue("P2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));
		Assert.Equal(typeof(string), descriptor.Type);
		Assert.Null(descriptor.DefaultValue);

		Assert.True(descriptors.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));
		Assert.Equal(typeof(string), descriptor.Type);
		Assert.Null(descriptor.DefaultValue);

		descriptors.Remove("P1");
		Assert.Single(descriptors);

		Assert.False(descriptors.Contains("P1"));
		Assert.False(descriptors.Contains("A1"));
		Assert.True(descriptors.Contains("P2"));
		Assert.True(descriptors.Contains("A2"));

		Assert.True(descriptors.TryGetValue("P2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));

		Assert.True(descriptors.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));

		descriptors.Remove("A2");
		Assert.Empty(descriptors);
	}

	private class EmptyConnectionSettingsDriver : ConnectionSettingsDriver<EmptyConnectionSettings>
	{
		public static readonly EmptyConnectionSettingsDriver Instance = new();
		private EmptyConnectionSettingsDriver() : base("Empty") { }
	}

	private class EmptyConnectionSettings : ConnectionSettingsBase<EmptyConnectionSettingsDriver>
	{
		public EmptyConnectionSettings(EmptyConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
		public EmptyConnectionSettings(EmptyConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	}
}
