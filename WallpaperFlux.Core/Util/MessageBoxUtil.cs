using System;
using System.Collections.Generic;
using System.Text;
using AdonisUI.Controls;

namespace WallpaperFlux.Core.Util
{
    // for use with AdonisUI's MessageBox Control
    public static class MessageBoxUtil
    {
        public static void ShowError(string text)
        {
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = text,
                Caption = "Error",
                Icon = MessageBoxImage.Error,
                Buttons = new[] { MessageBoxButtons.Ok() }
            };

            MessageBox.Show(messageBox);
        }
    }
}
