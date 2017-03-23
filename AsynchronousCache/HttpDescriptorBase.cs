using System.Threading;

namespace AsynchronousCache
{
    public class HttpDescriptorBase
    {
        public SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
    }
}