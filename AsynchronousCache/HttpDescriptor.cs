namespace AsynchronousCache
{
    public class HttpDescriptor : HttpDescriptorBase
    {
        public string Url;
        public int CacheDurationInSeconds;

        public override string ToString()
        {
            return Url;
        }
    }
}