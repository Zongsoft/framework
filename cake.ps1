[string]$project_core        = 'Zongsoft.Core/build.cake'
[string]$project_data        = 'Zongsoft.Data/build.cake'
[string]$project_web         = 'Zongsoft.Web/build.cake'
[string]$project_plugins     = 'Zongsoft.Plugins/build.cake'
[string]$project_plugins_web = 'Zongsoft.Plugins.Web/build.cake'
[string]$project_security    = 'Zongsoft.Security/build.cake'
[string]$project_commands    = 'Zongsoft.Commands/build.cake'
[string]$project_aliyun      = 'externals/aliyun/build.cake'
[string]$project_redis       = 'externals/redis/build.cake'

[string]$CAKE_ARGS = '--verbosity=verbose'

dotnet tool restore

Write-Host "dotnet cake $project_core $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_core $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_data $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_data $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_web $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_web $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_plugins $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_plugins $CAKE_ARGS $ARGS
dotnet cake $project_plugins_web $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_security $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_security $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_commands $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_commands $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_aliyun $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_aliyun $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_redis $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_redis $CAKE_ARGS $ARGS
