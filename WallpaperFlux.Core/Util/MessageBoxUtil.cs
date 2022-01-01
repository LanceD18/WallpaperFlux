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

        public static bool ShowErrorRequestRetry(string text = "Invalid Input. Retry?")
        {
            MessageBoxModel message = new MessageBoxModel()
            {
                Text = "Invalid Input. Retry?",
                Caption = "Error",
                Icon = MessageBoxImage.Error,
                Buttons = MessageBoxButtons.YesNo()
            };

            MessageBoxResult result = MessageBox.Show(message);
            return result == MessageBoxResult.Yes;
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

        #region Input Box
        // TODO Find a better way to implement this
        public static Func<string, string, string, InputBoxType, string> InputBoxFunc;

        // Default Input Box Call
        public static string InputBox(string title, string caption, string watermark = "", InputBoxType inputBoxType = InputBoxType.Default)
        {
            return InputBoxFunc.Invoke(title, caption, watermark, inputBoxType);
        }

        public static string GetString(string title, string caption, string watermark = "", InputBoxType inputBoxType = InputBoxType.Default)
        {
            return InputBoxFunc.Invoke(title, caption, watermark, InputBoxType.Default);
        }

        // Generic Number Prompt
        private static bool PromptNumber(string title, string caption, out float response, string watermark, InputBoxType inputBoxType)
        {
            response = -1;

            try
            {
                string input = InputBox(title, caption, watermark, inputBoxType);
                if (input == null) return false; // cancelled

                // cannot be negative since the '-' symbol is disabled

                switch (inputBoxType)
                {
                    case InputBoxType.Integer:
                    case InputBoxType.PositiveInteger:
                        response = int.Parse(input);
                        break;

                    case InputBoxType.Float:
                    case InputBoxType.PositiveFloat:
                        response = float.Parse(input);
                        break;
                }
                
                return true;
            }
            catch (Exception e)
            {
                if (ShowErrorRequestRetry())
                {
                    PromptNumber(title, caption, out response, watermark, inputBoxType);
                }

                return false;
            }
        }

        // ----- Get Number Variations -----
        // Integer
        public static bool GetInteger(string title, string caption, out int response, string watermark = "")
        {
            bool promptResult = PromptNumber(title, caption, out float responseFloat, watermark, InputBoxType.Integer);
            response = (int)responseFloat;
            return promptResult;
        }

        public static bool GetPositiveInteger(string title, string caption, out int response, string watermark = "")
        {
            bool promptResult = PromptNumber(title, caption, out float responseFloat, watermark, InputBoxType.PositiveInteger);
            response = (int)responseFloat;
            return promptResult;
        }

        // Float
        public static bool GetFloat(string title, string caption, out float response, string watermark = "")
        {
            return PromptNumber(title, caption, out response, watermark, InputBoxType.Float);
        }

        public static bool GetPositiveFloat(string title, string caption, out float response, string watermark = "")
        {
            return PromptNumber(title, caption, out response, watermark, InputBoxType.PositiveFloat);
        }
        #endregion
    }

    public enum InputBoxType
    {
        Default,
        Integer,
        PositiveInteger,
        Float,
        PositiveFloat
    }
}
