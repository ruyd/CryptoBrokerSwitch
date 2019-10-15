using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Piggy;

namespace PigSwitch
{
    public class UserServerModel
    {
        public Guid ID { get; set; }

        private readonly ConcurrentDictionary<string, Tuple<string, HMACSHA256>> _Encryptors = new ConcurrentDictionary<string, Tuple<string, HMACSHA256>>();
        public string Email { get; set; }
        public string OneSignalID { get; set; }
        public string Sign(string exchange, string uri, long expires)
        {
            Tuple<string, HMACSHA256> encryptor;
            if (_Encryptors.TryGetValue(exchange, out encryptor))
            {
                return ByteToString(encryptor.Item2.ComputeHash(Encoding.UTF8.GetBytes($"GET/realtime{expires}")));
            }
            return null;
        }
        //public string KeyId => _Encryptors.FirstOrDefault(a => a.Key == (LiveTurnedOn ? "Live" : "Test")).Value?.Item1;
        public string KeyId { get; set; }
        public string KeySecret { get; set; }//remove! 

        private BrokerPreference _Preferences;
        public BrokerPreference Preferences {
            get { if (_Preferences == null) _Preferences = new BrokerPreference(); return _Preferences; }
            set { _Preferences = value; }
        }
        public BitmexWalletSummary WalletBalance { get; set; }
        //Concurrent Collections Really Suck, dict not ordered and stack removing sucks
        //public readonly ConcurrentDictionary<Guid, BrokerStrategiesTrade> TradeBuffer = new ConcurrentDictionary<Guid, BrokerStrategiesTrade>();
        public readonly ConcurrentList<BrokerStrategiesTrade> TradeBuffer = new ConcurrentList<BrokerStrategiesTrade>();

        public BrokerStrategiesTrade OpenPositionXbt => TradeBuffer.LastOrDefault(a => a.StatusID == 1 && a.Symbol == "XBTUSD");
        public BrokerStrategiesTrade OpenPositionAda => TradeBuffer.LastOrDefault(a => a.StatusID == 1 && a.Symbol == "XBTADA");
        public BrokerStrategiesTrade OpenPositionBch => TradeBuffer.LastOrDefault(a => a.StatusID == 1 && a.Symbol == "XBTBCH");

        public bool LiveTurnedOn { get; set; }
        private List<BitmexPosition> _Positions;
        public List<BitmexPosition> Positions
        {
            get { if (_Positions == null) _Positions = new List<BitmexPosition>(); return _Positions; }
            set { _Positions = value; }
        }
        public string SignalR { get; set; }
        public bool PrivateConnected { get; set; }
        public DateTimeOffset? LastFilledOn { get; set; }

        //temporary bool, should be enum, id or string for future exchanges if bitmex is not enoght =) 
        public string GetSig(long expires)
        {
            Tuple<string, HMACSHA256> encryptor;
            if (_Encryptors.TryGetValue(LiveTurnedOn ? "Live" : "Test", out encryptor))
            {
                return ByteToString(encryptor.Item2.ComputeHash(Encoding.UTF8.GetBytes($"GET/realtime{expires}")));
            }
            return null;
        }

        private string ByteToString(byte[] buff)
        {
            var sbinary = "";
            foreach (var t in buff)
                sbinary += t.ToString("X2"); /* hex format */
            return sbinary;
        }

        public void AddKey(string exchange, string id, string secret)
        {
            _Encryptors.TryAdd(exchange, new Tuple<string, HMACSHA256>(id, new HMACSHA256(Encoding.ASCII.GetBytes(secret))));
        }

        public static UserServerModel FromUser(BrokerUser user, BrokerPreference prefs)
        {
            var m = new UserServerModel();
            m.ID = user.ID;
            m.SignalR = user.SignalR;
            m.OneSignalID = user.OneSignalID;
            m.Email = user.Email;
            m.Preferences = prefs;
            m.LiveTurnedOn = user.LiveTurnedOn == true;
            m.KeyId = m.LiveTurnedOn ? user.LiveID : user.TestID;
            m.KeySecret = m.LiveTurnedOn ? user.LiveKey : user.TestKey;

            if (!string.IsNullOrWhiteSpace(user.LiveKey))
            {
                m.AddKey("Live", user.LiveID, user.LiveKey);
            }

            if (!string.IsNullOrWhiteSpace(user.TestKey))
            {
                m.AddKey("Test", user.TestID, user.TestKey);
            }
            return m;
        }
    }

}