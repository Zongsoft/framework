var target = Argument("target", "default");
var edition = Argument("edition", "Debug");

var solutionFile = "Zongsoft.Data.sln";
var providerFiles = new string[]
{
	@"drivers/mssql/Zongsoft.Data.MsSql.sln",
	@"drivers/mysql/Zongsoft.Data.MySql.sln",
	@"drivers/sqlite/Zongsoft.Data.SQLite.sln",
	@"drivers/tdengine/Zongsoft.Data.TDengine.sln",
	@"drivers/clickhouse/Zongsoft.Data.ClickHouse.sln",
};

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	CleanDirectories("**/bin/{edition}");
	CleanDirectories("**/obj/{edition}");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetRestore(solutionFile);

	foreach(var providerFile in providerFiles)
	{
		DotNetRestore(providerFile);
	}
});

Task("build")
	.Description("编译项目")
	.IsDependentOn("clean")
	.IsDependentOn("restore")
	.Does(() =>
{
	var settings = new DotNetBuildSettings
	{
		NoRestore = true,
		Configuration = edition,
	};

	DotNetBuild(solutionFile, settings);

	foreach(var providerFile in providerFiles)
	{
		DotNetBuild(providerFile, settings);
	}
});

Task("test")
	.Description("单元测试")
	.IsDependentOn("build")
	.Does(() =>
{
	var settings = new DotNetTestSettings
	{
		NoBuild = true,
		NoRestore = true,
		Configuration = edition,
	};

	var projects = GetFiles("**/test/*.csproj");

	foreach(var project in projects)
	{
		DotNetTest(project.FullPath, settings);
	}
});

Task("default")
	.Description("默认")
	.IsDependentOn("test");

RunTarget(target);
