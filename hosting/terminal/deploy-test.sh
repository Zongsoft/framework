#!/bin/sh

dotnet deploy -cloud:aliyun -site:daemon -edition:Debug -environment:test -framework:net7.0
