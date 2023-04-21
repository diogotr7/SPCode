using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.Utils;

public class Config : ICloneable
{
    public bool AutoCopy { get; set; }
    public bool AutoUpload { get; set; }
    public bool AutoRcon { get; set; }
    public string CopyDirectory { get; set; } = string.Empty;
    public bool DeleteAfterCopy { get; set; }
    public string FtpDir { get; set; } = string.Empty;
    [DefaultValue("ftp://localhost/")] public string FtpHost { get; set; } = "ftp://localhost/";
    public string FtpPassword { get; set; } = string.Empty;
    public string FTPUser { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    [DefaultValue(2)] public int OptimizeLevel { get; set; } = 2;
    public string PostCmd { get; set; } = string.Empty;
    public string PreCmd { get; set; } = string.Empty;
    public string RConCommands { get; set; } = string.Empty;
    [DefaultValue("127.0.0.1")] public string RConIP { get; set; } = "127.0.0.1";
    public string RConPassword { get; set; } = string.Empty;
    [DefaultValue(27015)] public ushort RConPort { get; set; } = 27015;
    public string ServerArgs { get; set; } = string.Empty;
    public string ServerFile { get; set; } = string.Empty;

    [JsonIgnore] private SMDefinition? SMDef;

    public List<string> SMDirectories= new();
    public List<string> RejectedPaths = new();

    public bool Standard;

    [DefaultValue(1)] public int VerboseLevel = 1;

    public SMDefinition GetSMDef()
    {
        if (SMDef == null)
        {
            LoadSMDef();
        }

        return SMDef;
    }

    public void InvalidateSMDef()
    {
        SMDef = null;
    }

    public void LoadSMDef()
    {
        if (SMDef != null)
        {
            return;
        }

        try
        {
            var watch = Stopwatch.StartNew();
            var def = new SMDefinition();
            def.AppendFiles(SMDirectories, out var rejectedPaths);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);

            RejectedPaths.Clear();

            if (rejectedPaths.Any())
            {
                rejectedPaths.ForEach(x => RejectedPaths.Add(x));
            }

            SMDef = def;
        }
        catch (Exception)
        {
            SMDef = new SMDefinition();
        }
    }

    public object Clone()
    {
        return new Config
        {
            AutoCopy = AutoCopy,
            AutoUpload = AutoUpload,
            AutoRcon = AutoRcon,
            CopyDirectory = CopyDirectory,
            DeleteAfterCopy = DeleteAfterCopy,
            FtpDir = FtpDir,
            FtpHost = FtpHost,
            FtpPassword = FtpPassword,
            FTPUser = FTPUser,
            Name = Name,
            OptimizeLevel = OptimizeLevel,
            PostCmd = PostCmd,
            PreCmd = PreCmd,
            RConCommands = RConCommands,
            RConIP = RConIP,
            RConPort = RConPort,
            RConPassword = RConPassword,
            ServerArgs = ServerArgs,
            ServerFile = ServerFile,
            SMDef = SMDef,
            SMDirectories = new(SMDirectories),
            RejectedPaths = new(RejectedPaths),
            Standard = Standard,
            VerboseLevel = VerboseLevel
        };
    }
}