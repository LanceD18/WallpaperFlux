using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.Windows
{
    /// <summary>
    /// Interaction logic for InputBoxView.xaml
    /// </summary>
    public partial class InputBoxWindow
    {
        public string Input;

        // TODO Add a way to cancel input, either via a button, submitting empty input (Which would also occur is the 'X' button was pressed), or some other method
        public InputBoxWindow(string title, string caption, string watermark, InputBoxType inputBoxType)
        {
            InitializeComponent();

            WindowInputBox.Title = title;
            TextBlockCaption.Text = caption;
            AdonisUI.Extensions.WatermarkExtension.SetWatermark(TextBoxInput, watermark);

            switch (inputBoxType)
            {
                case InputBoxType.Default:
                    // just string input
                    break;

                case InputBoxType.PositiveInteger:
                    TextBoxInput.PreviewMouseDown += TextBox_MouseEvent_FocusText;
                    TextBoxInput.PreviewMouseUp += TextBox_MouseEvent_FocusText;
                    TextBoxInput.PreviewTextInput += OnPreviewTextInput_PositiveNumbersOnly;
                    break;

                case InputBoxType.PositiveFloat:
                    TextBoxInput.PreviewMouseDown += TextBox_MouseEvent_FocusText;
                    TextBoxInput.PreviewMouseUp += TextBox_MouseEvent_FocusText;
                    TextBoxInput.PreviewTextInput += OnPreviewTextInput_PositiveNumbersAndDecimalsOnly;
                    break;
            }

            TextBoxInput.Focus();
        }

        private void TextBox_OnKeyDownEnter_Submit(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Input = TextBoxInput.Text;
                this.Close();
            }
        }

        // Selects all text on mouse down
        private async void TextBox_MouseEvent_FocusText(object sender, MouseButtonEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(((TextBox)sender).SelectAll);
        }

        // Constrains text preview to numbers only
        private void OnPreviewTextInput_PositiveNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Constrains text preview to numbers and decimal places only
        private void OnPreviewTextInput_PositiveNumbersAndDecimalsOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9\\.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Constrains text preview to numbers only, allows negative input
        private void OnPreviewTextInput_NumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9-]");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Constrains text preview to numbers and decimal places only, allows negative input
        private void OnPreviewTextInput_NumbersAndDecimalsOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9-\\.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
