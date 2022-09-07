using BepInEx;
using BepInEx.IL2CPP;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnableResizeIL2CPP
{
    [BepInPlugin(GUID, GUID, Version)]
    public class BepInExLoader : BasePlugin
    {
        public const string GUID = "EnableResizeIL2CPP";

        public const string Version = "0.5";

        public override void Load()
        {
            //IL2CPP don't automatically inherits Monobehavior, so needs to add separatelly
            ClassInjector.RegisterTypeInIl2Cpp<EnableResizeComponent>();
            GameObject EnableResize = new GameObject("EnableResize_IL2CPP");
            GameObject.DontDestroyOnLoad(EnableResize);
            EnableResize.hideFlags = HideFlags.HideAndDontSave;
            EnableResize.AddComponent<EnableResizeComponent>();
        }
    }

    public class EnableResizeComponent : MonoBehaviour
    {
        //Got this from BepInEx Discord pinned messages
        public EnableResizeComponent(IntPtr handle) : base(handle) { }


        //Old code from mono version starts here
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("USER32.DLL")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x40000;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const string GET_CLASS_NAME_MAGIC = "UnityWndClass";

        private IntPtr WindowHandle = IntPtr.Zero;
        private bool prev = false;

        private void ResizeWindow()
        {
            if (Screen.fullScreen) return;
            var style = GetWindowLong(WindowHandle, GWL_STYLE);
            style |= WS_THICKFRAME | WS_MAXIMIZEBOX;
            SetWindowLong(WindowHandle, GWL_STYLE, style);
        }

        public void Awake()
        {
            var pid = Process.GetCurrentProcess().Id;
            EnumWindows((w, param) =>
            {
                if (w == IntPtr.Zero) return true;
                if (GetWindowThreadProcessId(w, out uint lpdwProcessId) == 0) return true;
                if (lpdwProcessId != pid) return true;
                var cn = new StringBuilder(256);
                if (GetClassName(w, cn, cn.Capacity) == 0) return true;
                if (cn.ToString() != GET_CLASS_NAME_MAGIC) return true;
                WindowHandle = w;
                return false;
            }, IntPtr.Zero);

            if (WindowHandle == IntPtr.Zero) return;

            //Scene Manager is done differently on unhollowed
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((s, lsm) =>
            {
                if (lsm == LoadSceneMode.Single) ResizeWindow();
            }));
        }

        public void Update()
        {
            var fs = Screen.fullScreen;
            if (!fs && prev) ResizeWindow();
            prev = fs;

            //TODO: send resize message when (fs && !prev) ?
        }
    }
}
