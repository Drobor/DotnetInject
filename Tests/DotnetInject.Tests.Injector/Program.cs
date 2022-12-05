using System.Diagnostics;
using System.Text.Json;
using DotnetInject.Core;

var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

var psi = new ProcessStartInfo
{
    EnvironmentVariables = { ["COREHOST_TRACE"] = "1" },
    FileName = config.FileName,
    RedirectStandardError = true,
    RedirectStandardOutput = true
};

var process = new Process
{
    EnableRaisingEvents = true,
    StartInfo = psi,
};

Action<object, DataReceivedEventArgs> actionWrite = (sender, e) => { Console.WriteLine(e.Data); };

process.ErrorDataReceived += (sender, e) => actionWrite(sender, e);
process.OutputDataReceived += (sender, e) => actionWrite(sender, e);

process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();

var injector = new ClrInjector();

injector.Inject(process, config.InjectingAssemblyPath, config.HostFxrPath, config.RuntimeConfigPath);

Console.ReadLine();