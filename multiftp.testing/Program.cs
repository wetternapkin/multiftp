using Renci.SshNet;

Console.WriteLine($"Will save to ${Path.GetTempPath()}");

var connectionInfo = new ConnectionInfo("localhost", 2222, "sa", new PasswordAuthenticationMethod("sa", "Bonjour01"));

var ftp = new multiftp.MultiFtp(connectionInfo, 4);

await ftp.Work((element) => 
    element.Name.Contains("sftp") || (element.Name.Contains("this") && !element.Name.Contains("not")), 
        fileStream => {
        var path = Path.GetTempPath() + "\\sftp\\" + Guid.NewGuid() + ".txt";
        using var file = File.OpenWrite(path);

        fileStream.Position = 0;
        fileStream.CopyTo(file);

        fileStream.Flush();
        fileStream.Close();
        fileStream.Dispose();

        file.Flush();
        file.Close();
    }
);

