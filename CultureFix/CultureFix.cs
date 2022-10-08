using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Preloader.Core.Patching;

namespace IllusionFixes.Patchers
{
    [PatcherPluginInfo("illusionfixes.patchers.culturefix", "Culture Fix BepInEx 6.0", "1.0")]
    public class CultureFix : BasePatcher
    {
        public override void Initialize()
        {
            var cf = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "CultureFix.cfg"), true);

            var cultureCode = cf.Bind("Bug Fixes", "Override culture", "ja-JP", "If not empty, set the process culture to this. Works similarly to a locale emulator. Fixes game crashes and lockups on some system locales.\nThe value has to be in the language-region format (e.g. en-US).").Value;

            if (string.IsNullOrEmpty(cultureCode))
            {
                Log.LogInfo("CultureFix is disabled");
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureCode);

                if (culture.IsNeutralCulture)
                {
                    Log.LogInfo("CultureFix failed to load - The sepecified culture " + cultureCode + " is neutral. It has to be in the language-region format (e.g. en-US).");
                    return;
                }

                Log.LogInfo("CultureFix - Forcing process culture to: " + cultureCode);

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception ex)
            {
                Log.LogInfo("CultureFix failed to load - Crashed while trying to set culture " + cultureCode + " - " + ex);
            }
        }
    }
}
