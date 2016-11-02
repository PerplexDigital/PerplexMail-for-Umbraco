using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace PerplexMail.Models
{
    public class SendTestEmailRequest
    {
        public List<string> EmailAddresses { get; set; }
        public int EmailNodeId { get; set; }
        public List<EmailTag> Tags { get; set; }
        public IEnumerable<MailAddress> MailAddresses
        {
            get
            {
                if (EmailAddresses != null)
                {
                    foreach (var emailaddress in EmailAddresses)
                    {
                        if (!String.IsNullOrEmpty(emailaddress))
                        {
                            MailAddress email;
                            try
                            {
                                email = new MailAddress(emailaddress);
                            }
                            catch (Exception)
                            {
                                email = null;
                            }
                            if (email != null)
                                yield return email;
                        }
                    }
                }
            }
        }
    }
}