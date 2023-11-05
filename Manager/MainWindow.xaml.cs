using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace Manager
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();

            logger.Info("StartUp");
        }
    }

    public class data : InpcBase
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public data()
        {
            Targets = new ObservableCollection<string>();
        }


        public ObservableCollection<string> Targets { get; private set; }

        public ObservableCollection<RfidEntry> RfidEntries { get; private set; }

        string _NewTarget;
        public string NewTarget
        {
            set
            {
                logger.Debug(nameof(NewTarget) + ": " + value);
                SetProperty(ref _NewTarget, value);
            }
            get
            {
                return _NewTarget;
            }
        }

        private string _Target;
        public string Target
        {
            get { return _Target; }
            set
            {
                logger.Debug(nameof(Target) + ": " + value);
                SetProperty(ref _Target, value);
            }
        }

        RelayCommand _AddNewTarget;
        public ICommand AddNewTarget
        {
            get
            {
                if (_AddNewTarget == null)
                {
                    _AddNewTarget = new RelayCommand(p =>
                    {
                        logger.Debug(nameof(AddNewTarget) + " click");
                        Targets.Add(NewTarget);
                        OnPropertyChanged(nameof(Targets));

                    },p =>
                    {
                        return !string.IsNullOrEmpty(NewTarget);
                    });
                }
                return _AddNewTarget;
            }
        }


        private RelayCommand _ReadFromEsp;
        public RelayCommand ReadFromEsp
        {
            get
            {
                if (null == _ReadFromEsp)
                {
                    _ReadFromEsp = new RelayCommand(async p =>
                    {
                        logger.Debug(nameof(ReadFromEsp) + " click");

                        await ReadFromEspTask();
                    }, p =>
                    {
                        //true = kann geklickt werden
                        return true;
                    }
                              );
                }
                return _ReadFromEsp;
            }
        }
        private async Task ReadFromEspTask()
        {
            HttpClient client = new HttpClient();
            try
            {
                string url = "http://" + Target + "/rfid";
                logger.Info("Hole von:" + url);
                string response = await client.GetStringAsync(url);
                logger.Debug("Antwort" + response);

                RfidEntries = new ObservableCollection<RfidEntry>(
                                JsonSerializer.Deserialize<List<RfidEntry>>(
                                response,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                }));
                OnPropertyChanged(nameof(RfidEntries));
            }
            catch (Exception exp)
            {
                do
                {
                    logger.Error(exp, "Fehler");
                    exp = exp.InnerException;
                } while (null != exp);
            }
        }



        private RelayCommand _ReadFromFile;
        public RelayCommand ReadFromFile
        {
            get
            {
                if (null == _ReadFromFile)
                {
                    _ReadFromFile = new RelayCommand(p =>
                    {
                        logger.Debug(nameof(ReadFromFile) + " click");
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        if(true == openFileDialog.ShowDialog())
                        {
                            logger.Info("Lese aus " + openFileDialog.FileName);
                            
                            RfidEntries = new ObservableCollection<RfidEntry>(
                                JsonSerializer.Deserialize<List<RfidEntry>>(
                                File.ReadAllText(openFileDialog.FileName),
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                }));
                            OnPropertyChanged(nameof(RfidEntries));
                        }
                        
                    }, p =>
                    {
                        //true = kann geklickt werden
                        return true;
                    }
                              );
                }
                return _ReadFromFile;
            }
        }

        private RelayCommand _WriteToFile;
        public RelayCommand WriteToFile
        {
            get
            {
                if (null == _WriteToFile)
                {
                    _WriteToFile = new RelayCommand(p =>
                    {
                        logger.Debug(nameof(WriteToFile) + " click");
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "JSON Datei|*.json";
                        if (true == saveFileDialog.ShowDialog())
                        {
                            logger.Info("Schreibe nach: " + saveFileDialog.FileName);

                            var options = new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };
                            File.WriteAllText(saveFileDialog.FileName, JsonSerializer.Serialize(RfidEntries.ToArray(),options));
                        }
                    }, p =>
                    {
                        //true = kann geklickt werden
                        return true;
                    }
                              );
                }
                return _WriteToFile;
            }
        }
    }

    public class RfidEntry : InpcBase
    {
        //https://github.com/biologist79/ESPuino/blob/master/html/locales/de.json#L97
        //"mode": {
        //            "1":"Einzelner Titel",
        //            "2":"Einzelner Titel (Endlosschleife)",
        //            "12":"Einzelner Titel eines Verzeichnis (zufällig). Danach schlafen.",
        //            "3":"Hörbuch",
        //            "4":"Hörbuch (Endlosschleife)",
        //            "5":"Alle Titel eines Verzeichnis (sortiert)",
        //            "6":"Alle Titel eines Verzeichnis (zufällig)",
        //            "7":"Alle Titel eines Verzeichnis (sortiert, Endlosschleife)",
        //            "9":"Alle Titel eines Verzeichnis (zufällig, Endlosschleife)",
        //            "13":"Alle Titel aus einem zufälligen Unterverzeichnis (sortiert)",
        //            "14":"Alle Titel aus einem zufälligen Unterverzeichnis (zufällig)",
        //            "8":"Webradio",
        //            "11":"Liste (Dateien von SD und/oder Webstreams) aus lokaler .m3u-Datei"
        //        },
        public RfidEntry()
        {
            modes = new List<PlayMode>()
            {
                {new PlayMode{Value=0,Description="-" } },
                {new PlayMode{Value=3,Description="Hörbuch" } },
                {new PlayMode{Value=5,Description="Alle Titel eines Verzeichnis (sortiert)" } },
                {new PlayMode{Value=8,Description="Webradio" } }
            };
        }
        [JsonIgnore]
        public List<PlayMode> modes { get; private set; }


        private string _id;
        public string id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _fileOrUrl;
        public string fileOrUrl
        {
            get { return _fileOrUrl; }
            set { SetProperty(ref _fileOrUrl, value); }
        }

        private int _playMode;
        public int playMode
        {
            get { return _playMode; }
            set
            {   
                _playMode = value;
                OnPropertyChanged();
                
                OnPropertyChanged(nameof(playModeObj));
            }
        }
        [JsonIgnore]
        public PlayMode playModeObj
        {
            get
            {
                return modes.FirstOrDefault( pm => pm.Value ==  playMode);
            }
            set
            {
                if (null != value) playMode = value.Value;
            }
        }

        private int _lastPlayPos;
        public int lastPlayPos
        {
            get { return _lastPlayPos; }
            set { SetProperty(ref _lastPlayPos, value); }
        }


        private int _trackLastPlayed;
        public int trackLastPlayed
        {
            get { return _trackLastPlayed; }
            set { SetProperty(ref _trackLastPlayed, value); }
        }
    }

    public class PlayMode : InpcBase
    {

        private int _Value;
        public int Value
        {
            get { return _Value; }
            set { SetProperty(ref _Value, value); }
        }

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }
        
        public override string ToString()
        {
            return $"{Description} ({Value})";
        }
    }

    /// <summary>Basis-Klasse, die INotifyPropertyChanged 
    /// Basis-Funktionalität zur Verfügung stellt.</summary>
    public class InpcBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        #region Fields

        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        #endregion // Fields

        #region Constructors
        public RelayCommand(Action<object> execute)
        : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }
        #endregion // Constructors

        #region ICommand Members
        [System.Diagnostics.DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        #endregion // ICommand Members
    }
}
