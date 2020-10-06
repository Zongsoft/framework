#!/usr/bin/env bash

CAKE_ARGS="--verbosity=verbose";
PROJECT_CORE="Zongsoft.Core/build.cake";

dotnet tool restore

dotnet cake $PROJECT_CORE $CAKE_ARGS "$@"
