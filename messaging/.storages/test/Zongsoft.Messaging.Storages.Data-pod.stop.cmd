@echo off
podman kube down .\Zongsoft.Messaging.Storages.Data.PostgreSql-pod.yaml
podman kube down .\Zongsoft.Messaging.Storages.Data.MySql-pod.yaml
