using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace RepoPhotonCloudMod
{
    [BepInPlugin("jp.user.repophotoncloud", "REPO Photon Cloud", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance;
        internal static ConfigEntry<string> PhotonAppId;
        internal static ConfigEntry<string> Region;
        internal static ConfigEntry<bool> SetSteamAppId480;
        internal static ConfigEntry<bool> EnablePhotonPatch;
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            PhotonAppId = Config.Bind("Photon", "AppIdRealtime", "",
                "Photon Dashboard > Realtime App ID. Leave empty to keep the game's current App ID.");
            Region = Config.Bind("Photon", "FixedRegion", "",
                "Optional Photon region code (for example jp, us, eu). Empty uses Photon best-region selection.");
            SetSteamAppId480 = Config.Bind("Steam", "SetAppId480", true,
                "Writes steam_appid.txt containing 480 and sets SteamAppId/SteamGameId environment variables.");
            EnablePhotonPatch = Config.Bind("Photon", "EnableConnectionPatch", true,
                "Apply the configured App ID and region immediately before PUN ConnectUsingSettings calls.");

            if (SetSteamAppId480.Value) ConfigureSteamAppId();
            if (EnablePhotonPatch.Value) InstallPhotonPatch();

            Logger.LogInfo("REPO Photon Cloud loaded. Configure BepInEx/config/jp.user.repophotoncloud.cfg.");
            if (string.IsNullOrWhiteSpace(PhotonAppId.Value))
                Logger.LogWarning("Photon AppIdRealtime is empty; the game/default App ID will be retained.");
        }

        private void OnDestroy()
        {
            if (_harmony != null) _harmony.UnpatchSelf();
            Instance = null;
        }

        private void ConfigureSteamAppId()
        {
            try
            {
                Environment.SetEnvironmentVariable("SteamAppId", "480");
                Environment.SetEnvironmentVariable("SteamGameId", "480");
                string gameRoot = Paths.GameRootPath;
                string file = Path.Combine(gameRoot, "steam_appid.txt");
                File.WriteAllText(file, "480");
                Logger.LogInfo("Steam test AppID configured as 480 (steam_appid.txt).");
            }
            catch (Exception ex)
            {
                Logger.LogError("Could not configure Steam AppID 480: " + ex);
            }
        }

        private void InstallPhotonPatch()
        {
            try
            {
                Type photonNetwork = FindType("Photon.Pun.PhotonNetwork");
                if (photonNetwork == null)
                {
                    Logger.LogWarning("Photon.Pun.PhotonNetwork was not found. Photon patch not installed.");
                    return;
                }

                MethodInfo prefix = typeof(Plugin).GetMethod("ConnectUsingSettingsPrefix",
                    BindingFlags.Static | BindingFlags.NonPublic);
                int patched = 0;
                MethodInfo[] methods = photonNetwork.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "ConnectUsingSettings") continue;
                    if (method.IsGenericMethodDefinition) continue;
                    if (method.GetParameters().Length > 2) continue;
                    if (_harmony == null) _harmony = new Harmony("jp.user.repophotoncloud");
                    _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                    patched++;
                }
                Logger.LogInfo("Patched PhotonNetwork.ConnectUsingSettings overloads: " + patched);
                if (patched == 0) Logger.LogWarning("No ConnectUsingSettings overloads found; verify this game's PUN version.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Photon patch installation failed: " + ex);
            }
        }

        // Harmony's __args contains the original method arguments independent of overload signature.
        private static void ConnectUsingSettingsPrefix(object[] __args)
        {
            try
            {
                string appId = PhotonAppId == null ? "" : PhotonAppId.Value;
                string region = Region == null ? "" : Region.Value;

                // PUN overload taking AppSettings: modify that exact object before it is consumed.
                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        object arg = __args[i];
                        if (arg != null && arg.GetType().FullName == "Photon.Realtime.AppSettings")
                            ApplySettings(arg, appId, region);
                    }
                }

                // Standard PUN path: PhotonNetwork.PhotonServerSettings.AppSettings.
                Type photonNetwork = FindType("Photon.Pun.PhotonNetwork");
                object serverSettings = GetStaticMember(photonNetwork, "PhotonServerSettings");
                object appSettings = GetMember(serverSettings, "AppSettings");
                if (appSettings != null) ApplySettings(appSettings, appId, region);
            }
            catch (Exception ex)
            {
                if (Instance != null) Instance.Logger.LogError("Applying Photon settings failed: " + ex);
            }
        }

        private static void ApplySettings(object settings, string appId, string region)
        {
            if (!string.IsNullOrWhiteSpace(appId))
            {
                if (SetMember(settings, "AppIdRealtime", appId) &&
                    Instance != null)
                    Instance.Logger.LogInfo("Photon Realtime App ID overridden from config.");
            }
            if (!string.IsNullOrWhiteSpace(region))
            {
                if (SetMember(settings, "FixedRegion", region) && Instance != null)
                    Instance.Logger.LogInfo("Photon fixed region set to: " + region);
            }
        }

        private static Type FindType(string fullName)
        {
            Type t = Type.GetType(fullName + ", PhotonUnityNetworking", false);
            if (t != null) return t;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try { t = assemblies[i].GetType(fullName, false); }
                catch { t = null; }
                if (t != null) return t;
            }
            return null;
        }

        private static object GetStaticMember(Type type, string name)
        {
            if (type == null) return null;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo p = type.GetProperty(name, flags);
            if (p != null) return p.GetValue(null, null);
            FieldInfo f = type.GetField(name, flags);
            return f == null ? null : f.GetValue(null);
        }

        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo p = target.GetType().GetProperty(name, flags);
            if (p != null) return p.GetValue(target, null);
            FieldInfo f = target.GetType().GetField(name, flags);
            return f == null ? null : f.GetValue(target);
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo p = target.GetType().GetProperty(name, flags);
            if (p != null && p.CanWrite && p.PropertyType == value.GetType())
            {
                p.SetValue(target, value, null);
                return true;
            }
            FieldInfo f = target.GetType().GetField(name, flags);
            if (f != null && f.FieldType == value.GetType())
            {
                f.SetValue(target, value);
                return true;
            }
            return false;
        }
    }
}
