using Renci.SshNet;
using Renci.SshNet.Common;

Console.WriteLine($"Will save to ${Path.GetTempPath()}");

var ftp = new multiftp.MultiFtp(4);

var connectionInfo = new ConnectionInfo("localhost", 2222, "sa", new PasswordAuthenticationMethod("sa", "Bonjour01"));

ftp.Connect(connectionInfo);

await ftp.Work((element) => element.Name.Contains("sftp") || (element.Name.Contains("this") && !element.Name.Contains("not")));
