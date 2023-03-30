[string]$project_core            = 'Zongsoft.Core/build.cake'
[string]$project_data            = 'Zongsoft.Data/build.cake'
[string]$project_net             = 'Zongsoft.Net/build.cake'
[string]$project_web             = 'Zongsoft.Web/build.cake'
[string]$project_plugins         = 'Zongsoft.Plugins/build.cake'
[string]$project_plugins_web     = 'Zongsoft.Plugins.Web/build.cake'
[string]$project_scheduling      = 'Zongsoft.Scheduling/build.cake'
[string]$project_security        = 'Zongsoft.Security/build.cake'
[string]$project_commands        = 'Zongsoft.Commands/build.cake'
[string]$project_reporting       = 'Zongsoft.Reporting/build.cake'
[string]$project_messaging_mqtt  = 'Zongsoft.Messaging.Mqtt/build.cake'
[string]$project_messaging_kafka = 'Zongsoft.Messaging.Kafka/build.cake'
[string]$project_aliyun          = 'externals/aliyun/build.cake'
[string]$project_redis           = 'externals/redis/build.cake'
[string]$project_wechat          = 'externals/wechat/build.cake'
[string]$project_grapecity       = 'externals/grapecity/build.cake'
[string]$project_hangfire        = 'externals/hangfire/build.cake'

[string]$project_administratives = '../Administratives/build.cake'

[string]$CAKE_ARGS = '--verbosity=normal'

Write-Host "dotnet cake $project_core $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_core $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_data $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_data $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_net $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_net $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_web $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_web $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_plugins $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_plugins $CAKE_ARGS $ARGS
dotnet cake $project_plugins_web $CAKE_ARGS $ARGS

# Write-Host "dotnet cake $project_scheduling $CAKE_ARGS $ARGS" -ForegroundColor Magenta
# dotnet cake $project_scheduling $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_security $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_security $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_commands $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_commands $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_reporting $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_reporting $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_messaging_mqtt $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_messaging_mqtt $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_messaging_kafka $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_messaging_kafka $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_aliyun $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_aliyun $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_redis $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_redis $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_wechat $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_wechat $CAKE_ARGS $ARGS

# Write-Host "dotnet cake $project_grapecity $CAKE_ARGS $ARGS" -ForegroundColor Magenta
# dotnet cake $project_grapecity $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_hangfire $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_hangfire $CAKE_ARGS $ARGS

if(Test-Path $project_administratives)
{
	Write-Host "dotnet cake $project_administratives $CAKE_ARGS $ARGS" -ForegroundColor Magenta
	dotnet cake $project_administratives $CAKE_ARGS $ARGS
}
