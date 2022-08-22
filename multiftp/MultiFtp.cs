
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace multiftp
{
    public class MultiFtp : IDisposable
    {
        private MasterConnection? masterConnection;
        private SlaveConnections? slaveConnections;

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly ConnectionInfo connectionInfo;

        public int MaxAllowedConnectionCount { get; }

        public MultiFtp(ConnectionInfo connectionInfo, int maxAllowedConnectionCount)
        {
            if (maxAllowedConnectionCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAllowedConnectionCount), "we need at least 2 connections to work in harmony :(");
            }

            this.connectionInfo = connectionInfo;
            MaxAllowedConnectionCount = maxAllowedConnectionCount;
        }

        private void Connect(Action<Stream> downloadedAction)
        {
            slaveConnections = new SlaveConnections(MaxAllowedConnectionCount - 1, downloadedAction);

            slaveConnections.Connect(connectionInfo);

            masterConnection = new MasterConnection(connectionInfo, slaveConnections.AddFile);

            masterConnection.Connect();
        }

        /// <summary>
        /// This method does it all.
        /// </summary>
        /// <param name="selector">Give a lambda that will tell the FtpClient to navigate in it (if it is a folder) or to download it (if it is a file)</param>
        /// <returns></returns>
        public async Task Work(Func<SftpFile, bool> selector, Action<Stream> downloadedAction)
        {
            Connect(downloadedAction);

            var scanningTask = masterConnection.StartScanning(selector, cancellationTokenSource.Token);

            var downloadTasks = slaveConnections.Start(cancellationTokenSource.Token);

            await scanningTask;

            slaveConnections.MarkNoMoreFiles();

            await downloadTasks;
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}