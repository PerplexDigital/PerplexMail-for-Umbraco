using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail.Models
{
    public class MailStatisticsResponse
    {
        public int TotalSent { get; set; }
        public int TotalRead { get; set; }
        public int SelectionCount { get; set; }
        public IEnumerable<LogEmail> Emails { get; set; }
    }
}