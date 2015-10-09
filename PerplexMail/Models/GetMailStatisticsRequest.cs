using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail.Models
{
    public class GetMailStatisticsRequest
    {
        public int CurrentNodeId { get; set; }
        public string SearchReceiver { get; set; }
        public string SearchContent { get; set; }
        public string FilterDateFrom2
        {
            get
            {
                return FilterDateFrom.ToShortDateString();
            }
            set
            {
                DateTime tmp;
                if (DateTime.TryParse(value, out tmp))
                    FilterDateFrom = tmp;
            }
        }

        public string FilterDateTo2
        {
            get
            {
                return FilterDateTo.ToShortDateString();
            }
            set
            {
                DateTime tmp;
                if (DateTime.TryParse(value, out tmp))
                    FilterDateTo = tmp;
            }
        }

        public DateTime FilterDateFrom { get; set; }
        public DateTime FilterDateTo { get; set; }
        public EnmStatus FilterStatus { get; set; }
        public string OrderBy { get; set; }
        public int CurrentPage { get; set; }
        public int AmountPerPage { get; set; }
    }
}