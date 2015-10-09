using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail.Models
{
    /// <summary>
    /// Helper class that makes it easier for us to send a response back to the client
    /// </summary>
    public class AjaxResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}