var target = Argument("target", "default");
var edition = Argument("edition", "Debug");
var solutionFile  = "Zongsoft.Hosting.Web.sln";

Task("clean")
	.Description("清理解决方案")
	.Does(() =>
{
	DeleteFiles("*.nupkg");
	CleanDirectories("**/bin");
	CleanDirectories("**/obj");
});

Task("restore")
	.Description("还原项目依赖")
	.Does(() =>
{
	DotNetRestore(solutionFile);
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
});

Task("deploy")
	.Description("部署插件")
	.Does(() =>
{
	DotNetTool(solutionFile, "deploy", $" -edition:{edition} -target:net5.0 .deploy");
});

Task("default")
	.Description("默认")
	.IsDependentOn("build")
	.IsDependentOn("deploy");

RunTarget(target);
