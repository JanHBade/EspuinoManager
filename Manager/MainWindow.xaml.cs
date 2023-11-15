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
            FileInfo fi = new FileInfo(KonfigFile);
            if (!fi.Exists)
            {
                addLog("Keine Konfig vorhanden, erstelle leere");
                if (!Directory.Exists(fi.DirectoryName))
                    Directory.CreateDirectory(fi.DirectoryName);

                konfig = new Konfig();
            }
            else
            {
                addLog("Konfig vorhanden, lade von: " + KonfigFile);
                konfig = JsonSerializer.Deserialize<Konfig>(File.ReadAllText(KonfigFile));
            }

            client = new HttpClient();
            client.Timeout = konfig.getHtppTimeout();
        }

        public Konfig konfig { get; private set; }

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
                        konfig.Targets.Add(NewTarget);
                        saveKonfig();
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
                        try
                        {
                            string url = "http://" + Target + "/rfid";
                            addLog("Hole von: " + url);
                            string response = await client.GetStringAsync(url);
                            logger.Debug("Antwort: " + response);

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
                                addLog("Fehler: " + exp, NLog.LogLevel.Error);
                                exp = exp.InnerException;
                            } while (null != exp);
                        }
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


        private RelayCommand _WriteToEsp;
        public RelayCommand WriteToEsp
        {
            get
            {
                if (null == _WriteToEsp)
                {
                    _WriteToEsp = new RelayCommand(async p =>
                    {
                        logger.Debug(nameof(WriteToEsp) + " click");                        
                        try
                        {
                            string url = "http://" + Target + "/rfid";
                            addLog("Schreibe nach: " + url);

                            foreach (RfidEntry re in RfidEntries)
                            {
                                addLog("Schreibe " + re);
                                StringContent stringContent = new StringContent(JsonSerializer.Serialize(re), Encoding.UTF8, "application/json");
                                HttpResponseMessage httpResponseMessage = await client.PostAsync(url, stringContent);

                                logger.Debug(httpResponseMessage.ToString());
                            }
                            addLog("Schreiben fertig");
                        }
                        catch (Exception exp)
                        {
                            do
                            {
                                addLog("Fehler: " + exp, NLog.LogLevel.Error);
                                exp = exp.InnerException;
                            } while (null != exp);
                        }
                    }, p =>
                    {
                        //true = kann geklickt werden
                        return true;
                    }
                              );
                }
                return _WriteToEsp;
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
        
        private HttpClient client;

        private string _Log;
        public string Log
        {
            get { return _Log; }
            set { SetProperty(ref _Log, value); }
        }
        private void addLog(string s, NLog.LogLevel logLevel=null)
        {
            Log += "\n" + DateTime.Now + ": " + s;

            if (null == logLevel)
                logger.Info(s);
            else
                logger.Log(logLevel, s);
        }

        private void saveKonfig()
        {
            File.WriteAllText(KonfigFile,JsonSerializer.Serialize(konfig));
        }

        private readonly string KonfigFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\EspuinoManager\\Konfig.json";
    }

    public class Konfig : InpcBase
    {
        public Konfig()
        {
            Targets = new ObservableCollection<string>();
            HttpTimeout = 5000; //in ms
        }

        public ObservableCollection<string> Targets { get; set; }

        public int HttpTimeout { get; set; } //in ms

        public TimeSpan getHtppTimeout()
        {
            return new TimeSpan(0, 0, 0, 0, HttpTimeout);
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
                {new PlayMode{Value=1,Description="Einzelner Titel" } },
                {new PlayMode{Value=2,Description="Einzelner Titel (Endlosschleife)" } },
                {new PlayMode{Value=3,Description="Hörbuch" } },
                {new PlayMode{Value=4,Description="Hörbuch (Endlosschleife)" } },
                {new PlayMode{Value=5,Description="Alle Titel eines Verzeichnis (sortiert)" } },
                {new PlayMode{Value=6,Description="Alle Titel eines Verzeichnis (zufällig)" } },
                {new PlayMode{Value=7,Description="Alle Titel eines Verzeichnis (sortiert, Endlosschleife)" } },
                {new PlayMode{Value=8,Description="Webradio" } },
                {new PlayMode{Value=9,Description="Alle Titel eines Verzeichnis (zufällig, Endlosschleife)" } },
                {new PlayMode{Value=11,Description="Liste (Dateien von SD und/oder Webstreams) aus lokaler .m3u-Datei" } },
                {new PlayMode{Value=12,Description="Einzelner Titel eines Verzeichnis (zufällig). Danach schlafen." } },
                {new PlayMode{Value=13,Description="Alle Titel aus einem zufälligen Unterverzeichnis (sortiert)" } },
                {new PlayMode{Value=14,Description="Alle Titel aus einem zufälligen Unterverzeichnis (zufällig)" } },
            };
        }
        [JsonIgnore]
        public List<PlayMode> modes { get; private set; }

        public override string ToString()
        {
            return id + ": " + fileOrUrl;
        }

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

    public static class TextBoxUtilities
    {
        public static readonly DependencyProperty AlwaysScrollToEndProperty = DependencyProperty.RegisterAttached("AlwaysScrollToEnd",
                                                                                                                  typeof(bool),
                                                                                                                  typeof(TextBoxUtilities),
                                                                                                                  new PropertyMetadata(false, AlwaysScrollToEndChanged));

        private static void AlwaysScrollToEndChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                bool alwaysScrollToEnd = (e.NewValue != null) && (bool)e.NewValue;
                if (alwaysScrollToEnd)
                {
                    tb.ScrollToEnd();
                    tb.TextChanged += TextChanged;
                }
                else
                {
                    tb.TextChanged -= TextChanged;
                }
            }
            else
            {
                throw new InvalidOperationException("The attached AlwaysScrollToEnd property can only be applied to TextBox instances.");
            }
        }

        public static bool GetAlwaysScrollToEnd(TextBox textBox)
        {
            if (textBox == null)
            {
                throw new ArgumentNullException("textBox");
            }

            return (bool)textBox.GetValue(AlwaysScrollToEndProperty);
        }

        public static void SetAlwaysScrollToEnd(TextBox textBox, bool alwaysScrollToEnd)
        {
            if (textBox == null)
            {
                throw new ArgumentNullException("textBox");
            }

            textBox.SetValue(AlwaysScrollToEndProperty, alwaysScrollToEnd);
        }

        private static void TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }
    }
}
