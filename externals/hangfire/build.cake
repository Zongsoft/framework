var target = Argument("target", "default");

var solutionFile = "Zongsoft.Externals.Hangfire.sln";
var dependents = new []
{
	"web/Zongsoft.Externals.Hangfire.Web.sln",
	"storages/Zongsoft.Externals.Hangfire.Storages.sln",
	"samples/Zongsoft.Externals.Hangfire.Samples.sln"
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

	foreach(var dependent in dependents)
	{
		DotNetRestore(dependent);
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

	foreach(var dependent in dependents)
	{
		DotNetBuild(dependent, settings);
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
