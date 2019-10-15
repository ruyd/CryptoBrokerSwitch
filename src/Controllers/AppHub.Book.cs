using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PigSwitch.Hubs 
{
    public partial class AppHub
    {

        //public List<BitMexInstrument> DownloadInstrumentList(string state)
        //{
        //    List<BitMexInstrument> instruments = new List<BitMexInstrument>();
        //    try
        //    {
        //        WebClient client = new WebClient();
        //        string json = client.DownloadString("https://www.bitmex.com/api/v1/instrument");

        //        if (json == null)
        //        {
        //            //ErrorFx.Log("BitMexHandler - instrument download failed");
        //        }
        //        else
        //        {
        //            instruments = json.Deserialize<List<BitMexInstrument>>();
        //            instruments = instruments.Where(s => state == null || state.Equals(s.state)).OrderBy(s => s.state).ThenBy(s => s.rootSymbol).ThenByDescending(s => s.expiry).ToList();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //ErrorFx.Log("BitMexHandler instrument download exception {0}", ex.Message);
        //    }

        //    return instruments;
        //}


        //public List<BitMexIndex> DownloadIndex(string symbol, int count, DateTime start, DateTime end)
        //{
        //    List<BitMexIndex> index = new List<BitMexIndex>();
        //    try
        //    {
        //        WebClient client = new WebClient();
        //        client.QueryString.Add("symbol", symbol);
        //        if (count >= 0)
        //            client.QueryString.Add("count", count.ToString());
        //        if (start != default(DateTime))
        //            client.QueryString.Add("startTime", start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        //        if (end != default(DateTime) && end >= start && end.Year > 2000)    //excel can pass 1899 as a default date, so validate > y2k
        //            client.QueryString.Add("endTime", end.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

        //        string json = client.DownloadString("https://www.bitmex.com/api/v1/trade");

        //        if (json == null)
        //        {
        //            //ErrorFx.Log("BitMexHandler - instrument download failed");
        //        }
        //        else
        //        {
        //            index = json.Deserialize<List<BitMexIndex>>();
        //            index = index.OrderByDescending(s => s.timestamp).ToList();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //ErrorFx.Log("BitMexHandler index download exception {0}", ex.Message);
        //    }

        //    return index;
        //}


        //private void OnDepthUpdate(object sender, MarketDataSnapshot snap)
        //{
        //    //update data cache
        //    MarketData[snap.Product] = snap;

        //    //no current subscription for this product
        //    if (!_topics.ContainsKey(snap.Product))
        //        return;

        //    // Dictionary<Tuple<DataPoint, int>, string> productTopics = _topics[snap.Product].TopicItems;

        //    //iterate down the depth on the bid side, updating any currently subscribed topics
        //    for (int level = 0; level < snap.BidDepth.Count; level++)
        //    {
        //        //update bid
        //        Tuple<DataPoint, int> key = Tuple.Create(DataPoint.Bid, level);
        //        //if (productTopics.ContainsKey(key))
        //        //    productTopics[key].UpdateValue(snap.BidDepth[level].Price);

        //        ////update bidvol
        //        //key = Tuple.Create(DataPoint.BidVol, level);
        //        //if (productTopics.ContainsKey(key))
        //        //    productTopics[key].UpdateValue(snap.BidDepth[level].Qty);
        //    }

        //    //iterate down the depth on the ask side, updating any currently subscribed topics
        //    for (int level = 0; level < snap.AskDepth.Count; level++)
        //    {
        //        //update ask
        //        Tuple<DataPoint, int> key = Tuple.Create(DataPoint.Ask, level);
        //        //if (productTopics.ContainsKey(key))
        //        //    productTopics[key].UpdateValue(snap.AskDepth[level].Price);

        //        ////update askvol
        //        //key = Tuple.Create(DataPoint.AskVol, level);
        //        //if (productTopics.ContainsKey(key))
        //        //    productTopics[key].UpdateValue(snap.AskDepth[level].Qty);
        //    }
        //}


        //private void OnTradeUpdate(object sender, BitMexTrade trade)
        //{
        //    //update data cache
        //    //if (!Instruments.ContainsKey(trade.symbol))
        //    //{
        //    //    //ErrorFx.Log($"Market trade, but instrument not yet downloaded {trade.symbol} {trade.price}");
        //    //}
        //    //else
        //    //{
        //    //    //ErrorFx.Log($"Updating last price {trade.symbol} {trade.price}");
        //    //    Instruments[trade.symbol].lastPrice = trade.price;
        //    //}

        //    //no current subscription for this product
        //    if (!_topics.ContainsKey(trade.symbol))
        //        return;

        //    //Dictionary<Tuple<DataPoint, int>, Topic> productTopics = _topics[trade.symbol].TopicItems;

        //    ////update any last price/size subscriptions
        //    //Tuple<DataPoint, int> key = Tuple.Create(DataPoint.Last, 0);
        //    //if (productTopics.ContainsKey(key))
        //    //    productTopics[key].UpdateValue(trade.price);

        //}

        //private Dictionary<string, TopicCollection> _topics;  //one topic corresponds to one unique item of data in Excel (product, datapoint, depth)
        //private Dictionary<int, TopicSubscriptionDetails> _topicDetails;

        //public enum DataPoint
        //{
        //    Bid,
        //    Ask,
        //    BidVol,
        //    AskVol,
        //    Last
        //}

        //internal class TopicCollection
        //{
        //    // internal Dictionary<Tuple<DataPoint, int>, ExcelDna.Integration.Rtd.ExcelRtdServer.Topic> TopicItems = new Dictionary<Tuple<DataPoint, int>, ExcelRtdServer.Topic>();
        //}

        //internal class TopicSubscriptionDetails
        //{
        //    internal string Product { get; private set; }
        //    internal DataPoint DataPoint { get; private set; }
        //    internal int Level { get; private set; }

        //    internal TopicSubscriptionDetails(string product, DataPoint dataPoint, int level)
        //    {
        //        Product = product;
        //        DataPoint = dataPoint;
        //        Level = level;
        //    }

        //    public override string ToString()
        //    {
        //        return Product + "|" + DataPoint + "|" + Level;
        //    }

        //}

    }
}