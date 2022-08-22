using Renci.SshNet;
using System.Collections.Concurrent;

namespace multiftp
{
    internal class SlaveConnections : IDisposable
    {
        public int Count { get; }

        public readonly List<AsyncFtpClient> slaveConnections = new();

        private readonly ConcurrentQueue<string> pendingDownloads = new();
        private readonly Action<Stream> fileDownloadedAction;
        private bool noMoreFiles = false;

        public SlaveConnections(int count, Action<Stream> fileDownloadedAction)
        {
            Count = count;

            this.fileDownloadedAction = fileDownloadedAction;
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

        public async Task Start(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach(var connection in slaveConnections)
            {
                var task = Work(connection, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (var connection in slaveConnections)
            {
                connection.Disconnect();
            }
        }

        private Task Work(AsyncFtpClient connection, CancellationToken cancellationToken)
        {
            return
                Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested && !(pendingDownloads.IsEmpty && noMoreFiles))
                    {
                        if (pendingDownloads.TryDequeue(out var filePath))
                        {
                            var fileStream = await connection.DownloadFile(filePath);

                            fileDownloadedAction(fileStream);
                        }

                        if (pendingDownloads.IsEmpty) await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                }, cancellationToken);
        }

        public void AddFile(string path)
        {
            pendingDownloads.Enqueue(path);
        }

        public void MarkNoMoreFiles()
        {
            noMoreFiles = true;
        }

        public void Dispose()
        {
            foreach(var connection in slaveConnections)
            {
                connection.Dispose();
            }
        }
    }
}
