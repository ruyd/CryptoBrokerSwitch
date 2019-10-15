using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitmex;

namespace Piggy
{
    public class PiggyBroker
    {
        /// <summary>
        /// phased out
        /// </summary>
        /// <param name="post"></param>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        public static async Task<BrokerStrategiesTrade> BrokerOrderOLD(NewOrderPost post, string sourceName)
        {
            using (BrokerDatabase context = new BrokerDatabase())
            {
                try
                {
                    ErrorFx.V("POST::NEW ORDER", 1, post.Serialize());

                    //Get User 
                    Guid? userId = post.ID;
                    BrokerUser user = await context.BrokerUsers.FirstAsync(a => a.ID == userId);
                    string key = user.LiveTurnedOn == true ? user.LiveKey : user.TestKey;
                    string id = user.LiveTurnedOn == true ? user.LiveID : user.TestID;
                    //... and current preferences 
                    BrokerPreference prefs =
                        await context.BrokerPreferences.FirstOrDefaultAsync(a => a.FK_UserID == userId);

                    string rawSym = post.Data.symbol;
                    string[] sym = rawSym.Split(':');
                    string exchange = sym.FirstOrDefault();
                    string symbol = sym.LastOrDefault();

                    string strategyName = post.Data.strategyName;

                    string strategyTvId = post.Data.strategyId;

                    bool skipExiting = false;
                    BrokerStrategy strategy = await context.BrokerStrategies
                        .Where(a => a.FK_UserID == userId && a.StrategyId == strategyTvId).FirstOrDefaultAsync();
                    if (strategy == null)
                    {
                        strategy = post.ToStrategy();
                        context.BrokerStrategies.Add(strategy);
                        await context.SaveChangesAsync();
                        skipExiting = true;
                    }

                    List<BrokerStrategiesTrade> existingList = skipExiting
                        ? new List<BrokerStrategiesTrade>()
                        : await context.BrokerStrategyTrades.Where(a =>
                                a.FK_UserID == userId && a.FK_StrategyID == strategy.ID)
                            .ToListAsync();

                    BrokerStrategiesTrade lastTrade = existingList.LastOrDefault();


                    StringBuilder nofyText = new StringBuilder();
                    if (post.Data.orders?.Count > 0)
                    {
                        //log.Info($"New Live Trade::{post.Data.strategyName} {post.Data.symbol}");

                        //previous trade open 
                        //previouss

                        RawOrder prevTradeEntry = post.Data.orders?[2];
                        RawOrder prevTradeExit = post.Data.orders?[1];
                        RawOrder newTradeEntry = post.Data.orders?[0];

                        //missed trade alert here 
                        int? tradeDiff = (lastTrade?.TradeNum - prevTradeExit?.TradeNum) * 1;
                        if (tradeDiff > 1)
                        {
                            string s = $"{(newTradeEntry.Signal == lastTrade.EntrySignal ? "Missed Side!" : "Sequence Broken")}: New #:{newTradeEntry.TradeNum} {newTradeEntry.Signal} Prev#:{lastTrade.TradeNum} {lastTrade.EntrySignal}";
                            ErrorFx.V(s);
                            OinkNotifications.SendPushNofy(s);
                        }

                        nofyText.Append($"{post.Data.strategyName}:{symbol} | ");

                        BrokerStrategiesTrade newTrade = post.ToTrade(newTradeEntry);
                        newTrade.FK_UserID = post.ID;
                        newTrade.FK_StrategyID = user.LastStrategyID;
                        newTrade.FK_RunID = user.LastRunID;
                        newTrade.SourceName = sourceName;

                        newTrade.TradeNum = newTradeEntry.TradeNum;

                        //SUPER IMPORTANT - to Trade attaches Strategy Quantity 
                        newTrade.AskQuantity = prefs?.Quantity ?? 0; //que explote si no hay 
                        newTrade.AskType = prefs?.EnableMarket == true ? "Market" : "Limit";

                        context.BrokerStrategyTrades.Add(newTrade);

                        // ***************************
                        // BEGIN SEND 

                        OrderResponse resp = null;

                        BrokerRequest br = new BrokerRequest();

                        br.UniqueID = newTrade.UniqueID;
                        br.ClientId = id;
                        br.ClientKey = key;
                        br.Symbol = symbol;
                        br.Price = newTrade.AskPrice;
                        br.Quantity = newTrade.AskQuantity;
                        br.Side = newTrade.EntrySide;
                        br.Comment = $"{newTrade.TradeNum}";
                        br.LiveNet = user.LiveTurnedOn == true;
                        br.GroupById = symbol;
                        br.ExchangeId = GetExchangeId(exchange);
                        br.OrderType = prefs?.EnableMarket == true ? "Market" : "Limit";
                        br.TryLogic = prefs?.LimitOptionId;

                        //Toggle Off 
                        string blockError = $"{newTrade.EntrySide} Off";
                        bool allowSendToBroker = newTrade.EntryLong && prefs?.EnableLong == true
                                                 || newTrade.EntryShort && prefs?.EnableShort == true;

                        //Exchange not ready block
                        if (br.ExchangeId != 1 && br.ExchangeId != 2)
                        {
                            allowSendToBroker = false;
                            blockError = $"{exchange} Off";
                        }

                        /////////////////////////////// 
                        //Get Positions!                     
                        //TODO: Rework to websocket, avoid this hit 
                        List<BitmexPosition> positions = await GetPositionsAsync(br);
                        BitmexPosition position = positions?.FirstOrDefault();
                        if (position != null)
                        {
                            br.HasOpenPosition = position.isOpen == true;

                            //ErrorFx.Log($"Position: {position.isOpen} {position.currentQty} | {position.unrealisedRoePcnt.toString("p")} B/E: {position.breakEvenPrice} Entry: {position.avgEntryPrice.toString()}", $"Ask: {newTrade.AskPrice} M:{position.markPrice.toString()} L:{position.lastPrice.toString()} Prev: {position.prevClosePrice}", positions.Serialize());
                            ErrorFx.V($"POSITION {position.isOpen}: {position.Side} ({position.currentQty}) Entry: {newTrade.EntryPrice} B/E: {position.breakEvenPrice} DIFF: {newTrade.EntryPrice - position.breakEvenPrice}", 1, position.Serialize());

                            //LUBE PRICE 
                            //position.markPrice
                            //position.avgEntryPrice
                            //position.lastPrice
                            //position.prevClosePrice

                            if (position.markPrice > 0)
                            {
                                //SIDE
                                decimal? askDiff = position.markPrice - br.Price;
                                bool bearishMomemtum = position.prevClosePrice > position.markPrice;
                                decimal? prevDiff = position.markPrice - position.prevClosePrice;

                                //Market Dropped or Popped suddenly and this will correct 
                                //or we could miss out which is fine 

                                //position.prevClosePrice

                                decimal? suggested = 0m;
                                if (br.Side == "Buy")
                                {
                                    //Ask: 8191.5 M: 8,195.2 L: 8,195.2 AVG: Prev: 8211.97
                                    //Sug: 8194.71 AskDiff: 3.71 Bearish: True PrevDiff: -16.76

                                    suggested = position.markPrice + (askDiff > 0 && askDiff < 1 ? 0.85m : (askDiff > 1 ? 2 : 0));
                                }
                                else
                                {
                                    suggested = position.markPrice + (askDiff < 0 && askDiff > -1 ? -0.75m : (askDiff > -1 ? -2 : 0));
                                }

                                suggested = Convert.ToDecimal(Math.Round((double)(suggested * 2)) / 2);

                                //if below zero the diff best to leave as is, might be a drop, good to miss                                 
                                ErrorFx.V($"Ask: {br.Price} Mark: {position.markPrice} Sug: {suggested} AskDiff: {askDiff} Bearish: {bearishMomemtum} PrevDiff: {prevDiff} ");
                                //br.Price = suggested;
                            }

                            if (position.isOpen == true)
                            {
                                //Profitable, if losing we can reduce quantity and do some adjustments 
                                if (position.unrealisedPnlPcnt > 0)
                                {
                                }

                                if (position.currentQty != null)
                                {
                                    decimal? positiveQuantity = position.currentQty >= 0
                                        ? position.currentQty
                                        : position.currentQty * -1;

                                    if (position.Side == br.Side)
                                    {
                                        if (prefs.EnableSame != true)
                                        {
                                            ErrorFx.Log("Same Side - Blocked");
                                            allowSendToBroker = false;
                                            blockError = "Same Side";
                                        }
                                        else
                                        {
                                            if (positiveQuantity + newTrade.AskQuantity >= prefs.MaxSame)
                                            {
                                                allowSendToBroker = false;
                                                blockError = "Max Qty";
                                            }
                                        }
                                    }

                                    if (allowSendToBroker && position.Side != br.Side)
                                    {

                                        BrokerRequest cr = br.New();
                                        cr.IsClose = true;
                                        cr.TimeInForce = "FillOrKill";
                                        cr.Comment = $"{lastTrade?.TradeNum} Take Profit {position.unrealisedPnlPcnt}";
                                        cr.Quantity = positiveQuantity; // orderQty is not specified, a 'Close' order has an orderQty equal to your current position's size

                                        OrderResponse close = await SendBitmexAsync(cr);
                                        ErrorFx.V($"Limit Close: {close.ordStatus ?? close.ordRejReason}", 1, close.Serialize());

                                        if (!close.Success)
                                        {
                                            cr.OrderType = "Market";
                                            cr.TimeInForce = null;
                                            cr.Comment = $"{lastTrade?.TradeNum} Take Profit Market {position.unrealisedPnlPcnt}";
                                            cr.Quantity = positiveQuantity;
                                            close = await SendBitmexAsync(cr);
                                            ErrorFx.V($"Market Close: {close.ordStatus ?? close.ordRejReason}", 1, close.Serialize());
                                        }

                                        if (lastTrade != null)
                                        {
                                            lastTrade.ExitPrice = close.price;
                                        }

                                        //rework 
                                        //string nofy = $"{lastTrade?.TradeNum}#) CLOSE: {close.ordType} {close.ordStatus} | {lastTrade?.TradeNum}#) {lastTrade?.ExitProfitPercentCalculated.toString()}% PNL: {position.unrealisedPnl} ";
                                       // ErrorFx.V(nofy);
                                       // OinkNotifications.SendPushNofy(nofy);
                                    }
                                }
                            }
                        }


                        //Toggle Off block 
                        resp = allowSendToBroker
                            ? await SendBitmexAsync(br)
                            : OrderResponse.Error(blockError, 5);

                        newTrade.EntryMessage = resp.ordStatus;
                        newTrade.Success = resp.Success;
                        newTrade.RequestCode = br.Serialize().Clean(1500);
                        newTrade.ResponseCode = resp.Serialize().Clean(1500);

                        if (resp.Success)
                        {
                            newTrade.EntryPrice = resp.price;
                            newTrade.EntryQuantity = resp.orderQty;
                            newTrade.EntryType = resp.ordType;
                            newTrade.EntryTime = DateTime.UtcNow;

                            newTrade.ExchangeOrderID = resp.orderID;

                    

                            //ordStatus - Filled, New 
                            if (resp.ordStatus == "Filled")
                            {
                                newTrade.StatusID = 1;
                            }
                            else if (resp.ordStatus == "New")
                            {
                                newTrade.StatusID = 0;
                            }
                            else if (resp.ordStatus == "Cancelled")
                            {
                                newTrade.StatusID = 86;
                            }
                            else if (resp.ordStatus == "PartiallyFilled")
                            {
                                newTrade.StatusID = 11;
                            }
                            else
                            {
                                newTrade.StatusID = 9;
                                ErrorFx.Log("Switch: Status not coded...", resp.ordStatus, resp.Serialize());
                            }

                            string typeInitial = newTrade.ExchangeOrderID != null && newTrade.EntryType != null ? " (" + newTrade.EntryType.Substring(0, 1) + ")" : null;
                            nofyText.Append(
                                $"{newTrade.TradeNum}# New {newTrade.EntrySignal?.ToUpper()}: {newTrade.EntryQuantity}@{newTrade.EntryPrice} Slip: {newTrade.EntrySplippage?.ToString("p")} S: {newTrade.EntryMessage ?? newTrade.ExchangeError}{typeInitial} {lastTrade?.ExitProfitPercent?.ToString("n2")}%");


                            //await context.SaveChangesAsync();//MID-SAVE REQUIRED FOR WEBSOCKET COLLISSION PREVENTION

                            ////// NOTIFICATIONS

                            //Filled has an internal nofy, this will create a dupe alert 
                            //TODO: Consolidate alerts, a bigger more comprehensive                                                         
                            if (prefs?.EnablePush == true)
                            {
                                OinkNotifications.SendPushNofy(nofyText.ToString());
                            }

                        }
                        else
                        {
                            //gotta be a few responses 
                            newTrade.ExchangeError = resp.error?.message;
                            newTrade.StatusID = resp.error?.code ?? 500;

                            //if (!string.IsNullOrWhiteSpace(resp.ordRejReason))
                            //    newTrade.EntryMessage = resp.ordRejReason;

                            //If we are allowed but got an error then...
                            if (allowSendToBroker)
                            {
                                OinkNotifications.SendPushNofy($"Error: {newTrade.EntryMessage} {resp.error?.message}");
                            }

                            ErrorFx.Log($"{sourceName}: Order Failed", resp.error?.Serialize(), resp.Serialize());
                        }

                        //Quick Hacks - bad string methods =( memory mess
                        newTrade.Sanitize();

                        await context.SaveChangesAsync();

                        return newTrade;
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException e)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (System.Data.Entity.Validation.DbEntityValidationResult eve in e.EntityValidationErrors)
                    {
                        sb.Append($"Error: {eve.Entry.Entity.GetType().Name} s: {eve.Entry.State} ");
                        foreach (System.Data.Entity.Validation.DbValidationError ve in eve.ValidationErrors)
                        {
                            sb.Append($"- Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    e.SaveToDB(sb.ToString());
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    ex.InnerException?.SaveToDB();
                }
                catch (Exception ex)
                {
                    ex.SaveToDB();
                    ex.InnerException?.SaveToDB();
                    Console.WriteLine("BrokerEx::", ex);
                }
                return null;
            }
        }

        public static async Task<List<BitmexPosition>> GetPositionsAsync(BrokerRequest br)
        {
            BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);
            ErrorFx.V($"Getting position for {br.ClientId} live:{br.LiveNet}", 3);

            var response = await client.GetPositionsAsync();

            if (response.Overloaded)
            {
                ErrorFx.V($"Position Overloaded::Retry 1", 0);
                await Task.Delay(200);
                response = await client.GetPositionsAsync();
            }

            if (response.Overloaded)
            {
                ErrorFx.V($"Position Overloaded::Retry 2", 0);
                await Task.Delay(500);
                response = await client.GetPositionsAsync();
            }

            if (response.Result != null)
            {
                foreach (var item in response.Result)
                {
                    item.timestamp = DateTime.UtcNow;
                }
            }

            return response.Result;
        }
        public static async Task<List<OrderResponse>> GetOpenOrdersAsync(BrokerRequest br)
        {
            BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);
            ErrorFx.V($"Getting Orders for {br.ClientId} live:{br.LiveNet}", 3);
            return await client.GetOrdersAsync(br.Symbol, true);
        }
        

        public static void GelPrice(BrokerRequest br, BitmexPosition position)
        {
            //LUBE PRICE 
            //position.markPrice
            //position.avgEntryPrice
            //position.lastPrice
            //position.prevClosePrice

            decimal? suggested = 0m;

            //if (position?.markPrice > 0)
            //{
            //    //SIDE
            //    decimal? askDiff = position.markPrice - br.Price;
            //    bool bearishMomemtum = position.prevClosePrice > position.markPrice;
            //    decimal? prevDiff = position.markPrice - position.prevClosePrice;

            //    if (br.Side == "Buy")
            //    {
            //        //Ask: 8191.5 M: 8,195.2 L: 8,195.2 AVG: Prev: 8211.97
            //        //Sug: 8194.71 AskDiff: 3.71 Bearish: True PrevDiff: -16.76

            //        suggested = position.markPrice + (askDiff > 0 && askDiff < 1 ? 0.85m : (askDiff > 1 ? 2 : 0));
            //    }
            //    else
            //    {
            //        suggested = position.markPrice + (askDiff < 0 && askDiff > -1 ? -0.75m : (askDiff > -1 ? -2 : 0));
            //    }

            //    //Market Dropped or Popped suddenly and this will correct 
            //    //or we could miss out which is fine 

            //    //position.prevClosePrice              
            //    ErrorFx.V($"Greed Gel:: Ask: {br.Price} Mark: {position.markPrice} Sug: {suggested} AskDiff: {askDiff} Bearish: {bearishMomemtum} PrevDiff: {prevDiff} ", 1);
            //}
            //else
            //{
                suggested = br.Price + (br.Side == "Buy" ? 1 : -1);
                ErrorFx.V($"Greed Gel:: Ask: {br.Price} Sug: {suggested} ", 1);
            //}
         
            suggested = Convert.ToDecimal(Math.Round((double)(suggested * 2)) / 2);
                        
            br.Price = suggested;
        }

        


        /// <summary>
        /// Looking like a bitmex specific method... what about Binance
        /// - TODO: Bitmex has a nice html timeout page from cloudflare, check for deserialize error 
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public static async Task<OrderResponse> SendBitmexAsync(BrokerRequest br)
        {
            ErrorFx.V($"To Bitmex => {br.OrderType} {br.Side} {br.Quantity} {br.Price}", 1, br.Serialize());

            BitmexHttpClient client = new BitmexHttpClient(br.ClientId, br.ClientKey, br.LiveNet);
            decimal? moreFavorablePrice = br.Side == "Buy" ? br.Price - 1 : br.Price + 1;
            OrderResponse resp = new OrderResponse();
            try
            {
                //**********************************************************************
                // BROKER 0.1 alpha =) CONSOLIDATE A LITTLE MORE DUDE 

                if (br.IsKeyTest)
                {
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V("<= Bitmex::KeyTest", 3, resp.Serialize());
                    return resp;
                }

                if (br.IsClose)
                {
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"=> Bitmex::Close::{br.OrderType} {resp.side} S:{resp.ordStatus} Q:{resp.orderQty} P:{resp.price} RR:{resp.ordRejReason}", 3, resp.Serialize());
                    return resp;
                }

                //OrderType != Limit
                if (br.IsOneShot)
                {
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"=> Bitmex::Response:{br.OrderType} {resp.side} S:{resp.ordStatus} Q:{resp.orderQty} P:{resp.price} RR:{resp.ordRejReason}", 3, resp.Serialize());
                    return resp;
                }

                //LIMIT ORDERING 

                if (br.TryLogic == null)
                {
                    br.TryLogic = "lo";
                }

                //leave open, dont use OCO
                if (br.TryLogic == "lo")
                {
                    br.GroupById = null;
                }

                if (br.TryLogic == "kill")
                {
                    ErrorFx.V("FillOrKill");
                    br.TimeInForce = "FillOrKill";
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"OCO::Response: {resp.ordStatus} Qty:{resp.orderQty} U:{resp.leavesQty} {resp.ordRejReason} {resp.error?.message}", 3, resp.Serialize());
                    return resp;
                }

                //default logic
                if (br.TryLogic == "oco")
                {
                    ErrorFx.V("FillOrLeave::OCO");
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"OCO::Response: {resp.ordStatus} Qty:{resp.orderQty} U:{resp.leavesQty} {resp.ordRejReason} {resp.error?.message}", 3, resp.Serialize());
                    return resp;
                }

                if (br.TryLogic == "m") //m - cancel, buy market
                {
                    //place new as market if simulation didn't fill, quicker 
                    br.OrderType = "Limit";
                    br.TimeInForce = "FillOrKill";
                    ErrorFx.V("NewTrade Retry as Market", 3);
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"RetryMarket::Response: {resp.ordStatus} Qty:{resp.orderQty} U:{resp.leavesQty} {resp.ordRejReason} {resp.error?.message}", 3, resp.Serialize());

                    if (resp.ordStatus == "Filled")
                    {
                        ErrorFx.V("Filled, Done!");
                        return resp;
                    }

                    if (resp.orderQty > 0)
                    {
                        OrderResponse amd = await client.AmendOrderAsync(br, true, resp);
                        br.Quantity = resp.leavesQty; //new quantity = unfilled qty
                        ErrorFx.V($"RetryMarket::AmendResp {amd.ordStatus} {amd.error?.message}", 1, amd.Serialize());
                    }

                    ErrorFx.V("Bitmex::Buy Market", 1);
                    br.OrderType = "Market";
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"RetryMarket::LeftoverQtyNewMarket: {resp.ordStatus} Qty:{resp.orderQty} U:{resp.leavesQty} {resp.ordRejReason} {resp.error?.message}", 1, resp.Serialize());

                }

                if (br.TryLogic == "t") //t - trash, kill unless over 50%?
                {
                    br.TimeInForce = "ImmediateOrCancel";
                    resp = await client.PostOrdersAsync(br);
                    ErrorFx.V($"Take Partials:Response: {resp.ordStatus} Qty:{resp.orderQty} U:{resp.leavesQty} {resp.ordRejReason} {resp.error?.message}", 1, resp.Serialize());
                }

            }
            catch (Exception ex)
            {
                ex.SaveToDB(resp.error?.message);
            }

            return resp;
        }


        public static int GetExchangeId(string exchange)
        {
            string s = exchange?.ToUpper();
            if (s == "BITMEX")
            {
                return 2;
            }

            if (s == "BINANCE")
            {
                return 3;
            }

            if (s == "BITTREX")
            {
                return 4;
            }

            if (s == "POLONIEX")
            {
                return 5;
            }
            //TestNet 
            return 1;
        }




    }
}
