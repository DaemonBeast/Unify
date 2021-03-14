﻿using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Unify.Patches;

namespace Unify
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    public class UnifyPlugin : BasePlugin
    {
        public const string Id = "daemon.unify.reactor";
        private const string Name = "Unify";
        private const string Version = "2.0.2";
        
        public static readonly ConfigFile ConfigFile =
            new ConfigFile(Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg"), true);

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            Harmony.PatchAll();
        }
    }
}