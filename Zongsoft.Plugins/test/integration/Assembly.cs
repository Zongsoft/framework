using Xunit;

//ApplicationContext.Current 和 BuilderManager.Current 是进程级状态。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
