using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail.Models
{
    public class ResendEmailRequest
    {
        public int EmailLogId { get; set; }
        public string EmailAddress { get; set; }
    }
}