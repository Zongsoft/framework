#!/bin/sh

dotnet deploy -cloud:aliyun -edition:Debug -debug:off -environment:production -framework:net8.0
