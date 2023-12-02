#!/bin/sh

dotnet deploy -cloud:aliyun -edition:Debug -debug:off -environment:development -framework:net8.0
