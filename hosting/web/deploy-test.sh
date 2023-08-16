#!/bin/sh

dotnet deploy -cloud:aliyun -edition:Debug -environment:test -framework:net7.0
