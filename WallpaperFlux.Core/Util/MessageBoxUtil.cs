using System;
using System.Collections.Generic;
using System.Text;
using AdonisUI.Controls;

namespace WallpaperFlux.Core.Util
{
    // TODO Move this to LanceTools in some form
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

        public static void ShowImportant(string text)
        {
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = text,
                Caption = "Important",
                Icon = MessageBoxImage.Exclamation,
                Buttons = new[] { MessageBoxButtons.Ok() }
            };

            MessageBox.Show(messageBox);
        }

        public static void ShowStop(string text)
        {
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = text,
                Caption = "Stop",
                Icon = MessageBoxImage.Stop,
                Buttons = new[] { MessageBoxButtons.Ok() }
            };

            MessageBox.Show(messageBox);
        }

        public static void ShowInformation(string text)
        {
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = text,
                Caption = "Information",
                Icon = MessageBoxImage.Information,
                Buttons = new[] { MessageBoxButtons.Ok() }
            };

            MessageBox.Show(messageBox);
        }
    }
}
