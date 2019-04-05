using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace MassPlistViewer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            viewModel = new AboutViewModel();
            this.DataContext = viewModel;
            viewModel.NotifyDialogClosed += (o, e) =>
            {
                this.Close();
            };
        }

        public AboutViewModel viewModel { get; set; }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://choosealicense.com/licenses/gpl-3.0/#");
        }
    }

    public class AboutViewModel : INotifyPropertyChanged
    {

        public AboutViewModel()
        {

        }

        public event EventHandler NotifyDialogClosed = (o, e) => { };

        private InlineCommand _closeCommand;
        public InlineCommand closeCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    _closeCommand = new InlineCommand((obj) =>
                    {
                        // ..
                        NotifyDialogClosed(this, new EventArgs());
                    });
                }
                return _closeCommand;
            }
        }

        #region Standard INotifyPropertyChanged Implementation

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };
        #endregion

    }
}
