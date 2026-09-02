using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Tests;

public class DataSourceSelectorTest
{
	[Fact]
	public void GetSource_MultiScriptCommand_PrefersDefaultDriver()
	{
		var command = CreateCommand();
		command.Script("Secondary", "SELECT 'secondary';");
		command.Script("Default", "SELECT 'default';");

		var secondary = new DataSourceMocker("Selector#secondary", "Secondary");
		var primary = new DataSourceMocker("Selector", "Default");
		var selector = new DataSourceSelector([secondary, primary]);

		try
		{
			using var accessor = new DataAccess("Selector");
			using var context = new DataExecuteContextMocker(accessor, command.QualifiedName);

			Assert.Same(primary, selector.GetSource(context));
		}
		finally
		{
			Mapping.Commands.Remove(command.QualifiedName);
		}
	}

	[Fact]
	public void GetSource_DefaultDriverWithoutScript_FallsBackToCompatibleScriptDriver()
	{
		var command = CreateCommand();
		command.Script("Unavailable", "SELECT 'unavailable';");
		command.Script("Fallback", "SELECT 'fallback';");

		var fallback = new DataSourceMocker("Selector#fallback", "Fallback");
		var primary = new DataSourceMocker("Selector", "Default");
		var selector = new DataSourceSelector([fallback, primary]);

		try
		{
			using var accessor = new DataAccess("Selector");
			using var context = new DataExecuteContextMocker(accessor, command.QualifiedName);

			Assert.Same(fallback, selector.GetSource(context));
		}
		finally
		{
			Mapping.Commands.Remove(command.QualifiedName);
		}
	}

	private static DataCommand CreateCommand()
	{
		var command = new DataCommand("SelectorTests", $"Command{Guid.NewGuid():N}");
		Mapping.Commands.Add(command);
		return command;
	}

	private sealed class DataExecuteContextMocker(IDataAccess accessor, string name) : DataExecuteContextBase(accessor, name, false, null, null)
	{
		public override TFeature GetFeature<TFeature>() => default;
	}

	private sealed class DataSourceMocker(string name, string driver) : IDataSource
	{
		public string Name { get; } = name;
		public string ConnectionString => string.Empty;
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver { get; } = new DataDriverMocker(driver);
		public FeatureCollection Features => null;
		public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		public DataTable GetSchema(string name, bool refresh = false) => null;
		public bool Equals(IDataSource other) => ReferenceEquals(this, other);
	}

	private sealed class DataDriverMocker(string name) : IDataDriver
	{
		public string Name { get; } = name;
		public FeatureCollection Features => null;
		public IDataRecordGetter Getter => null;
		public IDataParameterSetter Setter => null;
		public IDataImporter Importer => null;
		public IStatementBinder Binder => StatementBinder.Default;
		public IStatementSlotEvaluator Slotter => null;
		public IStatementBuilder Builder => null;

		public Exception OnError(IDataAccessContext context, Exception exception) => exception;
		public DbCommand CreateCommand() => throw new NotSupportedException();
		public DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		public DbCommand CreateCommand(IDataAccessContextBase context, IStatementBase statement) => throw new NotSupportedException();
		public DbConnection CreateConnection(string connectionString = null) => throw new NotSupportedException();
		public DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => throw new NotSupportedException();
	}
}
