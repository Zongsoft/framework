#!/bin/sh

dotnet deploy -cloud:aliyun -edition:Debug -debug:off -environment:test -framework:net7.0
