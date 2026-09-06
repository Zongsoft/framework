---
name: zongsoft-learning
description: 修改或审查 Zongsoft.Learning 的 ML.NET Dataset、TextFileLoader、EstimatorBuilder、Pipeline Catalog 或元数据 Web API 时使用；不用于聊天智能体或业务模型训练执行。
---

# Zongsoft.Learning 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 源码入口与当前范围

- [Dataset](src/Dataset.cs) 与 `IDataset` 描述名称、Fields 和 Settings；[DatasetLoader](src/DatasetLoader.cs) 按名称从服务容器 Find 加载器。
- [TextFileLoader](src/Data/TextFileLoader.cs) 将设置转为 ML.NET TextLoader.Options，通过 FileSource 打开数据源并返回 IDataView。
- [Pipeline](src/Pipeline.cs) 使用全局 Catalog 递归查找 EstimatorDescriptor，然后调用其 Builder。
- [Transforms](src/Transforms) 与 [Trainers](src/Trainers) 分别提供编码/拼接和 LightGbm 回归构建器；配置驱动注册及分类在 [插件](src/Zongsoft.Learning.plugin)。
- [PipelineController](api/Controllers/PipelineController.cs) 返回 Catalogs/Estimators，仅是元数据读取，不是训练管理 API。

## 不能误当作已实现的能力

- 当前 TextFileLoaderSettings.Populate 使用 PropertyInfo，而 Core ConnectionSettingsBase 的虚方法使用 MemberInfo；当前 Debug/net10.0 定向构建报 CS0115。先确认目标 Core 版本与重写签名，不通过替换旧 DLL 掩盖源码不兼容。
- Web PipelineController 只有 Area 与 HttpGet，没有 Route/ControllerName；默认 MapControllers 不会替它生成约定路由。修正或扩展时分别验证控制器发现、路由生成、授权与真实 HTTP 响应，不用“插件已加载”代替端点可达性。
- 当前 Pipeline.Build 在后续步骤调用 `estimator.Append(estimator)`，没有把新链赋回 result；不应宣称可正确组装任意多步流水线。
- 未找到描述符时没有完整错误转换，空流水线返回 null；修复时需明确异常与空值兼容边界。
- 数据库目录是初步设计资料，不代表存在训练任务、模型仓储或迁移服务。
- TextFileLoader 读取 Settings 而不是自动把 Dataset.Fields 全部生成列配置；字段声明和 TextLoader.Options 需分别核对。
- IDataView 通常延迟枚举，Load 返回不意味着数据全部读取。源文件/流生命周期需要覆盖实际枚举期间。

## 扩展约束

- 新构建器应同步设置类、驱动名称、插件挂载分类、本地化资源；名称用于查找，不是仅用于展示。
- Builder 应构造 Estimator，不隐式执行 Fit 或长耗时训练；训练、评估与模型保存是不同阶段。
- 特征列/标签列名称、输入输出列配对和类型转换属于可保存配置契约；不得无迁移地重命名。
- 全局 Catalog 应在受控装配期修改；并发请求读取时避免临时清空或替换半成品目录。
- 不把 ML.NET 对象线程安全性、取消或流释放语义从框架异步接口名称中推断出来。

## 最小验证

- 目标 `src/Zongsoft.Learning.csproj` 与按需 `api/Zongsoft.Learning.Web.csproj`；当前无独立测试项目。
- 使用小型本地文本与记录型 Builder 验证目录递归、未知名称、空/单步/多步流水线、输入列错误和枚举期间文件失效。
- 文档任务不运行训练；实现测试也应先验证组装而非启动昂贵模型拟合。
