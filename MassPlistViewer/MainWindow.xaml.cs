using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using System.Data.SQLite;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

namespace MassPlistViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();
            this.DataContext = viewModel;
            ViewModel.MainWindowRef = this;
            ViewModel.textEditor = textEditor;

            // Colorization for when the user is searching stuff.
            // Thanks StackOverflow!
            textEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit());
            textEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Yellow;
            textEditor.TextArea.SelectionChanged += textEditor_TextArea_SelectionChanged;
        }

        void textEditor_TextArea_SelectionChanged(object sender, EventArgs e)
        {
            this.textEditor.TextArea.TextView.Redraw();
        }

        public ViewModel viewModel { get; set; }

        //  Handle drag & drop of plists en masse.
        private void Win1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                var fileList = e.Data.GetData("FileDrop") as string[];
                viewModel.HandlePlists(fileList);
            }
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            // We use this to delay searches by a few seconds while the user is actively typing.
            dtSearchThrottle.Tick += (o, e) =>
            {
                NotifyPropertyChanged("LoadedDataCVS");
                dtSearchThrottle.Stop();
            };

            // Load in the cheatsheet from JSON
            using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MassPlistViewer.CheatSheet.json")))
            {
                var cheatsheet = sr.ReadToEnd();
                CheatSheetEntries = JsonConvert.DeserializeObject<CheatSheetEntry[]>(cheatsheet);
            }
        }

        static public CheatSheetEntry[] CheatSheetEntries { get; set; }
        static public MetroWindow MainWindowRef;
        static public ICSharpCode.AvalonEdit.TextEditor textEditor;
        static public string[] ActiveSearchTerms { get; set; }

        DispatcherTimer dtSearchThrottle = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(300) };

        #region UI controlled properties
        private bool _FilterByCheatSheet;
        public bool FilterByCheatSheet
        {
            get { return _FilterByCheatSheet; }
            set
            {
                _FilterByCheatSheet = value;
                NotifyPropertyChanged("FilterByCheatSheet");
                NotifyPropertyChanged("LoadedDataCVS");
            }
        }

        private string _loadPath = Properties.Settings.Default.LoadPath;
        public string LoadPath
        {
            get { return _loadPath; }
            set
            {
                _loadPath = value;
                NotifyPropertyChanged("LoadPath");

                Properties.Settings.Default.LoadPath = value;
                Properties.Settings.Default.Save();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                NotifyPropertyChanged("SearchText");

                dtSearchThrottle.Stop();

                dtSearchThrottle.Start();
            }
        }

        private PlistViewModel _SelectedPlist;
        public PlistViewModel SelectedPlist
        {
            get { return _SelectedPlist; }
            set
            {
                _SelectedPlist = value;
                NotifyPropertyChanged("SelectedPlist");

                if (value != null)
                {
                    textEditor.Text = value.Content;
                }
            }
        }

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                NotifyPropertyChanged("StatusText");
            }
        }
        #endregion

        private ObservableCollection<PlistViewModel> _loadedData = new ObservableCollection<PlistViewModel>();
        public ObservableCollection<PlistViewModel> LoadedData
        {
            get { return _loadedData; }
            set
            {
                _loadedData = value;
                NotifyPropertyChanged("LoadedData");
            }
        }

        
        

        public ICollectionView LoadedDataCVS
        {
            get
            {
                var cvs = new CollectionViewSource();
                cvs.Source = LoadedData;




                if (FilterByCheatSheet)
                {
                    var terms = SearchText?.Split(' ');
                    if (terms != null)
                    {
                        ActiveSearchTerms = terms.ToArray();
                    }

                    cvs.Filter += (o, e) =>
                    {
                        var item = e.Item as PlistViewModel;
                        e.Accepted = false;

                        e.Accepted = item.assocCheatSheet != null;

                        if (e.Accepted == true && terms != null)
                        {
                            e.Accepted = terms.All(x => item.Name.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Content.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.assocCheatSheet?.Description.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0);
                        }

                    };
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var terms = SearchText.Split(' ');
                        ActiveSearchTerms = terms.ToArray();

                        cvs.Filter += (o, e) =>
                        {
                            var item = e.Item as PlistViewModel;
                            e.Accepted = terms.All(x => item.Name.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Content.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0);
                        };
                    }
                }

                var filtertxt = ".";
                if (cvs.View.Cast<PlistViewModel>().Count() < LoadedData.Count)
                    filtertxt = " due to filters.";
                StatusText = $"Displaying {cvs.View.Cast<PlistViewModel>().Count()} of {LoadedData.Count} records{filtertxt}";

                return cvs.View;
            }
        }

        public void HandlePlists(string[] inputFiles)
        {
            var results = new Dictionary<FileInfo, Claunia.PropertyList.NSObject>();
            var errorCount = 0;
            foreach (var file in inputFiles)
            {
                try
                {
                    var plist = Claunia.PropertyList.PropertyListParser.Parse(file);
                    results.Add(new FileInfo(file), plist);
                }
                catch
                {
                    errorCount++;
                }
            }

            if (errorCount > 0) {
                MainWindowRef.ShowMessageAsync("Error FYI", $"A total of {errorCount} error(s) occurred while parsing items.");
            }

            var output = new List<PlistViewModel>();
            foreach (var item in results)
            {
                var nPlist = new PlistViewModel()
                {
                    Name = item.Key.Name,
                    Content = JsonConvert.SerializeObject(item.Value, Formatting.Indented),
                    sourceFile = item.Key,
                    assocCheatSheet = CheatSheetEntries.FirstOrDefault(x => x.File.Equals(item.Key.Name, StringComparison.CurrentCultureIgnoreCase)),
                };

                if (nPlist.assocCheatSheet == null)
                {
                    var regexList = CheatSheetEntries.Where(x => x.File.Contains("*"));
                    foreach (var rgpattern in regexList)
                    {
                        if (Regex.IsMatch(nPlist.Name, rgpattern.File.Replace("*", @"[\w\W]*?").Replace(".", @"\.")))
                        {
                            nPlist.assocCheatSheet = rgpattern;
                            break;
                        }
                    }
                }
                
                LoadedData.Add(nPlist);
            }


            // Just to update our counts
            NotifyPropertyChanged("LoadedFilesCVS");
            return;
        }

        #region Commanding

        private InlineCommand _loadDirectoryCommand;
        // Whatever dir the user inputted, scan for and load in plists.
        public InlineCommand loadDirectoryCommand
        {
            get
            {
                if (_loadDirectoryCommand == null)
                {
                    _loadDirectoryCommand = new InlineCommand((obj) =>
                    {
                        // ..
                        if (!Directory.Exists(LoadPath))
                        {
                            MainWindowRef.ShowMessageAsync("Error", "The specified path is not a folder.");
                            return;
                        }

                        HandlePlists(Directory.GetFiles(LoadPath, "*.plist"));
                    });
                }
                return _loadDirectoryCommand;
            }
        }

        private InlineCommand _loadSQLiteFileCommand;
        // In the original design we pulled in the info from an SQLite db generated out of the app. Now meh..
        public InlineCommand loadSQLiteFileCommand
        {
            get
            {
                if (_loadSQLiteFileCommand == null)
                {
                    _loadSQLiteFileCommand = new InlineCommand((obj) =>
                    {
                        // ..
                        if (!File.Exists(LoadPath))
                        {
                            MainWindowRef.ShowMessageAsync("Error", "That file does not exist.");
                            return;
                        }

                        using (var con = new SQLiteConnection($"Data Source={LoadPath}"))
                        {
                            con.Open();

                            var selCmd = new SQLiteCommand(@"SELECT name, content FROM plists", con);
                            using (var reader = selCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var nPlist = new PlistViewModel()
                                    {
                                        Name = reader["name"] as string,
                                        Content = reader["content"] as string,
                                        assocCheatSheet = CheatSheetEntries.FirstOrDefault(x => x.File.Equals(reader["name"] as string, StringComparison.CurrentCultureIgnoreCase)),
                                    };

                                    if (nPlist.assocCheatSheet == null)
                                    {
                                        var regexList = CheatSheetEntries.Where(x => x.File.Contains("*"));
                                        foreach (var rgpattern in regexList)
                                        {
                                            if (Regex.IsMatch(nPlist.Name, rgpattern.File.Replace("*", @"[\w\W]*?").Replace(".", @"\.")))
                                            {
                                                nPlist.assocCheatSheet = rgpattern;
                                                break;
                                            }
                                        }
                                    }
                                    LoadedData.Add(nPlist);
                                }
                                reader.Close();
                            }

                            con.Close();
                        }

                    });
                }
                return _loadSQLiteFileCommand;
            }
        }


        private InlineCommand _aboutCommand;
        // Display the about dialog
        public InlineCommand aboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                {
                    _aboutCommand = new InlineCommand((obj) =>
                    {
                        // ..
                        new AboutWindow().ShowDialog();

                    });
                }
                return _aboutCommand;
            }
        }

        #endregion

        #region Standard INotifyPropertyChanged Implementation

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };
        #endregion

    }
}
