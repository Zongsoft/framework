using System;
using System.Threading.Tasks;

using MySqlConnector;

using Xunit;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MySql.Tests;

[Collection("Database")]
public class DriverErrorTest(DatabaseFixture database)
{
	[Theory]
	[InlineData("IX_SecondFirst")]
	[InlineData("IX_Second'First")]
	public async Task ConstraintMetadataPreservesIndexNamesAndFieldOrderAsync(string indexName)
	{
		Assert.SkipUnless(Global.IsTestingEnabled, "Database tests require ZONGSOFT_DATA_TESTS=1.");
		var mapping = new DataEntity(null, $"Constraint{Guid.NewGuid():N}");
		mapping.Properties.Simplex("Id", DataType.Int32, false);
		mapping.Properties.Simplex("First", DataType.Int32, false);
		mapping.Properties.Simplex("Second", DataType.Int32, false);
		mapping.SetKey("Id");
		Mapping.Entities.Add(mapping);
		await using var connection = MySqlDriver.Instance.CreateConnection(database.ConnectionSettings.Value);
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();

		try
		{
			command.CommandText = $"CREATE TABLE `{mapping.Name}` (`Id` INT PRIMARY KEY, `First` INT, `Second` INT, UNIQUE KEY `{indexName}` (`Second`,`First`)) ENGINE=InnoDB; " +
				$"INSERT INTO `{mapping.Name}` VALUES (1,10,20);";
			await command.ExecuteNonQueryAsync();
			using var context = new DataDeleteContext(database.Accessor, mapping.Name, Condition.Equal("Id", 1), null);
			await using var session = context.Session;
			Assert.NotNull(await context.Source.GetSchemaAsync("Indexes", true));
			Assert.NotNull(await context.Source.GetSchemaAsync("IndexColumns", true));
			command.CommandText = $"INSERT INTO `{mapping.Name}` VALUES (2,10,20);";
			var error = await Assert.ThrowsAsync<MySqlException>(async () => await command.ExecuteNonQueryAsync());
			var result = Assert.IsType<DataConstraintException>(await MySqlDriver.Instance.OnErrorAsync(context, error));
			Assert.Equal(DataConstraintKind.Unique, result.Kind);
			Assert.Equal(indexName, result.Name);
			Assert.NotNull(result.Principal);
			Assert.Equal(mapping.Name, result.Principal.Name);
			Assert.Equal(["Second", "First"], result.Principal.Fields);
		}
		finally
		{
			Mapping.Entities.Remove(mapping.QualifiedName);
			command.CommandText = $"DROP TABLE IF EXISTS `{mapping.Name}`;";
			await command.ExecuteNonQueryAsync();
		}
	}
}
