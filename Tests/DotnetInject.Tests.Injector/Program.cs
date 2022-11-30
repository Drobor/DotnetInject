using System.Diagnostics;
using DotnetInject.Core;

//cin >> a;
//auto hostFxrPath = LR"(c:\publish\hostfxr.dll)";
//auto hostFxrPath = LR"(c:\Program Files (x86)\dotnet\host\fxr\5.0.12\hostfxr.dll)";
var hostFxrPath = @"c:\priv\Projects\DotnetInject\Tests\DotnetInject.Tests.Payload\bin\Release\net6.0\win7-x86\publish\hostfxr.dll";
var runtimeConfigPath = @"c:\publish\net60.runtimeconfig.json";
//auto runtimeConfigPath = L"c:\\priv\\Projects\\DotnetInject\\Tests\\DotnetInject.Tests.Payload\\bin\\Release\\net5.0\\win7-x86\\publish\\DotnetInject.Tests.Payload.runtimeconfig.json";
//auto assemblyPath = LR"(c:\publish\ConsoleApp81.dll)";
var assemblyPath = @"c:\priv\Projects\DotnetInject\Tests\DotnetInject.Tests.Payload\bin\Release\net6.0\win7-x86\publish\DotnetInject.Tests.Payload.dll";
var initArgs = "";


var injector = new ClrInjector();

var psi = new ProcessStartInfo
{
    FileName = @"e:\Downloads\memtest.exe",
    EnvironmentVariables = { ["COREHOST_TRACE"] = "1" },
    RedirectStandardError = true,
    RedirectStandardOutput = true
};

var process = new Process
{
    EnableRaisingEvents = true,
    StartInfo = psi,
};


/*
var process = Process.Start(new ProcessStartInfo
{
    FileName = @"C:\Users\imalyavkin\AppData\Roaming\DowOnline\Patch12\Soulstorm.exe",
    WorkingDirectory = @"c:\Program Files (x86)\Steam\steamapps\common\Dawn of War Soulstorm\",
});*/


Action<object, DataReceivedEventArgs> actionWrite = (sender, e) => { Console.WriteLine(e.Data); };

process.ErrorDataReceived += (sender, e) => actionWrite(sender, e);
process.OutputDataReceived += (sender, e) => actionWrite(sender, e);


process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();


injector.Inject(process, assemblyPath, hostFxrPath, runtimeConfigPath);

Console.ReadLine();