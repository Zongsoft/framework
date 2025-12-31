[string]$project_core             = 'Zongsoft.Core/build.cake'
[string]$project_data             = 'Zongsoft.Data/build.cake'
[string]$project_net              = 'Zongsoft.Net/build.cake'
[string]$project_web              = 'Zongsoft.Web/build.cake'
[string]$project_diagnostics      = 'Zongsoft.Diagnostics/build.cake'
[string]$project_intelligences    = 'Zongsoft.Intelligences/build.cake'
[string]$project_plugins          = 'Zongsoft.Plugins/build.cake'
[string]$project_plugins_web      = 'Zongsoft.Plugins.Web/build.cake'
[string]$project_security         = 'Zongsoft.Security/build.cake'
[string]$project_commands         = 'Zongsoft.Commands/build.cake'
[string]$project_reporting        = 'Zongsoft.Reporting/build.cake'

[string]$project_messaging_mqtt   = 'messaging/mqtt/build.cake'
[string]$project_messaging_kafka  = 'messaging/kafka/build.cake'
[string]$project_messaging_rabbit = 'messaging/rabbit/build.cake'
[string]$project_messaging_zeromq = 'messaging/zero/build.cake'

[string]$project_aliyun           = 'externals/aliyun/build.cake'
[string]$project_amazon           = 'externals/amazon/build.cake'
[string]$project_redis            = 'externals/redis/build.cake'
[string]$project_polly            = 'externals/polly/build.cake'
[string]$project_wechat           = 'externals/wechat/build.cake'
[string]$project_closedxml        = 'externals/closedxml/build.cake'
[string]$project_hangfire         = 'externals/hangfire/build.cake'
[string]$project_scriban          = 'externals/scriban/build.cake'
[string]$project_python           = 'externals/python/build.cake'
[string]$project_lua              = 'externals/lua/build.cake'
[string]$project_opc              = 'externals/opc/build.cake'

[string]$project_administratives  = '../Administratives/build.cake'

[string]$CAKE_ARGS = '--verbosity=normal'

Write-Host "dotnet cake $project_core $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_core $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_data $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_data $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_net $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_net $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_web $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_web $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_diagnostics $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_diagnostics $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_intelligences $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_intelligences $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_plugins $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_plugins $CAKE_ARGS $ARGS
dotnet cake $project_plugins_web $CAKE_ARGS $ARGS

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

Write-Host "dotnet cake $project_messaging_rabbit $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_messaging_rabbit $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_messaging_zeromq $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_messaging_zeromq $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_aliyun $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_aliyun $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_amazon $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_amazon $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_redis $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_redis $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_polly $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_polly $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_wechat $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_wechat $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_closedxml $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_closedxml $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_hangfire $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_hangfire $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_scriban $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_scriban $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_python $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_python $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_lua $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_lua $CAKE_ARGS $ARGS

Write-Host "dotnet cake $project_opc $CAKE_ARGS $ARGS" -ForegroundColor Magenta
dotnet cake $project_opc $CAKE_ARGS $ARGS

if(Test-Path $project_administratives)
{
	Write-Host "dotnet cake $project_administratives $CAKE_ARGS $ARGS" -ForegroundColor Magenta
	dotnet cake $project_administratives $CAKE_ARGS $ARGS
}
