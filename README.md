# Zongsoft Framework
这是 Zongsoft 开发框架的 .NET CORE 版本集。


## 目录结构

```
[/Zongsoft/Framework]
├── .gitignore
├── README.md
│
├── [build]
│    └── ...
│
├── [externals]
│    ├── [alimap]
│    ├── [alipay]
│    ├── [aliyun]
│    ├── [wechat]
│    └── [redis]
│
├── [hosting]
│    ├── [web]
│    ├── [client]
│    ├── [daemon]
│    └── [terminal]
│
├── [Zongsoft.Core]
├── [Zongsoft.Data]
├── [Zongsoft.Common]
├── [Zongsoft.Plugins]
├── [Zongsoft.Plugins.Web]
├── [Zongsoft.Scheduling]
├── [Zongsoft.Security]
└── [Zongsoft.Web]
```

## 开始
### 安装Cake
```
dotnet tool install Cake.Tool -g
```
### 运行Cake
Windows执行
```
.\cake.ps1
```

Linux执行
```
.\cake.sh
```