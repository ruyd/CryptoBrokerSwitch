using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piggy
{
    public class EthereumAccount
    {
        public long ID { get; set; }
        public string Address { get; set; }
        public decimal? Balance { get; set; }
        public decimal? Percentage { get; set; }

        public int? TxCount { get; set; }
        public int? Rank { get; set; }
        public int? RunID { get; set; }

        public DateTime? DateTimeCreated { get; set; }

        [NotMapped]
        public string BalanceText { get; set; }

    }


    [NotMapped]
    public class EthAccountJson : EthereumAccount
    {
        public DateTime? RunDate { get; set; }
    }

    public class EthBalance
    {
        public int? RunID { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Balance { get; set; }
        public decimal? PrevBalance { get; set; }
        public decimal? Diff { get; set; }
        public decimal? DiffPer { get; set; }
    }

    public class EthNamed
    {
        public int? RunID { get; set; }
        public int? Rank { get; set; }
        public int? StartRank { get; set; }
        public string Address { get; set; }
        public DateTime? Date { get; set; }
        public decimal? LastBalance { get; set; }
        public decimal? StartBalance { get; set; }
        public decimal? Diff { get; set; }
        public decimal? DiffPer { get; set; }
    }
}
