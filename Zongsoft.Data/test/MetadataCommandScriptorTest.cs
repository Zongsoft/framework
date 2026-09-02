using System;
using System.IO;

using Xunit;

using Zongsoft.Data.Metadata.Profiles;

namespace Zongsoft.Data.Tests;

public class MetadataCommandScriptorTest
{
	[Fact]
	public void Load_FlatQualifiedScript_LoadsDriverFromFileNameSuffix()
	{
		const string DRIVER = "Mock";
		const string SCRIPT = "SELECT 'qualified';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var command = new MetadataCommand("Testing", "QualifiedCommand");

		try
		{
			File.WriteAllText(Path.Combine(scriptsDirectory, $"{command.QualifiedName}-{DRIVER}.sql"), SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Equal(DRIVER, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(SCRIPT, command.Scriptor.GetScript(DRIVER));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Load_FlatLegacyScript_RemainsCompatible()
	{
		const string DRIVER = "Mock";
		const string SCRIPT = "SELECT 'legacy';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var command = new MetadataCommand("Testing", "LegacyCommand");

		try
		{
			File.WriteAllText(Path.Combine(scriptsDirectory, $"{command.Name}-{DRIVER}.sql"), SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Equal(DRIVER, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(SCRIPT, command.Scriptor.GetScript(DRIVER));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Load_FlatQualifiedAndLegacyScripts_QualifiedOverridesLegacy()
	{
		const string DRIVER = "Mock";
		const string LEGACY_SCRIPT = "SELECT 'legacy';";
		const string QUALIFIED_SCRIPT = "SELECT 'qualified';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var command = new MetadataCommand("Testing", "OverrideCommand");

		try
		{
			File.WriteAllText(Path.Combine(scriptsDirectory, $"{command.Name}-{DRIVER}.sql"), LEGACY_SCRIPT);
			File.WriteAllText(Path.Combine(scriptsDirectory, $"{command.QualifiedName}-{DRIVER}.sql"), QUALIFIED_SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Equal(DRIVER, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(QUALIFIED_SCRIPT, command.Scriptor.GetScript(DRIVER));
			Assert.NotEqual(LEGACY_SCRIPT, command.Scriptor.GetScript(DRIVER));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("scripts")]
	public void Load_ContainerScriptWithoutDriverSuffix_DoesNotInferDriverFromContainerDirectory(string relativeDirectory)
	{
		const string SCRIPT = "SELECT 'missing-driver';";
		var directory = CreateTemporaryDirectory();
		var container = string.IsNullOrEmpty(relativeDirectory) ? directory : Directory.CreateDirectory(Path.Combine(directory, relativeDirectory)).FullName;
		var command = new MetadataCommand("Testing", "MissingDriverCommand");

		try
		{
			File.WriteAllText(Path.Combine(container, $"{command.QualifiedName}.sql"), SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Empty(command.Scriptor.Drivers);
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Theory]
	[InlineData("mysql")]
	[InlineData("mssql")]
	[InlineData("sqlite")]
	[InlineData("postgres")]
	public void Load_DriverDirectoryQualifiedScript_InfersDriverFromDirectory(string driver)
	{
		var script = $"SELECT '{driver}';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var driverDirectory = Directory.CreateDirectory(Path.Combine(scriptsDirectory, driver)).FullName;
		var command = new MetadataCommand("Messaging", "Storages.Get");

		try
		{
			File.WriteAllText(Path.Combine(driverDirectory, $"{command.QualifiedName}.sql"), script);

			command.Scriptor.Load(directory);

			Assert.Equal(driver, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(script, command.Scriptor.GetScript(driver));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Load_DriverDirectoryLegacyScript_RemainsCompatible()
	{
		const string DRIVER = "mysql";
		const string SCRIPT = "SELECT 'directory-legacy';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var driverDirectory = Directory.CreateDirectory(Path.Combine(scriptsDirectory, DRIVER)).FullName;
		var command = new MetadataCommand("Testing", "LegacyDirectoryCommand");

		try
		{
			File.WriteAllText(Path.Combine(driverDirectory, $"{command.Name}.sql"), SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Equal(DRIVER, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(SCRIPT, command.Scriptor.GetScript(DRIVER));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[Fact]
	public void Load_FlatLegacyAndDirectoryQualifiedScripts_QualifiedOverridesLegacy()
	{
		const string DRIVER = "mysql";
		const string LEGACY_SCRIPT = "SELECT 'flat-legacy';";
		const string QUALIFIED_SCRIPT = "SELECT 'directory-qualified';";
		var directory = CreateTemporaryDirectory();
		var scriptsDirectory = CreateScriptsDirectory(directory);
		var driverDirectory = Directory.CreateDirectory(Path.Combine(scriptsDirectory, DRIVER)).FullName;
		var command = new MetadataCommand("Testing", "CrossLayoutOverrideCommand");

		try
		{
			File.WriteAllText(Path.Combine(scriptsDirectory, $"{command.Name}-{DRIVER}.sql"), LEGACY_SCRIPT);
			File.WriteAllText(Path.Combine(driverDirectory, $"{command.QualifiedName}.sql"), QUALIFIED_SCRIPT);

			command.Scriptor.Load(directory);

			Assert.Equal(DRIVER, Assert.Single(command.Scriptor.Drivers));
			Assert.Equal(QUALIFIED_SCRIPT, command.Scriptor.GetScript(DRIVER));
			Assert.NotEqual(LEGACY_SCRIPT, command.Scriptor.GetScript(DRIVER));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private static string CreateTemporaryDirectory()
	{
		var directory = Path.Combine(Path.GetTempPath(), $"Zongsoft.Data.Tests-{Guid.NewGuid():N}");
		return Directory.CreateDirectory(directory).FullName;
	}

	private static string CreateScriptsDirectory(string directory) => Directory.CreateDirectory(Path.Combine(directory, "scripts")).FullName;
}
