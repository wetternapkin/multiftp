
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Collections.Concurrent;

namespace multiftp
{
    public class MultiFtp : IDisposable
    {
        private MasterConnection masterConnection;
        private SlaveConnections slaveConnections;


        public int MaxAllowedConnectionCount { get; }

        public MultiFtp(int maxAllowedConnectionCount)
        {
            if (maxAllowedConnectionCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAllowedConnectionCount), "we need at least 2 connections to work in harmony :(");
            }

            MaxAllowedConnectionCount = maxAllowedConnectionCount;
        }

        public void Connect(ConnectionInfo connectionInfo)
        {
            slaveConnections = new SlaveConnections(MaxAllowedConnectionCount - 1, SaveFile);

            slaveConnections.Connect(connectionInfo);

            masterConnection = new MasterConnection(connectionInfo, slaveConnections.AddFile);

            masterConnection.Connect();
        }

        /// <summary>
        /// This method does it all.
        /// </summary>
        /// <param name="selector">Give a lambda that will tell the FtpClient to navigate in it (if it is a folder) or to download it (if it is a file)</param>
        /// <returns></returns>
        public async Task Work(Func<SftpFile, bool> selector)
        {
            // TODO: CancellationToken
            var scanningTask = masterConnection.StartScanning(selector);

            var downloadTasks = slaveConnections.Start();

            await scanningTask;

            slaveConnections.MarkNoMoreFiles();

            await downloadTasks;
        }

        private static void SaveFile(Stream fileStream)
        {
            var path = Path.GetTempPath() + "\\sftp\\" + Guid.NewGuid() + ".txt";
            Console.WriteLine($"New file: {path}");
            Console.WriteLine($"Filestream length: {fileStream.Length}");
            using var file = File.OpenWrite(path);

            fileStream.Position = 0;
            fileStream.CopyTo(file);

            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();

            file.Flush();
            file.Close();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}