using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multiftp
{
    internal class MasterConnection : IDisposable
    {
        private readonly AsyncFtpClient sftpClient;
        private readonly Action<string> newFileCallback;

        public MasterConnection(ConnectionInfo connectionInfo, Action<string> newFileCallback)
        {
            sftpClient = new AsyncFtpClient(connectionInfo);
            this.newFileCallback = newFileCallback;
        }

        public void Connect()
        {
            sftpClient.Connect();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selector"></param>
        /// <returns>A task, but await it with caution, it will only end when all scanning ends</returns>
        public async Task StartScanning(Func<SftpFile, bool> selector, CancellationToken ct)
        {
            await FindFiles(selector, sftpClient.WorkingDirectory, ct);
        }

        private async Task FindFiles(Func<SftpFile, bool> selector, string workingDirectory, CancellationToken ct)
        {
            var directories = await sftpClient.ListDirectoryAsync(workingDirectory);

            foreach (var directory in directories)
            {
                if (ct.IsCancellationRequested) return;

                if (selector(directory))
                {
                    if (directory.IsRegularFile)
                    {
                        newFileCallback(directory.FullName);
                    }
                    else
                    {
                        await FindFiles(selector, directory.FullName, ct);
                    }
                }
            }
        }

        public void Dispose()
        {
            sftpClient?.Dispose();
        }
    }
}
