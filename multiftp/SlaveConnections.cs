using Renci.SshNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multiftp
{
    internal class SlaveConnections : IDisposable
    {
        public int Count { get; }

        public readonly List<AsyncFtpClient> slaveConnections;

        private readonly ConcurrentQueue<string> pendingDownloads = new();
        private readonly Action<Stream> fileDownloadedAction;
        private CancellationTokenSource cancellationTokenSource = new();
        private ConcurrentBag<Task> tasks = new();
        private bool noMoreFiles = false;

        public SlaveConnections(int count, Action<Stream> fileDownloadedAction)
        {
            Count = count;

            this.fileDownloadedAction = fileDownloadedAction;

            slaveConnections = new List<AsyncFtpClient>();
        }

        public void Connect(ConnectionInfo connectionInfo)
        {
            for (int i = 0; i < Count; i++)
            {
                var connection = new AsyncFtpClient(connectionInfo);
                connection.Connect();
                slaveConnections.Add(connection);
            }
        }

        public void Terminate()
        {
            cancellationTokenSource.Cancel();
        }

        public Task Start(CancellationToken? cancellationToken = null)
        {
            foreach(var connection in slaveConnections)
            {
                var task = Work(connection, cancellationToken == null ? 
                    cancellationTokenSource.Token : 
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value, cancellationTokenSource.Token).Token);

                tasks.Add(task);
            }

            return Task.WhenAll(tasks);
        }

        private Task Work(AsyncFtpClient connection, CancellationToken cancellationToken)
        {
            return
                Task.Run(async () =>
                {
                    Console.WriteLine($"Task {Task.CurrentId} started");
                    while (!cancellationToken.IsCancellationRequested && !(pendingDownloads.IsEmpty && noMoreFiles))
                    {
                        if (pendingDownloads.TryDequeue(out var filePath))
                        {
                            var fileStream = await connection.DownloadFile(filePath);

                            fileDownloadedAction(fileStream);
                        }

                        if (pendingDownloads.IsEmpty) await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    Console.WriteLine($"Task {Task.CurrentId} finished");
                }, cancellationToken);
        }

        public void AddFile(string path)
        {
            Console.WriteLine($"Added file {path} to queue");
            pendingDownloads.Enqueue(path);
        }

        public void MarkNoMoreFiles()
        {
            noMoreFiles = true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
