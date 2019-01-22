using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AspNetCoreWebApi.Processing
{
    public class SingleThreadWorker<T>
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<T> _queue = new BlockingCollection<T>();

        public SingleThreadWorker(Action<T> work, string message)
        {
            _thread = new Thread(() => {
                Console.WriteLine(message);

                while(true)
                {
                    work(_queue.Take());
                }
            });
            _thread.Start();
        }

        public void Enqueue(T item)
        {
            _queue.Add(item);
        }

        public void Stop()
        {
            _thread.Interrupt();
        }
    }
}