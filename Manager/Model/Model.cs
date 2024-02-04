

namespace Manager
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class Espuino : InpcBase
    {
        private ObservableCollection<RfidEntry> _RfidEntries;
        public ObservableCollection<RfidEntry> RfidEntries
        {
            get { return _RfidEntries; }
            set
            {
                _RfidEntries = value;
                OnPropertyChanged();
            }
        }

        public void parseRfidEntrys(string content, bool Add=false)
        {
            if (!Add)
            {
                RfidEntries = new ObservableCollection<RfidEntry>(
                                                JsonSerializer.Deserialize<List<RfidEntry>>(
                                                content,
                                                new JsonSerializerOptions
                                                {
                                                    PropertyNameCaseInsensitive = true,
                                                }));
            }
            else
            {
                ObservableCollection<RfidEntry> NewEntries = new ObservableCollection<RfidEntry>(
                                                                JsonSerializer.Deserialize<List<RfidEntry>>(
                                                                content,
                                                                new JsonSerializerOptions
                                                                {
                                                                    PropertyNameCaseInsensitive = true,
                                                                }));
                foreach (RfidEntry re in NewEntries)
                    RfidEntries.Add(re);
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
                return modes.FirstOrDefault(pm => pm.Value == playMode);
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
}
