﻿using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Preloader.Core;
using HarmonyLib;
using UnityEngine;
using Array = System.Array;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        private static IRegionInfo[] _newRegions = new IRegionInfo[]
        {
            new DnsRegionInfo("192.241.154.115", "skeld.net", StringNames.NoTranslation, "192.241.154.115")
                .Duplicate(),
            new DnsRegionInfo("localhost", "localhost", StringNames.NoTranslation, "127.0.0.1")
                .Duplicate()
        };
        private static IRegionInfo[] _customRegions = MergeRegions(_newRegions, LoadCustomUserRegions());
        
        public static void Patch()
        {
            IRegionInfo[] patchedRegions = MergeRegions(ServerManager.DefaultRegions, _customRegions);

            ServerManager.DefaultRegions = patchedRegions;
            ServerManager.Instance.AvailableRegions = patchedRegions;
            ServerManager.Instance.SaveServers();
        }

        private static IRegionInfo[] MergeRegions(IRegionInfo[] oldRegions, IRegionInfo[] newRegions)
        {
            IRegionInfo[] patchedRegions = new IRegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            return patchedRegions;
        }
        
        private static IRegionInfo[] LoadCustomUserRegions()
        {
            List<IRegionInfo> customRegions = new List<IRegionInfo>();
            
            for (int x = 0; x < 5; x++)
            {
                ConfigEntry<string> regionName = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", $"Name", "custom region");
                ConfigEntry<string> regionIp = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", "IP", "");

                if (String.IsNullOrWhiteSpace(regionIp.Value)) continue;

                IRegionInfo regionInfo = new DnsRegionInfo(
                    regionIp.Value, regionName.Value, StringNames.NoTranslation, regionIp.Value)
                    .Duplicate();
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
    }
}