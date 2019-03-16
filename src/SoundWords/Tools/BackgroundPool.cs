using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using ServiceStack.Logging;

namespace SoundWords.Tools
{
    /// <summary>
    /// Modified version of https://stackoverflow.com/a/44141040/352573
    /// </summary>
    public class BackgroundPool : IBackgroundPool
    {
        private readonly ILog _logger;
        private readonly IApplicationLifetime _lifetime;
        private readonly IComponentContext _componentContext;
        private readonly object _currentTasksLock = new object();
        private readonly List<Task> _currentTasks = new List<Task>();

        public BackgroundPool(ILogFactory logFactory, IApplicationLifetime lifetime, IComponentContext componentContext)
        {
            _logger = logFactory.GetLogger(GetType());
            _lifetime = lifetime;
            _componentContext = componentContext;

            _lifetime.ApplicationStopped.Register(() =>
            {
                lock (_currentTasksLock)
                {
                    Task.WaitAll(_currentTasks.ToArray());
                }

                _logger.Info("Background pool closed.");
            });
        }

        public void Enqueue<TJob, TParameters>(TParameters parameters) where TJob : IJob<TParameters>
        {
            var job = _componentContext.Resolve<TJob>();
            
            Enqueue(() => job.Execute(parameters));
        }

        private void Enqueue(Func<Task> func)
        {
            var task = Task.Run(async () =>
            {
                _logger.Debug("Queuing background work.");

                try
                {
                    await func();

                    _logger.Debug("Background work returns.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Background work failed.", ex);
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
        Task Execute(TParameters subscriptionInfo);
    }
}