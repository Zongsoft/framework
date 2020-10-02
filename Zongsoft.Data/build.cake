var target = Argument("target", "default");

var srcDirectory  = "src/";
var testDirectory = "test/";
var solutionFile  = "Zongsoft.Data.sln";

var mssqlDirectory = "drivers/mssql/";
var mssqlSolution  = mssqlDirectory + "Zongsoft.Data.MsSql.sln";
var mysqlDirectory = "drivers/mysql/";
var mysqlSolution  = mysqlDirectory + "Zongsoft.Data.MySql.sln";

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	DeleteFiles(srcDirectory + "*.nupkg");
	CleanDirectories(srcDirectory + "**/bin");
	CleanDirectories(srcDirectory + "**/obj");
	CleanDirectories(testDirectory + "**/bin");
	CleanDirectories(testDirectory + "**/obj");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetCoreRestore(solutionFile);
});

Task("build")
	.Description("编译项目")
	.IsDependentOn("clean")
	.IsDependentOn("restore")
	.Does(() =>
{
	var settings = new DotNetCoreBuildSettings
	{
		NoRestore = true
	};

	DotNetCoreBuild(solutionFile, settings);
});

Task("test")
	.Description("单元测试")
	.IsDependentOn("build")
	.WithCriteria(DirectoryExists(testDirectory))
	.Does(() =>
{
	var settings = new DotNetCoreTestSettings
	{
		NoRestore = true,
		NoBuild = true
	};

	var projects = GetFiles(testDirectory + "**/*.csproj");

	foreach(var project in projects)
	{
		DotNetCoreTest(project.FullPath, settings);
	}
});

Task("clean.drivers")
	.Description("清理驱动程序")
	.Does(() =>
{
	DeleteFiles(mssqlDirectory + "*.nupkg");
	CleanDirectories(mssqlDirectory + "**/bin");
	CleanDirectories(mssqlDirectory + "**/obj");

	DeleteFiles(mysqlDirectory + "*.nupkg");
	CleanDirectories(mysqlDirectory + "**/bin");
	CleanDirectories(mysqlDirectory + "**/obj");
});

Task("restore.drivers")
	.Description("还原驱动程序依赖")
	.Does(() =>
{
	DotNetCoreRestore(mssqlSolution);
	DotNetCoreRestore(mysqlSolution);
});

Task("build.drivers")
	.Description("编译驱动程序")
	.IsDependentOn("clean.drivers")
	.IsDependentOn("restore.drivers")
	.Does(() =>
{
	var settings = new DotNetCoreBuildSettings
	{
		NoRestore = true
	};

	DotNetCoreBuild(mssqlSolution, settings);
	DotNetCoreBuild(mysqlSolution, settings);
});

Task("default")
	.Description("默认")
	.IsDependentOn("test")
	.IsDependentOn("build.drivers");

RunTarget(target);
