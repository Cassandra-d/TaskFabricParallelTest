using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TestTaskFactory
{
    class Program
    {
        static volatile int currentExecutionCount = 0;

        static void Main(string[] args)
        {
            List<Task<long>> taskList = new List<Task<long>>();
            var timer = new Timer(Print, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            for (int i = 0; i < 1000; i++)
            {
                taskList.Add(DoMagic());
            }

            Task.WaitAll(taskList.ToArray());

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer = null;

            //to check that we have all the threads executed
            Console.WriteLine("Done " + taskList.Sum(t => t.Result));
            Console.ReadLine();
        }

        static void Print(object state)
        {
            Console.WriteLine(currentExecutionCount);
        }

        static async Task<long> DoMagic()
        {
            return await Task.Factory.StartNew(() =>
            {
                Interlocked.Increment(ref currentExecutionCount);

                //place your code here

                var policy = 
                    Policy
                        .Handle<WebException>()
                        .WaitAndRetry(new[] 
                        {
                            TimeSpan.FromMilliseconds(100),
                            TimeSpan.FromMilliseconds(200),
                        });

                try
                { 
                    policy.ExecuteAndCapture(() =>
                    {
                        return (new WebClient()).DownloadString("http://www.google.com");
                    });
                }
                catch { }

                Interlocked.Decrement(ref currentExecutionCount);
                return 4;
            }

            //this thing shold give a hint to scheduller to use new threads and not scheduled
            , TaskCreationOptions.LongRunning
            );
        }
    }
}
