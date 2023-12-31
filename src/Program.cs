﻿using dnlib.DotNet;
using dnlib.DotNet.Writer;
using x86Predicate;

if (args.Length == 0)
{
    Exit("Usage: x86Predicate <PATH>");
    return;
}

var src = args[0];

if (!File.Exists(src))
{
    Exit("File not found!");
    return;
}

var module = ModuleDefMD.Load(src);
MethodDefExt.OriginalMD = module;

foreach (var type in module.GetTypes())
{
    if (!type.HasMethods)
        continue;

    foreach (var method in type.Methods)
    {
        if (!method.IsNative)
            continue;

        Console.WriteLine($"0x{method.MDToken.Raw:X2}");

        var decrypted = X86ToILConverter.ILFromX86Method(
            new(method)
            );

        method.ImplAttributes = MethodImplAttributes.IL;

        method.Attributes =
            MethodAttributes.FamANDAssem |
            MethodAttributes.Family |
            MethodAttributes.Static |
            MethodAttributes.HideBySig;

        method.Body = decrypted.Body;
    }
}

Save(src, module);
Exit();


static void Save(
    string file,
    ModuleDefMD module
    )
{
    Console.WriteLine("Saving...");

    var writer = new NativeModuleWriterOptions(module, true)
    {
        KeepExtraPEData = true,
        KeepWin32Resources = true,
        Logger = DummyLogger.NoThrowInstance
    };

    writer.MetadataOptions.Flags =
        MetadataFlags.PreserveAll |
        MetadataFlags.KeepOldMaxStack;

    module.NativeWrite(
        file + $"_No-x86.exe",
        writer
        );

    Console.WriteLine("Saved!");
}

static void Exit(string? msg = null)
{
    Console.WriteLine();

    if (!string.IsNullOrEmpty(msg))
        Console.WriteLine(msg);

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey(true);
    Environment.Exit(0);
}