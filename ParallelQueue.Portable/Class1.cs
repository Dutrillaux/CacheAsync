using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ParallelQueue.Portable
{
    public class Class1
    {
        private static BufferBlock<string> m_data;  
…  
private static async Task<HttpResponseMessage> ConsumerAsync()
        {
            while (true)
            {
                int nextItem = await m_data.ReceiveAsync();
                ProcessNextItem(nextItem);
            }
        }  
…  
private static void Produce(string url)
        {
            m_data.Post(url);
        }


    }
}
