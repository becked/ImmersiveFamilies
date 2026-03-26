using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace ImmersiveFamilies
{
    public class ModEntryPoint : ModEntryPointAdapter
    {
        private const string HarmonyId = "com.ahmedaboulenein.immersivefamilies";
        private static Harmony _harmony;

        // (nationZType, familyClassZType) → textZType
        internal static Dictionary<(string, string), string> NameMappings;

        // Original values per family, captured at init before any mutations
        internal static Dictionary<FamilyType, TextType> OriginalNames;
        internal static Dictionary<FamilyType, int> OriginalColorIndices;

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);

            if (_harmony != null) return; // Triple-load guard: game loads DLL three times

            try
            {
                // Set _harmony early so the triple-load guard works even if we throw below
                _harmony = new Harmony(HarmonyId);

                Infos infos = modSettings.Infos;
                OriginalNames = new Dictionary<FamilyType, TextType>();
                OriginalColorIndices = new Dictionary<FamilyType, int>();
                for (FamilyType f = 0; f < infos.familiesNum(); f++)
                {
                    OriginalNames[f] = infos.family(f).meName;
                    OriginalColorIndices[f] = infos.family(f).miColorIndex;
                }

                NameMappings = LoadConfig(modSettings);

                _harmony.PatchAll(typeof(ModEntryPoint).Assembly);
                Debug.Log($"[ImmersiveFamilies] Loaded {NameMappings.Count} name mappings, captured {OriginalNames.Count} original names.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImmersiveFamilies] Failed to initialize: {ex}");
            }
        }

        public override void Shutdown()
        {
            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
            NameMappings = null;
            OriginalNames = null;
            OriginalColorIndices = null;
            Debug.Log("[ImmersiveFamilies] Harmony patches removed.");
            base.Shutdown();
        }

        private static Dictionary<(string, string), string> LoadConfig(ModSettings modSettings)
        {
            var mappings = new Dictionary<(string, string), string>();

            // Find our mod's directory via ModRecord.ModdedPath
            string modDir = null;
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            foreach (var mod in modSettings.ModPath.GetMods())
            {
                if (mod.ModName == assemblyName)
                {
                    modDir = mod.ModdedPath;
                    break;
                }
            }

            if (modDir == null)
            {
                Debug.LogWarning("[ImmersiveFamilies] Could not find mod directory.");
                return mappings;
            }

            string configPath = Path.Combine(modDir, "Infos", "family-class-names.xml");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning("[ImmersiveFamilies] No config file found at: " + configPath);
                return mappings;
            }

            var doc = new XmlDocument();
            doc.Load(configPath);

            foreach (XmlNode entry in doc.SelectNodes("//Entry"))
            {
                string nation = entry.SelectSingleNode("Nation")?.InnerText;
                string familyClass = entry.SelectSingleNode("FamilyClass")?.InnerText;
                string name = entry.SelectSingleNode("Name")?.InnerText;

                if (nation != null && familyClass != null && name != null)
                {
                    mappings[(nation, familyClass)] = name;
                }
            }

            return mappings;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.loadFamilyClass))]
    public static class PatchLoadFamilyClass
    {
        public static void Postfix(Game __instance, FamilyType eIndex, FamilyClassType eNewValue)
        {
            try
            {
                if (ModEntryPoint.NameMappings == null) return;
                if (eNewValue == FamilyClassType.NONE) return;

                Infos infos = __instance.infos();
                InfoFamily familyInfo = infos.family(eIndex);
                if (familyInfo == null) return;

                string nationZType = null;
                for (NationType n = 0; (int)n < familyInfo.mabNation.Count; n++)
                {
                    if (familyInfo.mabNation[(int)n])
                    {
                        nationZType = infos.nation(n).mzType;
                        break;
                    }
                }
                if (nationZType == null) return;

                string classZType = infos.familyClass(eNewValue).mzType;

                if (ModEntryPoint.NameMappings.TryGetValue(
                        (nationZType, classZType), out string textZType))
                {
                    TextType newName = infos.getType<TextType>(textZType);
                    if (newName != TextType.NONE)
                    {
                        familyInfo.meName = newName;
                    }
                    else
                    {
                        Debug.LogWarning($"[ImmersiveFamilies] Text key not found: {textZType}");
                    }
                    familyInfo.miColorIndex = (int)eNewValue;
                }
                else if (ModEntryPoint.OriginalNames != null
                         && ModEntryPoint.OriginalNames.TryGetValue(eIndex, out TextType originalName))
                {
                    familyInfo.meName = originalName;
                    if (ModEntryPoint.OriginalColorIndices.TryGetValue(eIndex, out int originalColorIndex))
                    {
                        familyInfo.miColorIndex = originalColorIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImmersiveFamilies] Error in loadFamilyClass postfix: {ex}");
            }
        }
    }
}
