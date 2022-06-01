var target = Argument("target", "default");

var solutionFile  = "Zongsoft.Data.sln";
var mssqlDirectory = "drivers/mssql/";
var mssqlSolution  = mssqlDirectory + "Zongsoft.Data.MsSql.sln";
var mysqlDirectory = "drivers/mysql/";
var mysqlSolution  = mysqlDirectory + "Zongsoft.Data.MySql.sln";

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
	DotNetRestore(mssqlSolution);
	DotNetRestore(mysqlSolution);
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
	DotNetBuild(mssqlSolution, settings);
	DotNetBuild(mysqlSolution, settings);
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
