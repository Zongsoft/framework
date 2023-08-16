#!/bin/sh

dotnet deploy -cloud:aliyun -site:daemon -edition:Debug -environment:production -framework:net7.0
