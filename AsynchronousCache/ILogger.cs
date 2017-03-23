using System;

namespace AsynchronousCache
{
    public interface ILogger
    {
        void DebugLog(string message);
        void ErrorLog(Exception ex);
        void ErrorLog(string message);
    }
}