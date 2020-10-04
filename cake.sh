#!/bin/sh

set -ex

project_core='Zongsoft.Core/build.cake'
project_data='Zongsoft.Data/build.cake'
project_web='Zongsoft.Web/build.cake'
project_plugins='Zongsoft.Plugins/build.cake'
project_plugins_web='Zongsoft.Plugins.Web/build.cake'
project_security='Zongsoft.Security/build.cake'
project_commands='Zongsoft.Commands/build.cake'
project_aliyun='externals/aliyun/build.cake'
project_redis='externals/redis/build.cake'

CAKE_ARGS='--verbosity=verbose'

dotnet tool restore

dotnet cake $project_core $CAKE_ARGS "$@"
dotnet cake $project_data $CAKE_ARGS "$@"
dotnet cake $project_web $CAKE_ARGS "$@"
dotnet cake $project_plugins $CAKE_ARGS "$@"
dotnet cake $project_plugins_web $CAKE_ARGS "$@"
dotnet cake $project_security $CAKE_ARGS "$@"
dotnet cake $project_commands $CAKE_ARGS "$@"
dotnet cake $project_aliyun $CAKE_ARGS "$@"
dotnet cake $project_redis $CAKE_ARGS "$@"
