using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using umbraco.NodeFactory;
using System.Data.Common;
using PerplexMail.Models;

namespace PerplexMail
{
    public static class Helper
    {
        #region Extensions

        /// <summary>
        /// Sends a .NET email to the client Browser which can be openend in Outlook.
        /// CAUTION: Calling this method will end the current request.
        /// </summary>
        /// <param name="msg">The .NET SMTP MailMessage to send to the browser</param>
        /// <param name="filename">The name of the file, without the extension</param>
        public static void SendMailMessageToBrowser(this MailMessage msg, string filename)
        {
            using (var client = new SmtpClient())
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name);
                tempFolder = Path.Combine(tempFolder, "MailMessageToMsgTmp");
                tempFolder = Path.Combine(tempFolder, Guid.NewGuid().ToString());
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = tempFolder;
                client.Send(msg);
                using (var fs = new FileStream(Directory.GetFiles(tempFolder).Single(), FileMode.Open))
                    Helper.StreamFileToBrowser(fs, filename + ".eml", "message/rfc822");
            }
        }

        public static string SanitizeFilename(string filename)
        {
            if (filename != null)
                return new String(filename.Where(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || c == '-' || c == '_').ToArray());
            else
                return null;
        }

        public static string GetProperty(this Node n, EnmUmbracoPropertyAlias alias, bool searchParent = true)
        {
            // Node bestaat?
            if (n != null)
            {
                // Haal de property op
                var p = n.GetProperty(alias.ToString());
                // Als er wat in zit retourneren we hem
                if (p != null && !String.IsNullOrEmpty(p.Value))
                    return p.Value;
                // Bestaat de parent?
                if (searchParent && n.Parent != null)
                {
                    // Haal de property op van de parent
                    p = n.Parent.GetProperty(alias.ToString());
                    // Bestaat de property en zit er wat in?
                    if (p != null && !String.IsNullOrEmpty(p.Value))
                        // Retourneren
                        return p.Value;
                }
            }
            // Default: Retourneer een lege string
            return string.Empty;
        }

        #endregion

        /// <summary>
        /// Website URL without / at the end, for example http://www.mywebsite.nl
        /// </summary>
        public static string WebsiteUrl
        {
            get
            {
                if (System.Web.HttpContext.Current != null)
                {
                    string url = (IsHttps ? "https" : "http") + System.Uri.SchemeDelimiter + System.Web.HttpContext.Current.Request.Url.Host;
                    if (!HttpContext.Current.Request.Url.IsDefaultPort)
                        url += ":" + System.Web.HttpContext.Current.Request.Url.Port.ToString();
                    return url;
                }
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Checks if the current connection is securely running under SSL (HTTPS).
        /// This method is different from calling Request.IsSecureConnection in that it also works in a loadbalanced environment.
        /// </summary>
        /// <returns></returns>
        public static bool IsHttps
        {
            get
            {
                var c = System.Web.HttpContext.Current;
                if (c != null)
                {
                    if (!String.IsNullOrEmpty(c.Request.ServerVariables["HTTP_X_FORWARDED_PROTO"]))
                        return c.Request.ServerVariables["HTTP_X_FORWARDED_PROTO"].ToLower() == "https";
                    else
                        return c.Request.IsSecureConnection;
                } else
                    return false;
            }
        }

        public static String ReadFileContents(String virtualPath)
        {
            if (String.IsNullOrEmpty(virtualPath))
                return "";
            if (virtualPath.StartsWith("/"))
                virtualPath = "~" + virtualPath;
            try
            {
                string absolutePath = System.Web.Hosting.HostingEnvironment.MapPath(virtualPath);
                return System.IO.File.ReadAllText(absolutePath);
            }
            catch (Exception)
            {
                // Fout of leeg
                return "";
            }
        }

        // De MBFS-calculatorendashboard variant van de if-then-else parsing
        public static string parseIfs(String sContent, List<EmailTag> values)
        {
            string sPattern = "{if (.*?)\\{/if}";
            // Get alle if-then-else-constructies in de string
            foreach (Match m in Regex.Matches(sContent, sPattern, RegexOptions.Multiline | RegexOptions.Singleline))
            {
                Boolean bUitkomstIf = false;

                // Bepaal of we het if-gedeelte of het else-gedeelte moeten weergeven (oftewel parse de if constructie)
                string sPatternIfStatement = "{if.*?}";
                Match mIf = Regex.Match(m.Value, sPatternIfStatement);

                // Kijk of er variabelen in staan
                string lefthandside = String.Empty; string righthandside = String.Empty; string sOperator = String.Empty;

                // Bepaal de operator
                if (mIf.Value.Contains("!="))
                    sOperator = "!=";
                else if (mIf.Value.Contains("="))
                    sOperator = "=";

                int positieOperator = mIf.Value.IndexOf(sOperator);

                lefthandside = mIf.Value.Substring(0, positieOperator).Replace("{if", "").Trim();
                righthandside = mIf.Value.Substring(positieOperator + sOperator.Length, mIf.Value.Length - (positieOperator + sOperator.Length)).Replace("}", "").Trim();

                if (sOperator == "=")
                    bUitkomstIf = parseTags(lefthandside, values).Replace("'", "").ToString() == parseTags(righthandside, values).Replace("'", "").ToString();
                else if (sOperator == "!=")
                    bUitkomstIf = parseTags(lefthandside, values).Replace("'", "").ToString() != parseTags(righthandside, values).Replace("'", "").ToString();

                String sOutputIfThenElse = String.Empty;

                if (bUitkomstIf)        // output if-part
                {
                    int positieElseOfEindeIf;
                    if (m.Value.Contains("{else}"))
                        positieElseOfEindeIf = m.Value.IndexOf("{else}");
                    else
                        positieElseOfEindeIf = m.Value.IndexOf("{/if}");

                    sOutputIfThenElse = m.Value.Substring(0, positieElseOfEindeIf).Replace(mIf.Value, "");
                }
                else if (m.Value.Contains("{else}"))                   // output else-part of vergeet if-part
                {
                    int positieElse; int positieEndIf;
                    positieElse = m.Value.IndexOf("{else}");
                    positieEndIf = m.Value.IndexOf("{/if}");
                    sOutputIfThenElse = m.Value.Substring(positieElse + "{else}".Length, positieEndIf - (positieElse + "{else}".Length));
                }

                sContent = sContent.Replace(m.Value, sOutputIfThenElse);
                sContent = parseIfs(sContent, values);                      // En hier roepen we onszelf nog een keer aan, om eventueel geneste if's te verhelpen
            }

            return sContent;
        }

        /// <summary>
        /// Replaces in the input all First-items with the Second-items
        /// </summary>
        /// <param name="input"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        static string parseTags(String input, List<EmailTag> values)
        {
            if (values != null && values.Count > 0)
                // Iterate through all pairs in the list and replace all the tags in the body
                foreach (var p in values)
                    if (p != null && p.FullTag != null && p.Value != null)
                        input = input.Replace(p.FullTag, p.Value);

            return input;
        }

        /// <summary>
        /// Replaces occurances of a specific if tag depending on true or false
        /// </summary>
        /// <param name="tagname">The tagname</param>
        /// <param name="stringToSearch">The string to find the tags etc in.</param>
        /// <param name="state">Evaluate if statement as true or false?</param>
        /// <returns>The parsed "stringToSearch" text, where the if tags are removed and the appropriate content is placed (or none)</returns>
        public static string ReplaceIfThenElseStatements(String tagname, String stringToSearch, Boolean state)
        {
            var r = new Regex(Regex.Escape("[#" + tagname + "#]") + "(.*?)" + Regex.Escape("[#/" + tagname + "#]"), RegexOptions.Singleline);
            stringToSearch = r.Replace(stringToSearch,
                        new MatchEvaluator(
                            delegate(Match m)
                            {
                                string text = m.Result("$1");
                                if (!String.IsNullOrEmpty(text))
                                {
                                    var data = text.Split(new []{"[#else#]"}, StringSplitOptions.None);
                                    if (data.Length == 2)
                                        if (state)
                                            return data[0];
                                        else
                                            return data[1];
                                    else if (state)
                                        return text;
                                }
                                return String.Empty;
                            }));
            // W: Even luiheid: Tweede regex om een NOT te parsen. Zou eventueel makkelijk in 1x kunnen maar even om het gemak...
            r = new Regex(Regex.Escape("[#!" + tagname + "#]") + "(.*?)" + Regex.Escape("[#/!" + tagname + "#]"), RegexOptions.Singleline);
            stringToSearch = r.Replace(stringToSearch,
                        new MatchEvaluator(
                            delegate(Match m)
                            {
                                string text = m.Result("$1");
                                if (!String.IsNullOrEmpty(text))
                                {
                                    var data = text.Split(new[] { "[#else#]" }, StringSplitOptions.None);
                                    if (data.Length == 2)
                                        if (!state)
                                            return data[0];
                                        else
                                            return data[1];
                                    else if (!state)
                                        return text;
                                }
                                return String.Empty;
                            }));
            return stringToSearch;
        }

        public static string RemoveMailTag(string htmlContent)
        {
            return Regex.Replace(htmlContent, "<img src=\".*?" + Constants.STATISTICS_IMAGE + ".*?\">", "");
        }

        public static string CssToXpath(string CSS)
        {
            foreach (var regexReplace in regexReplaces)
                CSS = regexReplace.Regex.Replace(CSS, regexReplace.Replace);
            return "//" + CSS;
        }

        struct RegexReplace
        {
            public Regex Regex;
            public string Replace;
        }

        // References:  http://ejohn.org/blog/xpath-css-selectors/
        //              http://code.google.com/p/css2xpath/source/browse/trunk/src/css2xpath.js
        static RegexReplace[] regexReplaces = new[] 
        {
            // add @ for attribs
            new RegexReplace {
                Regex = new Regex(@"\[([^\]~\$\*\^\|\!]+)(=[^\]]+)?\]", RegexOptions.Multiline),
                Replace = @"[@$1$2]"
            },
            //  multiple queries
            new RegexReplace {
                Regex = new Regex(@"\s*,\s*", RegexOptions.Multiline),
                Replace = @"|"
            },
            // , + ~ >
            new RegexReplace {
                Regex = new Regex(@"\s*(\+|~|>)\s*", RegexOptions.Multiline),
                Replace = @"$1"
            },
            //* ~ + >
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*])~([a-zA-Z0-9_\-\*])", RegexOptions.Multiline),
                Replace = @"$1/following-sibling::$2"
            },
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*])\+([a-zA-Z0-9_\-\*])", RegexOptions.Multiline),
                Replace = @"$1/following-sibling::*[1]/self::$2"
            },
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*])>([a-zA-Z0-9_\-\*])", RegexOptions.Multiline),
                Replace = @"$1/$2"
            },
            // all unescaped stuff escaped
            new RegexReplace {
                Regex = new Regex(@"\[([^=]+)=([^'|""][^\]]*)\]", RegexOptions.Multiline),
                Replace = @"[$1='$2']"
            },
            // all descendant or self to //
            new RegexReplace {
                Regex = new Regex(@"(^|[^a-zA-Z0-9_\-\*])(#|\.)([a-zA-Z0-9_\-]+)", RegexOptions.Multiline),
                Replace = @"$1*$2$3"
            },
            new RegexReplace {
                Regex = new Regex(@"([\>\+\|\~\,\s])([a-zA-Z\*]+)", RegexOptions.Multiline),
                Replace = @"$1//$2"
            },
            new RegexReplace {
                Regex = new Regex(@"\s+\/\/", RegexOptions.Multiline),
                Replace = @"//"
            },
            // :first-child
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*]+):first-child", RegexOptions.Multiline),
                Replace = @"*[1]/self::$1"
            },
            // :last-child
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*]+):last-child", RegexOptions.Multiline),
                Replace = @"$1[not(following-sibling::*)]"
            },
            // :only-child
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*]+):only-child", RegexOptions.Multiline),
                Replace = @"*[last()=1]/self::$1"
            },
            // :empty
            new RegexReplace {
                Regex = new Regex(@"([a-zA-Z0-9_\-\*]+):empty", RegexOptions.Multiline),
                Replace = @"$1[not(*) and not(normalize-space())]"
            },
            // |= attrib
            new RegexReplace {
                Regex = new Regex(@"\[([a-zA-Z0-9_\-]+)\|=([^\]]+)\]", RegexOptions.Multiline),
                Replace = @"[@$1=$2 or starts-with(@$1,concat($2,'-'))]"
            },
            // *= attrib
            new RegexReplace {
                Regex = new Regex(@"\[([a-zA-Z0-9_\-]+)\*=([^\]]+)\]", RegexOptions.Multiline),
                Replace = @"[contains(@$1,$2)]"
            },
            // ~= attrib
            new RegexReplace {
                Regex = new Regex(@"\[([a-zA-Z0-9_\-]+)~=([^\]]+)\]", RegexOptions.Multiline),
                Replace = @"[contains(concat(' ',normalize-space(@$1),' '),concat(' ',$2,' '))]"
            },
            // ^= attrib
            new RegexReplace {
                Regex = new Regex(@"\[([a-zA-Z0-9_\-]+)\^=([^\]]+)\]", RegexOptions.Multiline),
                Replace = @"[starts-with(@$1,$2)]"
            },
            // != attrib
            new RegexReplace {
                Regex = new Regex(@"\[([a-zA-Z0-9_\-]+)\!=([^\]]+)\]", RegexOptions.Multiline),
                Replace = @"[not(@$1) or @$1!=$2]"
            },
            // ids
            new RegexReplace {
                Regex = new Regex(@"#([a-zA-Z0-9_\-]+)", RegexOptions.Multiline),
                Replace = @"[@id='$1']"
            },
            // classes
            new RegexReplace {
                Regex = new Regex(@"\.([a-zA-Z0-9_\-]+)", RegexOptions.Multiline),
                Replace = @"[contains(concat(' ',normalize-space(@class),' '),' $1 ')]"
            },
            // normalize multiple filters
            new RegexReplace {
                Regex = new Regex(@"\]\[([^\]]+)", RegexOptions.Multiline),
                Replace = @" and ($1)"
            },
        };

        /// <summary>
        /// Returns the IP-adres of the client
        /// </summary>
        /// <returns></returns>
        public static string GetIp()
        {
            // Als je op een loadbalanced-omgeving zit, dan is het een ander verhaal
            String ip = String.Empty;
            try
            {
                if (!String.IsNullOrEmpty(System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]))
                    ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                else
                    ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }
            catch (Exception)
            {
                ip = "-";
                //' WB + JS 29-04-2012 Dit kan bijvoorbeeld optreden wanneer je vanuit Ogone via async deze functie aanroept
            }

            return ip;
        }

        /// <summary>
        /// Returns the description that is filled in by enum. Like Public Enum AdvanceVerzekeringType Description("Nieuw voertuig") NieuwVoertuig = 1 End Enum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string GetEnumDescription(object value)
        {
            string result = string.Empty;

            if (value != null)
            {
                result = value.ToString();
                //// Get the type from the object.
                Type type = value.GetType();
                try
                {
                    result = Enum.GetName(type, value);
                    //// Get the member on the type that corresponds to the value passed in.
                    FieldInfo fieldInfo = type.GetField(result);
                    //// Now get the attribute on the field.
                    object[] attributeArray = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    DescriptionAttribute attribute = null;

                    if (attributeArray.Length > 0)
                    {
                        attribute = (DescriptionAttribute)attributeArray[0];
                    }
                    if ((attribute != null))
                    {
                        result = attribute.Description;
                    }
                }
                catch (ArgumentNullException)
                {
                    ////We shouldn't ever get here, but it means that value was null, so we'll just go with the default.
                    result = string.Empty;
                }
                catch (ArgumentException)
                {
                    ////we didn't have an enum.
                    result = value.ToString();
                }
                //// Return the description.
            }
            return result;
        }

        private static DateTime makeDate(int dag, int maand, int jaar)
        {
            return new DateTime(jaar, maand, dag);
        }

        static object _synclock = new object();

        /// <summary>
        /// This function attempts to resolve SQL exceptions that arise within PerplexMail
        /// </summary>
        /// <param name="ex">The SQL exception that needs to be handled</param>
        /// <returns>Returns the success if the conflict has been resolved.</returns>
        public static bool HandleSqlException(DbException ex)
        {
            // Make sure a maximum of one thread attempts to fix the database at a time.
            lock (_synclock)
            {
                // Known errors:
                // "The specified table does not exist. [ perplexMailLog ]" - SQL CE
                // "Invalid object name 'TABLEAME'." - SQL
                if (ex.Message.Contains(Constants.SQL_TABLENAME_PERPLEXMAIL_LOG) ||
                    ex.Message.Contains(Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS))
                {
                    try
                    {
                        // Confirm whether or not the logging table exists
                        if (!Sql.DoesSQLTableExist(Constants.SQL_TABLENAME_PERPLEXMAIL_LOG))
                            // Create the logging table
                            Sql.ExecuteSql(Constants.SQL_QUERY_CREATE_TABLE_LOG, CommandType.Text);
                        // Confirm whether or not the statistics table exists
                        if (!Sql.DoesSQLTableExist(Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS))
                            // Create the statistics table
                            Sql.ExecuteSql(Constants.SQL_QUERY_CREATE_TABLE_STATISTICS, CommandType.Text);
                        return true; // Assume all missing objects have been added
                    }
                    catch
                    {
                        return false; // Could not create SQL tables
                    }
                } // <== Add more exception handling mechanics here
                else
                    // Unable to solve the problem
                    return false;
            }
        }

        /// <summary>
        /// Gets the webversion URL for any logged email based on the ID.
        /// </summary>
        /// <param name="id">The logged email ID</param>
        /// <returns>An absolute URL to the webversion URL of the email</returns>
        public static string GenerateWebversionUrl(string mailLogId)
        {
            String authenticationHash = HttpUtility.UrlEncode(Security.Hash(mailLogId.ToString()));
            return Helper.WebsiteUrl +
                    "?" + Constants.STATISTICS_QUERYSTRINGPARAMETER_MAILID + "=" + mailLogId.ToString() + // Email ID
                    "&" + Constants.STATISTICS_QUERYSTRINGPARAMETER_ACTION + "=" + EnmAction.webversion.ToString() + // The action to be performed (view email online)
                    "&" + Constants.STATISTICS_QUERYSTRINGPARAMETER_AUTH + "=" + authenticationHash;
        }
        
        /// <summary>
        /// Get the filepath that has been uploaded on any Umbraco media node.
        /// </summary>
        /// <param name="umbracoMediaId">The Umbraco Media node ID</param>
        /// <param name="toAbsoluteFilePath">(default/false) the resulting URL should be a relative path. If TRUE, the returned URL will be an absolute URL</param>
        /// <returns>A relative or absolute URL to the Umbraco Media file. Returns NULL if the file does not exist</returns>
        public static string GetUmbracoMediaFile(int umbracoMediaId, bool toAbsoluteFilePath = false)
        {
            if (umbracoMediaId > 0)
            {
                var m = umbraco.library.GetMedia(umbracoMediaId, false);
                if (m != null)
                {
                    var xpathNav = m.Current;
                    if (xpathNav.SelectSingleNode("error") == null)
                    {
                        //xpathNav.MoveToRoot(); // Navigate to the root of the media node
                        //xpathNav.MoveToFirstChild(); // Select the first child (there can be only one child. Since there are multiple media types of images we have to perform this step)
                        var n = xpathNav.SelectSingleNode("umbracoFile");
                        if (n != null)
                        {
                            string relativeFilePath = n.InnerXml;
                            if (!String.IsNullOrEmpty(relativeFilePath))
                                if (toAbsoluteFilePath)
                                    // Return the absolute filepath
                                    return System.Web.HttpContext.Current.Request.MapPath("~" + relativeFilePath, "/", false);
                                else
                                    // Return the relative filepath
                                    return relativeFilePath;
                        }
                    }

                }
            }
            return null;
        }

        /// <summary>
        /// Sends a file from a physical location to the client Browser.
        /// - Use with causion: only supply filepaths that are authorized to be sent over to the browser!
        /// - Only usable during a HttpRequest from a client
        /// - Calling this method immediatly ends the current request
        /// - This method may throw a FileNotFoundException if the file does not exist.
        /// </summary>
        /// <param name="targetFilePath">Het doelbestand. Mag een relatieve (begin met '/') of een absolute URL zijn</param>
        /// <remarks>Deze functie beeindigd direct de request. Voer hierna geen functies meer uit!</remarks>
        public static void StreamFileToBrowser(string targetFilePath)
        {
            if (String.IsNullOrEmpty(targetFilePath))
                throw new ArgumentException("Error executing PerplexMail.Helper.StreamFileToBrowser(): specify a valid filepath", "targetFilePath");
            if (targetFilePath.StartsWith("/"))
                targetFilePath = System.Web.Hosting.HostingEnvironment.MapPath(targetFilePath);

            // FileNotFoundException may occur
            using (var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                Helper.StreamFileToBrowser(fs, System.IO.Path.GetFileName(targetFilePath), Helper.GetContentType(targetFilePath));
        }

        /// <summary>
        /// Sends a stream of data (containing a filename) to a client browser. Usefull for in-memory files and data.
        /// - Only usable during a HttpRequest from a client
        /// - Calling this method immediatly ends the current request
        /// </summary>
        /// <param name="filebytes">The stream containing the (file) data to send to the client</param>
        /// <param name="filenameInBrowser">The name of the file in the browser</param>
        /// <param name="contentType">(optioneel) specify a MIME content type (for example 'application/pdf')</param>
        /// <remarks>Calling this method ends the current request</remarks>
        public static void StreamFileToBrowser(Stream filebytes, string filenameInBrowser, string contentType = "application/octet-stream")
        {
            // Validate input
            if (filebytes == null || !filebytes.CanRead || filebytes.Length == 0)
                throw new ArgumentException("Error executing PerplexLib.FileOperations.streamFileToBrowser(): invalid file contents", "file");
            if (String.IsNullOrEmpty(filenameInBrowser))
                throw new ArgumentException("Error executing PerplexLib.FileOperations.streamFileToBrowser(): specify a filename", "filenameInBrowser");
            if (String.IsNullOrEmpty(contentType))
                throw new ArgumentException("Error executing PerplexLib.FileOperations.streamFileToBrowser(): specify a valid MIME content type", "contentType");
            if (HttpContext.Current == null || HttpContext.Current.Response == null)
                throw new InvalidOperationException("Error executing PerplexLib.FileOperations.streamFileToBrowser(): this method can only be used within the context of a client httpwebrequest");
            var r = HttpContext.Current.Response;

            // Send in packats of 4k size, to avoid problems with larger files
            int transferredBytes = 0;
            long totalBytesToRead = filebytes.Length;
            byte[] buffer = new byte[4096];

            r.Clear();
            r.ClearHeaders();
            r.AddHeader("Cache-Control", "no-store, no-cache"); // IE fix
            r.ContentType = contentType;
            r.AddHeader("Content-Disposition", "attachment; filename=" + filenameInBrowser);
            filebytes.Position = 0;
            using (filebytes)
                while (transferredBytes < totalBytesToRead && r.IsClientConnected)
                {
                    int bytesRead = filebytes.Read(buffer, 0, buffer.Length);
                    r.OutputStream.Write(buffer, 0, bytesRead);
                    r.Flush();
                    transferredBytes += bytesRead;
                }
            r.End();
        }

        /// <summary>
        /// Determines the MIME type of a file based on it's filename extension.
        /// Note that it is possible the file's content type to not match it's extension's MIME type
        /// </summary>
        /// <param name="fileName">The file name to determine the MIME type for.</param>
        /// <returns>The MIME type corresponding to the extension of the specified file name, if found; otherwise, null.</returns>
        public static string GetContentType(string fileName)
        {
            // Determine the filename's extension
            var extension = Path.GetExtension(fileName);
            if (String.IsNullOrWhiteSpace(extension))
                return null;

            // Pull the MIME type out of the registry key
            var registryKey = Registry.ClassesRoot.OpenSubKey(extension);

            if (registryKey == null)
                return null;

            var value = registryKey.GetValue("Content Type") as string;

            return String.IsNullOrWhiteSpace(value) ? null : value;
        }

        static DateTime _nextLogCheck;

        static void PurgeOldLogs(int emailId, DateTime purgeDate)
        {
            bool retry = true;
            retry:
            try
            {
                // Create the purge request
                var parameters = new { emailID = emailId, maximumLogDate = purgeDate };
                Sql.ExecuteSql(Constants.SQL_QUERY_REMOVE_OLDLOGS, CommandType.Text, parameters);

                // Set the time at which the next purge will take place
                _nextLogCheck = DateTime.Now.AddHours(1);
            }
            catch (DbException sqlEx)
            {
                if (retry && HandleSqlException(sqlEx))
                {
                    retry = false;
                    goto retry;
                }
                else
                    throw;
            }
        }
        /// <summary>
        /// This function removes all old log entries from the database (as configured in Umbraco).
        /// </summary>
        /// <param name="emailId">The e-mail node type to purge log entries for</param>
        public static void RemoveOldLogEntries(int emailId)
        {
            // Input validation && only check for old log entries every now and then (don't check all the time)
            if (DateTime.Now < _nextLogCheck || emailId == 0)
                return;

            // Find the expiration settings for the log
            string expirationSettings = null;
            var n = new Node(emailId);
            if (n.Id > 0 && n.NodeTypeAlias == EnmUmbracoDocumentTypeAlias.ActionEmail.ToString())
            {
                // Search the current e-mail node
                var p = n.GetProperty(EnmUmbracoPropertyAlias.logExpiration.ToString());
                if (p != null && !String.IsNullOrEmpty(p.Value))
                    expirationSettings = p.Value;
                else
                {
                    // No settings found. Try and load the expiration settings from the parent (global settings)
                    p = n.Parent.GetProperty(EnmUmbracoPropertyAlias.logExpiration.ToString());
                    if (p != null)
                        expirationSettings = p.Value;
                }
            }

            // Have any expiration settings been found?
            if (!String.IsNullOrEmpty(expirationSettings))
            {

                // Strip the expiration number from the value (a bit hacky, but there is no value field for the default umbraco dropdownlist data type)
                int expirationNumber;
                string input = new String(expirationSettings.TakeWhile(x => Char.IsDigit(x)).ToArray());
                if (int.TryParse(input, out expirationNumber))
                {
                    // Determine the timespan type (week, month, year)
                    if (expirationSettings.EndsWith("week", StringComparison.OrdinalIgnoreCase))
                        PurgeOldLogs(emailId, DateTime.Now.AddDays(expirationNumber * -7));
                    if (expirationSettings.EndsWith("month", StringComparison.OrdinalIgnoreCase))
                        PurgeOldLogs(emailId, DateTime.Now.AddMonths(expirationNumber * -1));
                    if (expirationSettings.EndsWith("year", StringComparison.OrdinalIgnoreCase))
                        PurgeOldLogs(emailId, DateTime.Now.AddYears(expirationNumber * -1));
                    _nextLogCheck = DateTime.Now.AddHours(1);
                }
            }
        }

        /// <summary>
        /// Determines if logging is globally enabled for PerplexMail. This setting is configurable in the web.config.
        /// </summary>
        public static bool IsLoggingGloballyEnabled
        {
            get
            {
                return ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_DISABLE_LOG] != "true";
            }
        }

        public static T FromJSON<T>(string json)
        {
            try
            {
                return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<T>(json);
            }
            catch
            {
                return default(T);
            }
        }

        public static string ToJSON(object data)
        {
            var s = new System.Web.Script.Serialization.JavaScriptSerializer();
            try
            {
                var json = s.Serialize(data);
                return System.Text.RegularExpressions.Regex.Replace(json, "\"\\\\/Date\\((.*?)\\)\\\\/\"", ReplaceDates); 
            }
            catch
            {
                return null;
            }
        }

        static string ReplaceDates(System.Text.RegularExpressions.Match m)
        {
            long tmp;
            if (long.TryParse(m.Groups[1].Value, out tmp))
                return "\"" + UnixTimestampToDateTime(tmp).ToString("yyyy-MM-ddTHH\\:mm\\:ss") + "\"";
            else
                return String.Empty;
        }

        public static DateTime UnixTimestampToDateTime(long unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(unixTimeStamp).ToLocalTime();
        }

        public static bool IsStatisticsModuleEnabled
        {
            get
            {
                return HttpContext.Current.ApplicationInstance.Modules.AllKeys.Contains(Constants.WEBCONFIG_MODULE_NAME);
            }
        }
    }
}