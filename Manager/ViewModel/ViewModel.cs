
namespace Manager
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;

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
                konfig = Helper.readJson<Konfig>(KonfigFile);
            }

            espuino = new Espuino();

            client = new HttpClient();
            client.Timeout = konfig.getHtppTimeout();
        }

        public Konfig konfig { get; private set; }

        public Espuino espuino { get; private set; }

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
        public RelayCommand AddNewTarget
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
                    }, p =>
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

                            espuino.parseRfidEntrys(response);
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

                            /*foreach (RfidEntry re in RfidEntries)
                            {
                                addLog("Schreibe " + re);
                                StringContent stringContent = new StringContent(JsonSerializer.Serialize(re), Encoding.UTF8, "application/json");
                                HttpResponseMessage httpResponseMessage = await client.PostAsync(url, stringContent);

                                logger.Debug(httpResponseMessage.ToString());
                            }*/
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
                        if (true == openFileDialog.ShowDialog())
                        {
                            logger.Info("Lese aus " + openFileDialog.FileName);

                            espuino.parseRfidEntrys(File.ReadAllText(openFileDialog.FileName));
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

                            Helper.writeJson(saveFileDialog.FileName, espuino.RfidEntries.ToArray());
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
        private void addLog(string s, NLog.LogLevel logLevel = null)
        {
            Log += "\n" + DateTime.Now + ": " + s;

            if (null == logLevel)
                logger.Info(s);
            else
                logger.Log(logLevel, s);
        }

        private void saveKonfig()
        {
            Helper.writeJson(KonfigFile, konfig);
        }

        private readonly string KonfigFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\EspuinoManager\\Konfig.json";
    }
}
