#!/bin/sh

set -ex

CAKE_ARGS="--verbosity=normal"

PROJECT_CORE="Zongsoft.Core/build.cake"
PROJECT_DATA="Zongsoft.Data/build.cake"
PROJECT_NET="Zongsoft.Net/build.cake"
PROJECT_WEB="Zongsoft.Web/build.cake"
PROJECT_DIAGNOSTICS="Zongsoft.Diagnostics/build.cake"
PROJECT_PLUGINS="Zongsoft.Plugins/build.cake"
PROJECT_PLUGINS_WEB="Zongsoft.Plugins.Web/build.cake"
PROJECT_SECURITY="Zongsoft.Security/build.cake"
PROJECT_COMMANDS="Zongsoft.Commands/build.cake"
PROJECT_REPORTING="Zongsoft.Reporting/build.cake"

PROJECT_MESSAGING_MQTT="messaging/mqtt/build.cake"
PROJECT_MESSAGING_KAFKA="messaging/kafka/build.cake"
PROJECT_MESSAGING_RABBIT="messaging/rabbit/build.cake"
PROJECT_MESSAGING_ZEROMQ="messaging/zero/build.cake"

PROJECT_ALIYUN="externals/aliyun/build.cake"
PROJECT_REDIS="externals/redis/build.cake"
PROJECT_WECHAT="externals/wechat/build.cake"
PROJECT_CLOSEDXML="externals/closedxml/build.cake"
PROJECT_HANGFIRE="externals/hangfire/build.cake"
PROJECT_SCRIBAN="externals/scriban/build.cake"
PROJECT_PYTHON="externals/python/build.cake"
PROJECT_LUA="externals/lua/build.cake"

PROJECT_ADMINISTRATIVES="../Administratives/build.cake"

dotnet cake $PROJECT_CORE $CAKE_ARGS "$@"
dotnet cake $PROJECT_DATA $CAKE_ARGS "$@"
dotnet cake $PROJECT_NET $CAKE_ARGS "$@"
dotnet cake $PROJECT_WEB $CAKE_ARGS "$@"
dotnet cake $PROJECT_DIAGNOSTICS $CAKE_ARGS "$@"
dotnet cake $PROJECT_PLUGINS $CAKE_ARGS "$@"
dotnet cake $PROJECT_PLUGINS_WEB $CAKE_ARGS "$@"
dotnet cake $PROJECT_SECURITY $CAKE_ARGS "$@"
dotnet cake $PROJECT_COMMANDS $CAKE_ARGS "$@"
dotnet cake $PROJECT_REPORTING $CAKE_ARGS "$@"
dotnet cake $PROJECT_MESSAGING_MQTT $CAKE_ARGS "$@"
dotnet cake $PROJECT_MESSAGING_KAFKA $CAKE_ARGS "$@"
dotnet cake $PROJECT_MESSAGING_RABBIT $CAKE_ARGS "$@"
dotnet cake $PROJECT_MESSAGING_ZEROMQ $CAKE_ARGS "$@"
dotnet cake $PROJECT_ALIYUN $CAKE_ARGS "$@"
dotnet cake $PROJECT_REDIS $CAKE_ARGS "$@"
dotnet cake $PROJECT_WECHAT $CAKE_ARGS "$@"
dotnet cake $PROJECT_CLOSEDXML $CAKE_ARGS "$@"
dotnet cake $PROJECT_HANGFIRE $CAKE_ARGS "$@"
dotnet cake $PROJECT_SCRIBAN $CAKE_ARGS "$@"
dotnet cake $PROJECT_PYTHON $CAKE_ARGS "$@"
dotnet cake $PROJECT_LUA $CAKE_ARGS "$@"

if [ -f "$PROJECT_ADMINISTRATIVES" ]; then
	dotnet cake $PROJECT_ADMINISTRATIVES $CAKE_ARGS "$@"
fi
