@echo off
podman kube play --replace .\Zongsoft.Messaging.Storages.Data.MySql-pod.yaml
podman kube play --replace .\Zongsoft.Messaging.Storages.Data.PostgreSql-pod.yaml
