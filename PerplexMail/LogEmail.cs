using PerplexMail.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace PerplexMail
{ 
    public class LogEmail
    {
        public Int64 id { get; set; }
        public string from { get; set; }
        public string replyTo { get; set; }

        public string to { get; set; }
        public string cc { get; set; }
        public string bcc { get; set; }

        public IEnumerable<string> toList 
        { 
            get 
            {
                if (!String.IsNullOrEmpty(to))
                    return to.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
                else
                    return null;
            } 
        }
        public IEnumerable<string> ccList
        { 
            get 
            {
                if (!String.IsNullOrEmpty(cc))
                    return cc.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
                else
                    return null;
            } 
        }
        public IEnumerable<string> bccList
        { 
            get 
            {
                if (!String.IsNullOrEmpty(bcc))
                    return bcc.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
                else
                    return null;
            } 
        }

        public string subject { get; set; }
        public string body { get; set; }
        public string alternativeView { get; set; }
        public string attachment { get; set; }
        public List<AttachmentPreview> attachmentPreview
        {
            get
            {
                var result = new List<AttachmentPreview>();
                if (!String.IsNullOrEmpty(attachment))
                {
                    var data = attachment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data != null)
                        for (int i = 0; i < data.Length; i++)
			            {
                            string a = data[i];
                            // Is het toevallig een Umbraco Media Node Id?
                            int tmp;
                            if (a.All(c => Char.IsDigit(c)))
                            {
                                if (int.TryParse(a, out tmp))
                                {
                                    // Ah, dat is makkelijk, gewoon het bestand uit Umbraco vissen
                                    string filename = Helper.GetUmbracoMediaFile(tmp);
                                    if (!String.IsNullOrEmpty(filename))
                                    {
                                        var ap = new AttachmentPreview();
                                        ap.id = id;
                                        ap.mediaUrl = filename;
                                        ap.order = i;
                                        ap.name = System.IO.Path.GetFileName(filename);
                                        result.Add(ap);
                                    }
                                }
                            }
                            else
                            {
                                var ap = new AttachmentPreview();
                                ap.id = id;
                                ap.name = System.IO.Path.GetFileName(a);
                                ap.order = i;
                                result.Add(ap);
                            }
			            }
                }
                return result;
            }
        }
        public int emailID { get; set; }
        public string emailName { get; set; }
        public DateTime dateSent { get; set; }
        public string website { get; set; }
        public string host { get; set; }
        public string userID { get; set; }
        public string specificUrl { get; set; }
        public string ip { get; set; }
        public string exception { get; set; }
        public bool isEncrypted { get; set; }
        public DateTime viewed { get; set; }
        public int views { get; set; }
        public DateTime webversion { get; set; }
        public int webviews { get; set; }
        public DateTime clicked { get; set; }
        public int clicks { get; set; }
        public List<EnmStatus> Status
        {
            get
            {
                var result = new List<EnmStatus>();
                // Bekeken?
                if (viewed != default(DateTime))
                    result.Add(EnmStatus.Viewed);
                // Webversie?
                if (webversion != default(DateTime))
                    result.Add(EnmStatus.Webversion);
                // Geklikt?
                if (clicked != default(DateTime))
                    result.Add(EnmStatus.Clicked);
                // Error?
                if (!String.IsNullOrEmpty(exception))
                    result.Add(EnmStatus.Error);
                // Geen interactie?
                if (result.Count == 0)
                    result.Add(EnmStatus.Sent);
                return result;
            }
        }

        public static int GetEmailViewCount(int currentEmailTemplateId = 0)
        {
            try
            {
                string result = String.Empty;
                if (currentEmailTemplateId > 0)
                    result = Sql.ExecuteSql(Constants.SQL_QUERY_GET_VIEWCOUNT_BYTYPE, CommandType.Text, new { emailID = currentEmailTemplateId });
                else
                    result = Sql.ExecuteSql(Constants.SQL_QUERY_GET_VIEWCOUNT, CommandType.Text);
                int tmp = 0;
                int.TryParse(result, out tmp);
                return tmp;
            }
            catch
            {
                return 0;
            }
        }

        public static int GetEmailSendCount(int currentEmailTemplateId = 0)
        {
            try
            {
                string result = String.Empty;
                if (currentEmailTemplateId > 0)
                    result = Sql.ExecuteSql(Constants.SQL_QUERY_SUM_EMAILS_BYTYPE, CommandType.Text, new { templateId = currentEmailTemplateId } );
                else
                    result = Sql.ExecuteSql(Constants.SQL_QUERY_SUM_EMAILS, CommandType.Text);
                int tmp = 0;
                int.TryParse(result, out tmp);
                return tmp;
            }
            catch
            {
                return 0;
            }
        }

        public static List<LogEmail> Search(GetMailStatisticsRequest request)
        {
            if (request == null) return null; // Lege request? Dan krijg je niks terug!
            bool retry = true;
            retry:
            try
            {
                var parameters = new
                {
                    fromDate = request.FilterDateFrom,
                    toDate = request.FilterDateTo,
                    status = (int)request.FilterStatus,
                    emailID = request.CurrentNodeId,
                    orderBy = request.OrderBy,
                    receiver = "%%",
                    text = "%%",
                };

                var emails = Sql.ExecuteSql<LogEmail>(Constants.SQL_QUERY_GET_STATISTICS, System.Data.CommandType.Text, parameters);

                foreach (var email in emails)
                {
                    // Decrypt some values if the e-mail is encrypted
                    if (email.isEncrypted)
                    {
                        email.to = Security.Decrypt(email.to);
                        email.cc = Security.Decrypt(email.cc);
                        email.bcc = Security.Decrypt(email.bcc);
                        email.replyTo = Security.Decrypt(email.replyTo);
                        email.from = Security.Decrypt(email.from);
                        email.body = Security.Decrypt(email.body);
                        email.subject = Security.Decrypt(email.subject);
                    }
                }

                // Determine if we need to search the body of each
                if (!String.IsNullOrEmpty(request.SearchContent))
                    // Find all e-mails that contain the search string in their subject or body
                    emails = emails.Where(x => (!String.IsNullOrEmpty(x.subject) && x.subject.Contains(request.SearchContent)) || // Search Subject
                                                (!String.IsNullOrEmpty(x.body) && x.body.Contains(request.SearchContent))).ToList(); // Search Body
                
                // Determine if the user was searching for e-mailadresses
                if (!String.IsNullOrEmpty(request.SearchReceiver))
                    // Find all e-mails that contain the search string in their to/cc/bcc e-mailadresses
                    emails = emails.Where(x => (!String.IsNullOrEmpty(x.to) && x.to.Contains(request.SearchReceiver)) || // Search TO
                                               (!String.IsNullOrEmpty(x.cc) && x.cc.Contains(request.SearchReceiver)) || // Search CC
                                               (!String.IsNullOrEmpty(x.bcc) && x.bcc.Contains(request.SearchReceiver))).ToList(); // Search BCC
                return emails;
            }
            catch (DbException ex)
            {
                if (retry && Helper.HandleSqlException(ex))
                {
                    retry = false;
                    goto retry;
                }
                else
                    throw;
            }
        }

        /// <summary>
        /// Directly start the download for any given attachment. Calling this request will immediatly end the current request (if the file is found)
        /// </summary>
        /// <param name="request">The file (from the attachment) to send to the user</param>
        /// <returns>Directly ends the current request and sends the file to the user. Otherwise a string is returned with the error.</returns>
        public static string Download(RequestAttachment request)
        {
            if (request == null)
                // Invalid request (empty). Inform the user
                return "Invalid request"; 

        retry:
            try
            {
                // Voeg de datumrange toe
                string attachments = Sql.ExecuteSql(Constants.SQL_QUERY_GET_ATTACHMENT, CommandType.Text, new { id = request.logMailid });

                if (!String.IsNullOrEmpty(attachments))
                {
                    var data = attachments.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data != null && data.Length > request.order)
                    {
                        string attachment = data[request.order];
                        // Is het toevallig een Umbraco Media Node Id?
                        int tmp;
                        if (attachment.All(c => Char.IsDigit(c)))
                            if (int.TryParse(attachment, out tmp))
                                // Ah, dat is makkelijk, gewoon het bestand uit Umbraco vissen
                                attachment = Helper.GetUmbracoMediaFile(tmp, true);

                        if (!String.IsNullOrEmpty(attachment))
                            // Stream the file directly to browser (we do not want to expose any download URL)
                            Helper.StreamFileToBrowser(attachment);
                    }
                }
                // Attachment not found in the database. Report the problem to the client.
                return "Could not find the attachment";
            }
            catch (FileNotFoundException)
            {
                // File not found :( Report the problem to the client.
                return "The saved file for this attachment does not exist";
            }
            catch (DbException ex)
            {
                // Database exception. Try and handle the exception
                if (Helper.HandleSqlException(ex))
                    // Try again
                    goto retry;
                else
                    // The problem could not be solved. Report the problem to the client.
                    return "Internal database error while retrieving the attachment";
            }
        }

        static string[] _encryptedColumns = new[] { "to", "from", "replyTo", "cc", "bcc", "subject", "body" };
        static string[] _includeColumnsForExport = new[] { "id", "emailID", "dateSent", "to", "from", "replyTo", "cc", "bcc", "subject", "body", "exception", "emailName" };
        public static void Download(GetMailStatisticsRequest request)
        {
            if (request == null) 
                return; // Lege request? Dan krijg je niks terug!
        retry:
            try
            {
                var r = HttpContext.Current.Response;
                r.Clear();
                r.Buffer = true;
                r.ContentType = "application/vnd.ms-excel";
                r.AddHeader("content-disposition", "attachment;filename=" + "Export.xls");
                r.Charset = "";

                bool[] columnIsEncrypted = null;
                bool[] columnIsIncluded = null;
                var firstRow = true;
                var sbHeader = new StringBuilder("<tr>");
                // Bouw de response op
                var sbData = new StringBuilder();
                int isEncryptedColumnNumber = -1;
                foreach (IDataRecord dr in GetLogMailDataRecords(request))
                {
                    // Is this the first datarecord we are iterating over?
                    if (firstRow)
                    {
                        columnIsEncrypted = new bool[dr.FieldCount];
                        columnIsIncluded = new bool[dr.FieldCount];
                        // Determine which columns are available in the dataset by looping over all the columns and inspecting their names
                        for (int i = 0; i < dr.FieldCount; i++)
			            {
                            // Get the name of the currenet column
                            string colName = dr.GetName(i);
                            // Determine if the current column is an encrypted column
                            columnIsEncrypted[i] = _encryptedColumns.Contains(colName);
                            columnIsIncluded[i] = _includeColumnsForExport.Contains(colName);
                            // Determine if the isEncrypted column is present and what column number it is in
                            if (colName == "isEncrypted")
                                isEncryptedColumnNumber = i;
                            if (columnIsIncluded[i])
                                // Add the column to our excel header line (we don't want to include the isEncrypted column in our excel export)
                                sbHeader.AppendLine("<th>" + colName + "</th>");
			            }
                        firstRow = false;
                    }

                    // Determine if the column is encrypted
                    bool recordIsEncrypted = isEncryptedColumnNumber >= 0 ? dr.GetBoolean(isEncryptedColumnNumber) : false;

                    sbData.AppendLine("<tr>");                   
                    
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        // Only include columns that are specified in the include list
                        if (columnIsIncluded[i])
                        {
                            string text = dr.GetValue(i).ToString();
                            // If the current record is flagged as encrypted AND the current column is an encrypted column ...
                            if (recordIsEncrypted && columnIsEncrypted[i])
                                // ... then decrypt the column text
                                text = Security.Decrypt(text);
                            sbData.AppendLine("<td>&nbsp;" + HttpUtility.HtmlEncode(text) + "&nbsp;</td>");
                        }
                    }
                    sbData.AppendLine("</tr>");
                }
                sbHeader.AppendLine("</tr>");
                var sbResult = new StringBuilder();
                sbResult.AppendLine("<table>");
                sbResult.Append(sbHeader);
                sbResult.Append(sbData);
                sbResult.AppendLine("</table>");
                // Aan de hand van de BOM encoding character kan de browser weten wat voor een karakters er in het document staan.
                // Dit zorgt er eigenlijk voor dat gekke tekens zoals de euro er goed in komen te staan.
                r.Write("\uFEFF");
                r.Write(sbResult.ToString());
                r.End();
            }
            catch (DbException ex)
            {
                if (Helper.HandleSqlException(ex))
                    goto retry;
                else
                    throw;
            }
        }

        static IEnumerable<IDataRecord> GetLogMailDataRecords(GetMailStatisticsRequest request)
        {
            var parameters = new
            {
                fromDate = request.FilterDateFrom,
                toDate = request.FilterDateTo,
                status = (int)request.FilterStatus,
                emailID = request.CurrentNodeId > 0 ? (object)request.CurrentNodeId : DBNull.Value,
                text = !String.IsNullOrEmpty(request.SearchContent) && request.SearchContent != "null" ? (object)request.SearchContent : DBNull.Value,
                receiver = !String.IsNullOrEmpty(request.SearchReceiver) && request.SearchReceiver != "null" ? (object)request.SearchReceiver : DBNull.Value,
                orderBy = DBNull.Value
            };

            return Sql.CreateSqlDataEnumerator(Constants.SQL_QUERY_GET_STATISTICS, System.Data.CommandType.Text, parameters);
        }


        public static LogEmail Get(int logEmailId, bool preview = false)
        {
            retry:
            try
            {
                var e = PerplexMail.Sql.ExecuteSql<LogEmail>(Constants.SQL_QUERY_GET_LOGMAIL, CommandType.Text, new { id = logEmailId }).FirstOrDefault();
                if (e != null)
                {
                    // Decrypt some values if the e-mail is encrypted
                    if (e.isEncrypted)
                    {
                        e.to = Security.Decrypt(e.to);
                        e.cc = Security.Decrypt(e.cc);
                        e.bcc = Security.Decrypt(e.bcc);
                        e.replyTo = Security.Decrypt(e.replyTo);
                        e.from = Security.Decrypt(e.from);
                        e.body = Security.Decrypt(e.body);
                        e.subject = Security.Decrypt(e.subject);
                    }

                    if (preview)
                    {
                        string signature = "?i=" + e.id.ToString() + "&"; // <== If this querystring is found anywhere, it's the PerplexMail tracking querystring
                        // Simply remove the ID querystring from all statistic URLs in the HTML, that way the statistics module won't register
                        e.body = e.body.Replace(signature, "?");
                    }

                    #region Process style elements
                    // Process all <style> tags: remove all comments and add a container class in front of all CSS selectors.
                    // This is done so the styles are only applied to the preview and do not wreck our Umbraco layout.
                    var r = new System.Text.RegularExpressions.Regex("<style>(.*?)<\\/style>", System.Text.RegularExpressions.RegexOptions.Singleline);
                        foreach (System.Text.RegularExpressions.Match m in r.Matches(e.body))
                        {
                            if (m.Success)
                            {
                                const string containerClass = ".mail-content ";
                                string inputCSS = m.Groups[1].Value;
                                string CSS = inputCSS;
                                var sbOutput = new StringBuilder();

                                // Remove all single line comments from the code (starts with //)
                                var regels = CSS.Split('\n');
                                for (int i = regels.Length - 1; i >= 0; i--)
                                    if (regels[i].Trim().StartsWith("//"))
                                        regels[i] = String.Empty;

                                // Collapse all the CSS into a single line (remove all enters)
                                CSS = String.Join(String.Empty, regels);

                                // Remove all multi line comments ==> /* comment */
                                while (CSS.Contains("/*"))
                                {
                                    int startComment = CSS.IndexOf("/*");
                                    if (startComment == -1)
                                        break;
                                    int endComment = CSS.IndexOf("*/");
                                    if (endComment > CSS.Length - 1 || endComment == -1)
                                        endComment = CSS.Length - 1;
                                    endComment += 2; // +2 because you also want to include the closing tag */ itself
                                    CSS = CSS.Remove(startComment, endComment - startComment);
                                }

                                var _matchStyles = new Regex("\\s*(?<rule>(?<selector>[^{}]+){(?<style>[^{}]+)})",
                                                                    RegexOptions.IgnoreCase
                                                                    | RegexOptions.CultureInvariant
                                                                    | RegexOptions.IgnorePatternWhitespace
                                                                    | RegexOptions.Compiled);

                                foreach (Match matchCssStyle in _matchStyles.Matches(CSS))
                                    if (matchCssStyle.Success)
                                    {
                                        var cssSelector = matchCssStyle.Groups["selector"].Value.Trim();
                                        var cssStyle = matchCssStyle.Groups["style"].Value.Trim();
                                        
                                        string newSelector = String.Join(",",  cssSelector.Split(',').Select(x => containerClass + x));

                                        sbOutput.Append(newSelector).Append("{").Append(cssStyle).Append("}");
                                    }

                                e.body = e.body.Replace(inputCSS, sbOutput.ToString());
                            }
                        }
                        #endregion
                }
                return e;
            }
            catch (DbException ex)
            {
                if (Helper.HandleSqlException(ex))
                    goto retry;
                else
                    throw;
            }
        }
    }
}