namespace SoundWords.Tools
{
    public interface IBackgroundPool
    {
        void Enqueue<TJob, TParameters>(TParameters parameters) where TJob : IJob<TParameters>;
    }
}