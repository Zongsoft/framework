var target = Argument("target", "default");

var srcDirectory  = "src/";
var testDirectory = "test/";
var solutionFile  = "Zongsoft.Externals.Wechat.sln";

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

Task("default")
	.Description("默认")
	.IsDependentOn("test");

RunTarget(target);
