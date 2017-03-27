using System.Threading;

namespace AsynchronousCache.Descriptors
{
    public class HttpBaseDescriptor
    {
        public const int SemaphoreSlimMaxCount = 1;
        public SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
    }
}