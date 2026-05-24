using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SoundWords.Tools;

public class BackgroundPool : IBackgroundPool
{
    private readonly ILogger<BackgroundPool> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IComponentContext _componentContext;
    private readonly object _currentTasksLock = new();
    private readonly List<Task> _currentTasks = new();

    public BackgroundPool(ILogger<BackgroundPool> logger,
                          IHostApplicationLifetime lifetime,
                          IComponentContext componentContext)
    {
        _logger = logger;
        _lifetime = lifetime;
        _componentContext = componentContext;

        _lifetime.ApplicationStopped.Register(() =>
                                              {
                                                  lock (_currentTasksLock)
                                                  {
                                                      Task.WaitAll(_currentTasks.ToArray());
                                                  }

                                                  _logger.LogInformation("Background pool closed.");
                                              });
    }

    public void Enqueue<TJob, TParameters>(TParameters parameters) where TJob : IJob<TParameters>
    {
        TJob job = _componentContext.Resolve<TJob>();
        Enqueue(() => job.Execute(parameters));
    }

    private void Enqueue(Func<Task> func)
    {
        Task task = Task.Run(async () =>
                             {
                                 _logger.LogDebug("Queuing background work.");

                                 try
                                 {
                                     await func();
                                     _logger.LogDebug("Background work returns.");
                                 }
                                 catch (Exception ex)
                                 {
                                     _logger.LogError(ex, "Background work failed.");
                                 }
                             }, _lifetime.ApplicationStopped);

        lock (_currentTasksLock)
        {
            _currentTasks.Add(task);
        }

        task.ContinueWith(CleanupOnComplete, _lifetime.ApplicationStopping);
    }

    private void CleanupOnComplete(Task oldTask)
    {
        lock (_currentTasksLock)
        {
            _currentTasks.Remove(oldTask);
        }
    }
}

public interface IJob<in TParameters>
{
    Task Execute(TParameters parameters);
}
