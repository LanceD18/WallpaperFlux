using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MvvmCross.Platforms.Wpf.Presenters;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Views;
using WallpaperFlux.WPF.Windows;

namespace WallpaperFlux.WPF
{
    public class ViewPresenter : MvxWpfViewPresenter
    {
        public Window ViewWindow;

        private WindowStartupLocation _windowStartupLocation;

        public ViewPresenter(Type viewType, Type viewModelType, float width, float height, string title, bool modal)
        {
            MvxWindowPresentationAttribute attribute = new MvxWindowPresentationAttribute
            {
                Modal = modal
            };
            
            Show(attribute, viewType, viewModelType, width, height, title);
        }

        // For reference: https://github.com/MvvmCross/MvvmCross/blob/master/MvvmCross/Platforms/Wpf/Presenters/MvxWpfViewPresenter.cs
        public void Show(MvxWindowPresentationAttribute attribute, Type viewType, Type viewModelType, float width, float height, string title = "",
        WindowStartupLocation startupLocation = WindowStartupLocation.CenterScreen)
        {
            MvxViewModelRequest request = new MvxViewModelInstanceRequest(viewModelType);
            FrameworkElement viewElement = WpfViewLoader.CreateView(viewType);

            _windowStartupLocation = startupLocation; //? Needs to be set before showing the window
            ShowWindow(viewElement, attribute, request);

            ViewWindow.Height = height;
            ViewWindow.Width = width;
            ViewWindow.Title = title;
        }

        //? this is a replica of the default ShowWindow code with some slight modifications to allow for compatibility with MvvvmCross
        protected override Task<bool> ShowWindow(FrameworkElement element, MvxWindowPresentationAttribute attribute, MvxViewModelRequest request)
        {
            Window window;
            if (element is IMvxWindow mvxWindow)
            {
                window = (Window)element;
                mvxWindow.Identifier = attribute.Identifier ?? element.GetType().Name;
            }
            else if (element is Window normalWindow)
            {
                // Accept normal Window class
                window = normalWindow;
            }
            else
            {
                // Wrap in window
                window = new MvxWindow
                {
                    Identifier = attribute.Identifier ?? element.GetType().Name
                };
            }
            
            //? Applies "default" styling, which in the case of this program is: AdonisUI
            window.Style = new Style(typeof(Window), (Style)window.FindResource(typeof(Window)));

            window.Closed += Window_Closed;
            FrameworkElementsDictionary.Add(window, new Stack<FrameworkElement>());

            if (!(element is Window))
            {
                FrameworkElementsDictionary[window].Push(element);
                window.Content = element;
            }

            window.WindowStartupLocation = _windowStartupLocation;
            ViewWindow = window;

            if (attribute.Modal)
                window.ShowDialog();
            else
                window.Show();
            return Task.FromResult(true);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            var window = sender as Window;
            window.Closed -= Window_Closed;

            if (FrameworkElementsDictionary.ContainsKey(window))
                FrameworkElementsDictionary.Remove(window);
            
            ViewWindow = null;
        }
    }
}
