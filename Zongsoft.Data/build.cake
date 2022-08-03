var target = Argument("target", "default");

var solutionFile  = "Zongsoft.Data.sln";
var providerFiles = new string[]
{
	@"drivers/mssql/Zongsoft.Data.MsSql.sln",
	@"drivers/mysql/Zongsoft.Data.MySql.sln",
	@"drivers/clickhouse/Zongsoft.Data.ClickHouse.sln",
};

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	DeleteFiles("**/*.nupkg");
	CleanDirectories("**/bin");
	CleanDirectories("**/obj");
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
		NoRestore = true
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
		NoRestore = true,
		NoBuild = true
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
