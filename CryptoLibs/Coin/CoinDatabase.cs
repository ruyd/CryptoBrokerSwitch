using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Binance.Net.Objects;

namespace Piggy
{
     
    public partial class CoinDatabase : DbContext
    {
        public CoinDatabase()
            : base($"data source=" +  (Environment.MachineName == "DESKTOP" ? "(local)" : "db.database.windows.net") + ";initial catalog=BitrexApi;persist security info=True;MultipleActiveResultSets=True;App=CoinApi")
        {
        }
       
        public virtual DbSet<EthereumAccount> EthereumAccounts { get; set; }
        public virtual DbSet<BinanceMarket> BinanceMarkets { get; set; }
        public virtual DbSet<CoinMarketCap> CoinMarketCaps { get; set; }
        public virtual DbSet<BinanceWallet> BinanceWallets { get; set; }

        public virtual DbSet<BinanceSymbol> BinanceSymbols { get; set; }
        public virtual DbSet<BinanceOrder> BinanceOrders { get; set; }
        public virtual DbSet<BinanceTrade> BinanceTrades { get; set; }

        //public virtual DbSet<BrokerBufferTrade> BrokerBufferTrades { get; set; }

        public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

        /// <summary>
        /// TODO: MOVE PRECISION TO CLASSES eliminiate this 
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BinanceMarket>().Property(x => x.AskPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.AskQuantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.BidPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.BidQuantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.HighPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.LastPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.LastQuantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.LowPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.WeightedAveragePrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.Volume).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.QuoteVolume).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.PreviousClosePrice).HasPrecision(18, 8);

            modelBuilder.Entity<BinanceMarket>().Property(x => x.PriceChange).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceMarket>().Property(x => x.PriceChangePercent).HasPrecision(18, 8);

            modelBuilder.Entity<BinanceMarket>().Property(x => x.OpenPrice).HasPrecision(18, 8);
      

            modelBuilder.Entity<EthereumAccount>().Property(x => x.Balance).HasPrecision(18, 8);
            modelBuilder.Entity<EthereumAccount>().Property(x => x.Percentage).HasPrecision(18, 8);
            // ignore a type that is not mapped to a database table
     

            modelBuilder.Entity<CoinMarketCap>().Property(x => x.price_btc).HasPrecision(18, 8);

            modelBuilder.Entity<CoinMarketCap>().Property(x => x.price_usd).HasPrecision(18, 2);
            modelBuilder.Entity<CoinMarketCap>().Property(x => x.volume_usd_24h).HasPrecision(22, 2);
            modelBuilder.Entity<CoinMarketCap>().Property(x => x.market_cap_usd).HasPrecision(30, 2);

            modelBuilder.Entity<CoinMarketCap>().Property(x => x.percent_change_1h).HasPrecision(18, 8);
            modelBuilder.Entity<CoinMarketCap>().Property(x => x.percent_change_24h).HasPrecision(18, 8);
            modelBuilder.Entity<CoinMarketCap>().Property(x => x.percent_change_7d).HasPrecision(18, 8);

            modelBuilder.Entity<BinanceWallet>().Property(x => x.Free).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceWallet>().Property(x => x.Locked).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceWallet>().Property(x => x.Total).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceWallet>().Property(x => x.FirstBuyPrice).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceWallet>().Property(x => x.LastBuyPrice).HasPrecision(18, 8);

            //modelBuilder.Entity<BinanceOrder>().Property(f => f.OrderId).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<BinanceOrder>().Property(x => x.Price).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceOrder>().Property(x => x.StopPrice).HasPrecision(18, 8);
            //modelBuilder.Entity<BinanceOrder>().Property(x => x.USDT).HasPrecision(18, 2);
            modelBuilder.Entity<BinanceOrder>().Property(x => x.OriginalQuantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceOrder>().Property(x => x.ExecutedQuantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceOrder>().Property(x => x.IcebergQuantity).HasPrecision(18, 8);

            modelBuilder.Entity<BinanceTrade>().Property(x => x.Price).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceTrade>().Property(x => x.Quantity).HasPrecision(18, 8);
            modelBuilder.Entity<BinanceTrade>().Property(x => x.Commission).HasPrecision(18, 8);

            //modelBuilder.Entity<BrokerBufferTrade>().Property(x => x.Quantity).HasPrecision(18, 8);
            //modelBuilder.Entity<BrokerBufferTrade>().Property(x => x.EntryPrice).HasPrecision(18, 8);
            //modelBuilder.Entity<BrokerBufferTrade>().Property(x => x.ExitPrice).HasPrecision(18, 8);
            //modelBuilder.Entity<BrokerBufferTrade>().Property(x => x.Profit).HasPrecision(18, 8);
            //modelBuilder.Entity<BrokerBufferTrade>().Property(x => x.ProfitPercent).HasPrecision(18, 8);
 

        }
    }

 

}
