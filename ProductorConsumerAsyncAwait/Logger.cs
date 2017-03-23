using System;
using System.Diagnostics;
using System.Threading;

namespace AsynchronousCache
{
    public class Logger : ILogger
    {
        public void DebugLog(string message)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine($"[{threadId}] " + message);
        }

        public void ErrorLog(Exception ex)
        {
            Debug.WriteLine(ex);
        }

        public void ErrorLog(string message)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine($"[{threadId}] " + message);
        }
    }
}