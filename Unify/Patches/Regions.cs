﻿using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel.Udp;
using Reactor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        private static IRegionInfo[] _oldRegions = ServerManager.DefaultRegions;
        
        private static IRegionInfo[] _newRegions = new IRegionInfo[]
        {
            new DnsRegionInfo("192.241.154.115", "skeld.net", StringNames.NoTranslation, "192.241.154.115")
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("localhost", "localhost", StringNames.NoTranslation, "127.0.0.1")
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("152.228.160.91", "matux.fr", StringNames.NoTranslation, "152.228.160.91")
                .Cast<IRegionInfo>()
        };

        public static List<IRegionInfo> ModRegions = new List<IRegionInfo>();

        public static IRegionInfo DirectRegion;

        private static TextBox directConnect;

        public static void Patch()
        {
            IRegionInfo[] customRegions = UnifyPlugin.MergeRegions(_newRegions, ModRegions.ToArray());
            customRegions = UnifyPlugin.MergeRegions(customRegions, LoadCustomUserRegions());
            if (DirectRegion != null) customRegions = customRegions.AddToArray(DirectRegion);
            IRegionInfo[] patchedRegions = UnifyPlugin.MergeRegions(_oldRegions, customRegions);

            ServerManager.DefaultRegions = patchedRegions;
            ServerManager.Instance.AvailableRegions = patchedRegions;
            ServerManager.Instance.SaveServers();
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
                    .Cast<IRegionInfo>();
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
        
        private static void UpdateRegion()
        {
            RegionMenu regionMenu = GameObject.Find("RegionMenu").GetComponent<RegionMenu>();
                
            IRegionInfo newRegion = UnifyPlugin.SetDirectRegion(directConnect.text);
                
            regionMenu.ChooseOption(newRegion);
            regionMenu.Close();
        }
        
        [HarmonyPatch(typeof(NameTextBehaviour), nameof(NameTextBehaviour.Start))]
        public static class DirectConnectButtonPatch
        {
            public static void Postfix()
            {
                if (directConnect) return;

                JoinGameButton joinGameButton = DestroyableSingleton<JoinGameButton>.Instance;
                RegionMenu regionMenu = DestroyableSingleton<RegionMenu>.Instance;

                directConnect = Object.Instantiate(joinGameButton.GameIdText, regionMenu.transform);
                directConnect.gameObject.SetActive(false);
                directConnect.IpMode = true;
                directConnect.characterLimit = 15;
                directConnect.ClearOnFocus = false;

                directConnect.OnEnter = new Button.ButtonClickedEvent();
                directConnect.OnEnter.AddListener((Action) UpdateRegion);

                float offset = (float) 0.5 * ServerManager.DefaultRegions.Length;
                directConnect.transform.localPosition = new Vector3(0, 2 - offset, -100);
            }
        }

        [HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
        public static class DirectConnectEnterButtonPatch
        {
            public static bool Prefix(JoinGameButton __instance)
            {
                GameObject regionMenuGameObject = GameObject.Find("RegionMenu");
                if (!regionMenuGameObject) return true;

                UpdateRegion();

                return false;
            }
        }

        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
        public static class ShowDirectConnectPatch
        {
            public static void Postfix()
            {
                directConnect.gameObject.SetActive(true);
            }
        }
        
        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Close))]
        public static class HideDirectConnectPatch
        {
            public static void Postfix()
            {
                directConnect.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.SaveServers))]
        public static class HideDirectConnectOnSelectPatch
        {
            public static void Postfix()
            {
                directConnect.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(UdpConnection), nameof(UdpConnection.HandleSend))]
    public static class DisableModdedHandshakePatch
    {
        [HarmonyBefore(new string[] { "gg.reactor.api" })]
        public static void Prefix()
        {
            if (UnifyPlugin.HandshakeDisabled) return;
            if (!UnifyPlugin.NormalHandshake.Contains(ServerManager.Instance.CurrentRegion.Name)) return;
            
            PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value = false;
        }

        public static void Postfix()
        {
            if (UnifyPlugin.HandshakeDisabled) return;
            
            PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value = true;
        }
    }
}

/*

Change GUID
Change fields to properties
Change layout of region menu

*/