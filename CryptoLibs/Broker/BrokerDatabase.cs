using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;


namespace Piggy 
{  
    public partial class BrokerDatabase : DbContext
    {
        public BrokerDatabase()
            : base($"data source=" + (Environment.MachineName == "DESKTOP" ? "(local)" : "db.database.windows.net") + ";initial catalog=BitrexApi;persist security info=True;MultipleActiveResultSets=True;App=PiggyApi")
        {

        }

        public virtual DbSet<BitmexInstrument> BitmexInstruments { get; set; }

        public virtual DbSet<AppErrorLog> AppErrorLogs { get; set; }
        public virtual DbSet<ErrorLog> ErrorLogs { get; set; }
        public virtual DbSet<BrokerUser> BrokerUsers { get; set; }
        public virtual DbSet<BrokerPreference> BrokerPreferences { get; set; }
        public virtual DbSet<BrokerStrategy> BrokerStrategies { get; set; }
        public virtual DbSet<BrokerStrategiesRun> BrokerStrategyRuns { get; set; }
        public virtual DbSet<BrokerStrategiesTrade> BrokerStrategyTrades { get; set; }
        public virtual DbSet<ScraperAlert> ScraperAlerts { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.AskPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.EntryPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.ExitPrice).HasPrecision(18,11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.ExitProfit).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.ExitProfitPercent).HasPrecision(18, 11);

            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.MarginCost).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.StopPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.LiquidationPrice).HasPrecision(18, 11);

            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.EntryFee).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.ExitFee).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.Balance).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.LastPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerStrategiesTrade>().Property(x => x.MarkPrice).HasPrecision(18, 11);            

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.markPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.askPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.lastPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.lastPriceProtected).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.bidPrice).HasPrecision(18, 11);

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.askPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.highPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.lowPrice).HasPrecision(18, 11);

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.impactAskPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.impactBidPrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.impactMidPrice).HasPrecision(18, 11);

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.prevClosePrice).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.prevPrice24h).HasPrecision(18, 11);

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.lastChangePcnt).HasPrecision(18, 3);

            modelBuilder.Entity<BitmexInstrument>().Property(x => x.fundingRate).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.indicativeFundingRate).HasPrecision(18, 11);
            modelBuilder.Entity<BitmexInstrument>().Property(x => x.fairBasisRate).HasPrecision(18, 11);

            modelBuilder.Entity<BrokerPreference>().Property(x => x.TakeAt).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerPreference>().Property(x => x.SecondTakeAt).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerPreference>().Property(x => x.StopAt).HasPrecision(18, 11);
            modelBuilder.Entity<BrokerPreference>().Property(x => x.PegAt).HasPrecision(18, 11);

        }
    }

    public class ScraperAlert
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public int? Value { get; set; }
        public string Data { get; set; }
        public DateTime? TimeStamp { get; set; }
        public DateTime? LastPublished { get; set; }
        public bool? Processed { get; set; }
        public int? WaitBeforeNext { get; set; }
        public bool? Enabled { get; set; }
        public bool? IsDataScrape { get; set; }
    }

    
}
