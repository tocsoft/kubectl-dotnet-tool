using System.Diagnostics;
using System.Runtime.InteropServices;

var currentAssembly = typeof(Program).Assembly;
var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(currentAssembly.Location);
string version = fvi.FileVersion;

var rootExeLocation = Path.Combine(Path.GetDirectoryName(currentAssembly.Location)!, "_cache");
Directory.CreateDirectory(rootExeLocation);
var exeName = $"kubectl-{version}";
var exeExtension = ".exe";
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    exeExtension = "";
}

Dictionary<OSPlatform, string> downloadPaths = new Dictionary<OSPlatform, string>()
{
    [OSPlatform.Linux] = $"https://dl.k8s.io/release/v{version}/bin/linux/amd64/kubectl",
    [OSPlatform.OSX] = $"https://dl.k8s.io/release/v{version}/bin/darwin/amd64/kubectl",
    [OSPlatform.Windows] = $"https://dl.k8s.io/release/v{version}/bin/windows/amd64/kubectl.exe",
};

var exeLocation = Path.Combine(rootExeLocation, $"{exeName}{exeExtension}");
var downloadUrl = downloadPaths.Where(x => RuntimeInformation.IsOSPlatform(x.Key)).FirstOrDefault().Value ?? downloadPaths[OSPlatform.Linux];

// on first run download the exe into a relative folder to this exe
if (!File.Exists(exeLocation))
{
    HttpClient client = new HttpClient();
    using var stream = await client.GetStreamAsync(downloadUrl);
    using var file = File.Create(exeLocation);
    await stream.CopyToAsync(file);
}

// get the bundled exe 
var psi = new ProcessStartInfo
{
    FileName = exeLocation,
    UseShellExecute = false
};

foreach (var a in args)
{
    psi.ArgumentList.Add(a);
}

var _process = new Process
{
    StartInfo = psi
};

// Ignore Ctrl-C for the remainder of the command's execution
// Forwarding commands will just spawn the child process and exit
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; };

_process.Start();
_process.WaitForExit();
return _process.ExitCode;