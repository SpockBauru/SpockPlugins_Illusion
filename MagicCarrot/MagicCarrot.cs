using BepInEx.Preloader.Core.Patching;
using Mono.Cecil;
using System.Linq;

namespace IllusionFixes.Patchers
{
    /// <summary>
    /// Created by Horse.
    /// > Fixes random hangups in Illusion's new game (AI-Shoujo). Why? I dunno lol
    /// </summary>
    [PatcherPluginInfo("illusionfixes.patchers.magiccarrot", "Magic Carrot BepInEx 6.0", "1.0")]
    public class MagicCarrot : BasePatcher
    {
        // Making the same patch twice, because targeting multiple assemblies is not working like in BepInEx 6.0 documentation
        [TargetAssembly("Sirenix.Utilities.dll")]
        public void PatchAssembly(AssemblyDefinition ad)
        {
            var assemblyUtilities = ad.MainModule.Types.FirstOrDefault(t => t.Name == "AssemblyUtilities");
            var cctor = assemblyUtilities?.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (cctor == null)
                return;
            assemblyUtilities.Methods.Remove(cctor);
        }

        [TargetAssembly("Sirenix.Serialization.dll")]
        public void PatchAssembly2(AssemblyDefinition ad)
        {
            var assemblyUtilities = ad.MainModule.Types.FirstOrDefault(t => t.Name == "AssemblyUtilities");
            var cctor = assemblyUtilities?.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (cctor == null)
                return;
            assemblyUtilities.Methods.Remove(cctor);
        }
    }
}
