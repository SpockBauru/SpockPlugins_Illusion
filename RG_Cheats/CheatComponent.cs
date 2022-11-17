using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RG_Cheats
{
    public class CheatComponent : MonoBehaviour
    {
        public CheatComponent(IntPtr handle) : base(handle) { }
        void OnEnable()
        {
            RG_Cheats.charaStatus = RG_Cheats.statusUI.Target;
            RG_Cheats.Hooks.currentCharacter = RG_Cheats.charaStatus.name;
            RG_Cheats.UpdateCheatCanvas(RG_Cheats.charaStatus);
        }
    }
}
