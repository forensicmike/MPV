using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using MahApps.Metro.Controls.Dialogs;

namespace MassPlistViewer
{
    public class PlistViewModel : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string _content;
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                NotifyPropertyChanged("Content");
            }
        }

        private FileInfo _sourceFile;
        public FileInfo sourceFile
        {
            get { return _sourceFile; }
            set
            {
                _sourceFile = value;
                NotifyPropertyChanged("sourceFile");
            }
        }

        private CheatSheetEntry _assocCheatSheet;
        public CheatSheetEntry assocCheatSheet
        {
            get { return _assocCheatSheet; }
            set
            {
                _assocCheatSheet = value;
                NotifyPropertyChanged("assocCheatSheet");
            }
        }



        private InlineCommand _openPlistInNotepadPPCommand;
        public InlineCommand openPlistInNotepadPPCommand
        {
            get
            {
                if (_openPlistInNotepadPPCommand == null)
                {
                    _openPlistInNotepadPPCommand = new InlineCommand((obj) =>
                    {

                        if (sourceFile == null || !sourceFile.Exists)
                        {
                            ViewModel.MainWindowRef.ShowMessageAsync("Error", "There is no file to launch. Did you load by dragging and dropping?");
                            return;
                        }

                        var nppPath = Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\Notepad++\notepad++.exe");
                        if (!File.Exists(nppPath))
                        {
                            ViewModel.MainWindowRef.ShowMessageAsync("Error", "Wasn't able to find NP++... reverting to notepad..");

                            var psinfo = new ProcessStartInfo("notepad.exe", this.sourceFile.FullName);
                            Process.Start(psinfo);
                        }
                        else
                        {
                            var psinfo = new ProcessStartInfo(nppPath, this.sourceFile.FullName);
                            Process.Start(psinfo);
                        }

                    });
                }
                return _openPlistInNotepadPPCommand;
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
