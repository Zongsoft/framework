#!/bin/sh

dotnet deploy -cloud:aliyun -site:daemon -edition:Debug -framework:net8.0
