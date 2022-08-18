using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multiftp
{
    internal class MasterConnection
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
        public async Task StartScanning(Func<SftpFile, bool> selector)
        {
            await FindFiles(selector, sftpClient.WorkingDirectory);
        }

        private async Task FindFiles(Func<SftpFile, bool> selector, string workingDirectory)
        {
            var directories = await sftpClient.ListDirectoryAsync(workingDirectory);

            foreach (var directory in directories)
            {
                if (selector(directory))
                {
                    Console.WriteLine($"directory {directory.FullName} passed");
                    if (directory.IsRegularFile)
                    {
                        newFileCallback(directory.FullName);
                    }
                    else
                    {
                        await FindFiles(selector, directory.FullName);
                    }
                } else
                {
                    Console.WriteLine($"directory {directory.FullName} failed");
                }
            }
        }
    }
}
