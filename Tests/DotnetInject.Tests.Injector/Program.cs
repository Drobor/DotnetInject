using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DotnetInject.Injector;

var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

var psi = new ProcessStartInfo
{
    EnvironmentVariables =
    {
        ["PAL_DBG_CHANNELS"] = "+all.all",
        ["COREHOST_TRACE"] = "1",
        ["COREHOST_TRACE_VERBOSITY"] = "4",
    },
    FileName = config.FileName,
    WorkingDirectory = Path.GetDirectoryName(config.FileName),
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