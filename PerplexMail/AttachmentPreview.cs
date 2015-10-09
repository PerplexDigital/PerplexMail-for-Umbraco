using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail
{
    /// <summary>
    /// A container class containing information about where an attachment can be located that has been sent with an e-mail by the e-mailpackage.
    /// </summary>
    public class AttachmentPreview
    {
        /// <summary>
        /// If the attachment was an Umbraco Media Node, contains the Media Node ID.
        /// </summary>
        public Int64 id { get; set; }
        /// <summary>
        /// The order number of the attachment within the e-mail.
        /// </summary>
        public int order { get; set; }
        /// <summary>
        /// If the attachment was an Umbraco Media Node, contains the Media Node ID.
        /// </summary>
        public string mediaUrl { get; set; }
        /// <summary>
        /// The name of the attachment
        /// </summary>
        public string name { get; set; }
    }
}