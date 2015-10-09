using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace PerplexMail.Models
{
    public class SendTestEmailRequest
    {
        public string EmailAddress { get; set; }
        public int EmailNodeId { get; set; }
        public List<EmailTag> Tags { get; set; }
        public MailAddress MailAddress
        {
            get
            {
                if (!String.IsNullOrEmpty(EmailAddress))
                    return new MailAddress(EmailAddress);
                else
                    return null;
            }
        }
    }
}