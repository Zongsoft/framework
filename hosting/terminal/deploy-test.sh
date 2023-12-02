#!/bin/sh

dotnet deploy -cloud:aliyun -site:daemon -edition:Debug -debug:off -environment:test -framework:net8.0
