using System;
using System.Windows.Forms;
using System.Windows.Interop;
using LanceTools.WinForm.FormUtil;
using LanceTools.WPF.Adonis.Util;
using WallpaperFlux.Core.Util;
using static System.String;
using Application = System.Windows.Application;

namespace WallpaperFlux.WPF.Tools
{
    public class HotkeyManager
    {
        private GlobalHotkey _ghShiftAltCtrl;

        private IntPtr _handle;

        private HwndSource _source;

        public HotkeyManager(IntPtr handle)
        {
            _handle = handle;
            _source = HwndSource.FromHwnd(_handle);

            if (_source != null)
            {
                _source.AddHook(HwndHook);
                RegisterKeys();
            }
            else
            {
                MessageBoxUtil.ShowError("Hotkey registration failed!");
            }
        }

        private void RegisterKeys()
        {
            // GlobalHotkey
            _ghShiftAltCtrl = new GlobalHotkey(VirtualKey.SHIFT + VirtualKey.ALT + VirtualKey.CTRL, Keys.None, _handle);
            //ghDivide = new GlobalHotkey(VirtualKey.NOMOD, Keys.Divide, this);
            //ghMultiply = new GlobalHotkey(VirtualKey.NOMOD, Keys.Multiply, this);
            //ghNumPad5 = new GlobalHotkey(VirtualKey.NOMOD, Keys.NumPad5, this);

            if (!_ghShiftAltCtrl.Register())
            {
                MessageBoxUtil.ShowError("ALT + SHIFT + CTRL hotkey failed to register!");
            }
        }

        public void UnregisterKeys()
        {
            if (!_ghShiftAltCtrl.Unregister())
            {
                MessageBoxUtil.ShowError("ALT + SHIFT + CTRL hotkey failed to unregister!");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == VirtualKey.WM_HOTKEY_MSG_ID)
            {
                OnHotKeyPressed(wParam);
                handled = true;
            }
            
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed(IntPtr wParam)
        {
            int hotkeyId = wParam.ToInt32();

            if (hotkeyId == _ghShiftAltCtrl.GetHashCode())
            {
                //xif (!IsNullOrEmpty(JsonUtil.LoadedThemePath))
                //x{
                    //xJsonUtil.QuickSave();
                    MainWindow.Instance.Close();
                //x}
            }

            /*x
            // opens the default theme
            if (ThemeUtil.ThemeSettings)
            {
                //xWallpaperData.SaveData(WallpaperPathSetter.ActiveWallpaperTheme);
                //xWallpaperData.LoadDefaultTheme();
            }
            */
        }
    }
}
