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
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
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

            textEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit());
            textEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Yellow;
            textEditor.TextArea.SelectionChanged += textEditor_TextArea_SelectionChanged;

            void textEditor_TextArea_SelectionChanged(object sender, EventArgs e)
            {
                this.textEditor.TextArea.TextView.Redraw();
            }
        }

        public ViewModel viewModel { get; set; }

        private void Win1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                var fileList = e.Data.GetData("FileDrop") as string[];
                viewModel.HandlePlists(fileList);
            }
        }
    }

    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (ViewModel.ActiveSearchTerms == null || ViewModel.ActiveSearchTerms.Count() == 0)
            {
                return;
            }

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            foreach (var term in ViewModel.ActiveSearchTerms)
            {
                while ((index = text.IndexOf(term, start)) >= 0)
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + term.Length, // endOffset
                        (VisualLineElement element) =>
                        {
                        // This lambda gets called once for every VisualLineElement
                        // between the specified offsets.
                        Typeface tf = element.TextRunProperties.Typeface;
                        // Replace the typeface with a modified version of
                        // the same typeface
                        element.TextRunProperties.SetTypeface(new Typeface(
                                tf.FontFamily,
                                FontStyles.Italic,
                                FontWeights.Bold,
                                tf.Stretch
                            ));
                            element.TextRunProperties.SetFontRenderingEmSize(20.0);
                            element.TextRunProperties.SetBackgroundBrush(Brushes.LightGreen);
                        });
                    start = index + 1; // search for next occurrence
                }
            }
        }
    }

    public class CheatSheetEntry {
        public string Path { get; set; }
        public string File { get; set; }
        public string Description { get; set; }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            dtSearchThrottle.Tick += (o, e) =>
            {
                NotifyPropertyChanged("LoadedDataCVS");
                dtSearchThrottle.Stop();
            };

            using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MassPlistViewer.CheatSheet.json")))
            {
                var cheatsheet = sr.ReadToEnd();
                CheatSheetEntries = JsonConvert.DeserializeObject<CheatSheetEntry[]>(cheatsheet);
            }
        }

        static public CheatSheetEntry[] CheatSheetEntries { get; set; }

        //static public readonly string[] CheatSheetPlists = new string[] { "SuspendedState.plist", "com.apple.icloud.findmydeviced.FMIPAccounts.plist", "ClearedSections.plist", "significant.plist", "significantVisitAuth.plist", "significantVisitInterest.plist",
        //    "UserSettings.plist", "AccountInformation.plist", ".mboxCache.plist", "activeStateMachine.plist", "Bookmarks.plist", "com.apple.preferences.datetime.plist",
        //    "com.apple.madrid.plist", "com.apple.mobilenotes.plist", "com.apple.mobileslideshow.plist", "com.apple.MobileSMS.plist", "com.apple.preferences.network.plist", "com.apple.purplebuddy.plist",
        //    "com.apple.springboard.plist", "com.apple.weather.plist", "com.apple.WebFoundation.plist", "IconState.plist", "device_values.plist", "com.apple.assistant.backedup.plist", "com.apple.coreduetd.plist", "com.apple.homesharing.plist",
        //    "com.apple.NanoRegistry.plist", "com.apple.accounts.exists.plist", "com.apple.networkidentification.plist", "com.apple.wifi.plist", "preferences.plist", "com.apple.commcenter.plist",
        //    "CloudConfigurationDetails.plist", "com.apple.accountsettings.plist", "com.apple.commcenter.plist", "com.apple.conference.plist", "com.apple.identityservices.idstatuscache.plist",
        //    "com.apple.Maps.plist", "com.apple.mmcs.plist", "com.apple.MobileBluetooth.devices.plist", "info.plist", "manifest.plist", "status.plist", "user.plist", "addaily.plist",
        //    "com.apple.accounts.plist", "com.apple.ActivitySharing.plist", "com.apple.AdLib.plist", "com.apple.aggregated.plist", "com.apple.airplay.plist", "com.appleseed.FeedbackAssistant.plist", "com.apple.BatteryCenter.BatteryWidget.plist",
        //    "com.apple.camera.plist", "com.apple.carplay.plist", "com.apple.celestial.plist", "com.apple.cloudphotod.plist", "com.apple.cmfsyncagent.plist", "com.apple.commcenter.shared.plist",
        //    "com.apple.contextstored.plist", "com.apple.contacts.donation-agent.plist", "com.apple.CoreDuet.plist", "com.apple.CoreDuet.QueuedDenials.plist", "com.apple.corerecents.recentsd.plist",
        //    "com.apple.corespotlightui.plist", "com.apple.networkidentification.plist", "preferences.plist", "clients.plist"
        //};
        //static public readonly string[] RgxCheatSheet = new string[] { @"recoveryManager-[\w\W]*?\.plist", @"stateMachine-[\w\W]*?\.plist" };


        static public MetroWindow MainWindowRef;
        static public ICSharpCode.AvalonEdit.TextEditor textEditor;


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

        static public string[] ActiveSearchTerms { get; set; }

        DispatcherTimer dtSearchThrottle = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(300) };

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


        public void ApplySearchTextFilter(CollectionViewSource cvs)
        {
            var terms = SearchText.Split(' ');
            ActiveSearchTerms = terms.ToArray();

            cvs.Filter += (o, e) =>
            {
                var item = e.Item as PlistViewModel;
                e.Accepted = terms.All(x => item.Name.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Content.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0);
            };
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
                            e.Accepted = terms.All(x => item.Name.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Content.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0);
                        }

                    };
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        ApplySearchTextFilter(cvs);
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
            foreach (var file in inputFiles)
            {
                var plist = Claunia.PropertyList.PropertyListParser.Parse(file);
                results.Add(new FileInfo(file), plist);
            }

            var output = new List<PlistViewModel>();
            foreach (var item in results)
            {
                var nPlist = new PlistViewModel()
                {
                    Name = item.Key.Name,
                    Content = JsonConvert.SerializeObject(item.Value),
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

            return;
        }

        private InlineCommand _loadSQLiteFileCommand;
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
        

        #region Standard INotifyPropertyChanged Implementation

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };
        #endregion

    }
}
