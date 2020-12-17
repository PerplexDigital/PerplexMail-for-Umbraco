using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using HtmlAgilityPack;
using System.IO;
using System.Configuration;
using PerplexMail.Models;
using umbraco.NodeFactory;
using umbraco.interfaces;
using System.Data.Common;

namespace PerplexMail
{
    /// <summary>
    /// The central class to the email package. It can be configured to send emails.
    /// </summary>
    public class Email
    {
        MailMessage _mail;
        SmtpClient _smtp;
        string _headTag = "<style>html,body,p,div{font-family:Verdana,Arial;font-size:11px;}</style>";
        string _smtpUser;
        List<EmailTag> _listOfTags;

        bool IsLoggingDisabled
        {
            get
            {
                if (Helper.IsLoggingGloballyEnabled)
                {
                    var nMail = new Node(EmailId);
                    if (nMail.Id > 0)
                    {
                        var p = nMail.GetProperty(EnmUmbracoPropertyAlias.disableAutomatedLogging.ToString());
                        if (p != null && p.Value == "1")
                            return true;
                    }
                }
                return false;
            }
        }

        Email(List<EmailTag> values = null)
        {
            Initialize(values);
        }

        /// <summary>
        /// Create an email object, but load the basic configuration from an umbraco email node
        /// </summary>
        /// <param name="umbracoEmailNodeId">The Umbraco node id of the email to load the configuration from</param>
        /// <param name="values">Contains the values to replace the tags in the email with</param>
        Email(int umbracoEmailNodeId, List<EmailTag> values = null)
        {
            // Input validation
            if (umbracoEmailNodeId == 0)
                throw new ArgumentException(String.Format("Invalid Umbraco Node Id '{0}' (must be >= 1000)", umbracoEmailNodeId), "mailNodeID");

            // Validate the specified node
            Node nMail = new Node(umbracoEmailNodeId);
            if (nMail == null || nMail.Id == 0)
                throw new Exception(String.Format("The specified Umbraco Node with id '{0}' does not exist", umbracoEmailNodeId));

            Initialize(values);

            // Optional: If SMTP credentials have been entered from Umbraco, use them instead of the default web.config's settings
            var nEmails = nMail.Parent;
            var p = nEmails.GetProperty("smtpHost");
            if (p != null && !String.IsNullOrEmpty(p.Value))
                _smtp.Host = p.Value;
            p = nEmails.GetProperty("smtpLogin");
            if (p != null && !String.IsNullOrEmpty(p.Value))
            {
                _smtp.UseDefaultCredentials = false;
                var c = new System.Net.NetworkCredential();
                c.UserName = p.Value;
                p = nEmails.GetProperty("smtpPassword");
                if (p != null && !String.IsNullOrEmpty(p.Value))
                    c.Password = p.Value;
                _smtp.Credentials = c;
                int port;
                p = nEmails.GetProperty("smtpPort");
                if (p != null && int.TryParse(p.Value, out port) && port > 0)
                    _smtp.Port = port;
            }

            // Default: NO header data or elements
            HeadTag = "";

            // Add the email template ID so it will get logged to the DB
            EmailId = nMail.Id;
            // Load sender
            From = GetEmailAdressesFromUmbracoProperty(nMail, EnmUmbracoPropertyAlias.from).FirstOrDefault();
            // Load to receivers
            foreach (var emailadres in GetEmailAdressesFromUmbracoProperty(nMail, EnmUmbracoPropertyAlias.to))
                To.Add(emailadres);
            // Load replyto list
            foreach (var emailadres in GetEmailAdressesFromUmbracoProperty(nMail, EnmUmbracoPropertyAlias.replyTo))
                ReplyToList.Add(emailadres);
            // Load CC recipients
            foreach (var emailadres in GetEmailAdressesFromUmbracoProperty(nMail, EnmUmbracoPropertyAlias.cc))
                CC.Add(emailadres);
            // Load BCC recipients
            foreach (var emailadres in GetEmailAdressesFromUmbracoProperty(nMail, EnmUmbracoPropertyAlias.bcc))
                BCC.Add(emailadres);

            // Get the subject header and replace tags
            Subject = ReplaceTags(nMail.GetProperty(EnmUmbracoPropertyAlias.subject));

            // Load the body template (including parent template if selected)
            Body = BuildEmailBody(nMail);

            // Determine if any attachment is to be sent with the e-mail
            String attachments = nMail.GetProperty(EnmUmbracoPropertyAlias.attachments);
            if (!String.IsNullOrEmpty(attachments))
                // Loop through all picked Umbraco media items
                foreach (String attachment in attachments.Split(','))
                {
                    int attachmentId;
                    // Valid media item?
                    if (!string.IsNullOrEmpty(attachment) && int.TryParse(attachment.Trim(), out attachmentId) && attachmentId > 0)
                    {
                        var a = new Attachment(attachmentId);
                        if (!a.IsEmpty)
                            Attachments.Add(a);
                    }
                }

            // If any text-only version has been specified, include it
            AlternativeView = ReplaceTags(nMail.GetProperty(EnmUmbracoPropertyAlias.textVersion));

            // Determine if any template has been selected for the email
            string templateID = nMail.GetProperty(EnmUmbracoPropertyAlias.emailTemplate);
            int id;
            if (!string.IsNullOrEmpty(templateID) && int.TryParse(templateID, out id))
            {
                // A basic template has been selected for this e-mail, fetch the node
                Node nTemplate = new Node(id);
                // Attempt to load the umbraco node property containing the CSS
                string CSS = nTemplate.GetProperty(EnmUmbracoPropertyAlias.css);
                if (!String.IsNullOrEmpty(CSS))
                {
                    if (IsInlinerDisabled(nMail))
                        // We are not inlining our CSS, so place the style tag in the header of the e-mail.
                        // The recipient will have to interpret the stylesheet for the e-mail
                        HeadTag = "<style>" + CSS + "</style>";
                }
            }
        }

        static bool _supportsIdent = true;
        static bool _supportsAutoInc = true;
        /// <summary>
        /// Attempt to predict the next logID this e-mail will get.
        /// Note: This function is not 100% reliable as emails sent simultaniously may result in the email getting a different (higher) log ID.
        /// </summary>
        /// <returns>The log ID of the next email that will be sent</returns>
        string GetNextLogMailId()
        {

            if (_supportsIdent)
                try
                {
                    return Sql.ExecuteSql(Constants.SQL_QUERY_GET_NEXT_LOGID, CommandType.Text);
                }
                catch (Exception ex)
                {
                    // If the database starts rambling about IDENT_CURRENT, it most likely means we can't use IDENT_CURRENT in our query (for example in SQL CE)
                    if (ex.Message.Contains("IDENT_CURRENT"))
                        _supportsIdent = false;
                }

            if (_supportsAutoInc)
                try
                {
                    // Attempt to pull the information out of the schema
                    return Sql.ExecuteSql("SELECT AUTOINC_NEXT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "' AND COLUMN_NAME = 'id'", CommandType.Text);
                }
                catch (Exception ex)
                {
                    _supportsAutoInc = false;
                }

            // Do a max value on the column (ugly method)
            var result = Sql.ExecuteSql("SELECT MAX([id]) + 1 FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "]", CommandType.Text);
            if (String.IsNullOrEmpty(result))
            {
                // The table is empty, just reseed and return the next expected value (1)
                Sql.ExecuteSql("ALTER TABLE[" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] ALTER COLUMN [id] IDENTITY(1, 1)", CommandType.Text);
                return "1";
            }
            else
                return result;
        }

        public string SmtpHost { get { return _smtp.Host; } set { _smtp.Host = value; } }

        public string Body { get { return _mail.Body; } set { _mail.Body = value; } }

        /// <summary>
        /// Specify custom header elements for the email as required. The default contains a basic CSS <style> tag
        /// </summary>
        public string HeadTag { get { return _headTag; } set { _headTag = value; } }

        public string AlternativeView { get; set; }

        public int EmailId { get; set; }

        public MailAddress From { get { return _mail.From; } set { if (value != null)_mail.From = value; } }

        public MailAddressCollection To { get { return _mail.To; } }

        public MailAddressCollection CC { get { return _mail.CC; } }

        public MailAddressCollection BCC { get { return _mail.Bcc; } }

        public MailAddressCollection ReplyToList { get { return _mail.ReplyToList; } }

        List<Attachment> _Attachments = new List<Attachment>();
        public List<Attachment> Attachments { get { return _Attachments; } }

        public int LogId { get; private set; }

        public string Subject
        {
            get
            {
                return _mail.Subject;
            }
            set
            {
                _mail.Subject = value.Replace("\r\n", "").Replace(char.ConvertFromUtf32(10), " ").Replace(char.ConvertFromUtf32(12), " ").Replace(char.ConvertFromUtf32(13), " ").Replace("  ", " ").Replace("&nbsp;", " ");
            }
        }

        void Initialize(List<EmailTag> values)
        {
            // Mail settings
            _mail = new MailMessage();
            //_mail.From = new MailAddress("info@perplex.nl"); // Het moet ergens vandaan komen
            _mail.IsBodyHtml = true;
            // SMTP settings
            _smtp = new SmtpClient();
            //_smtp.Host = "localhost";
            //_smtp.UseDefaultCredentials = true;
            _listOfTags = values ?? new List<EmailTag>();
        }

        string ReplaceTags(string input)
        {
            return Helper.ParseTags(input, _listOfTags);
        }

        public class PerplexEmailadres
        {
            public string Address { get; set; }
            public string DisplayName { get; set; }
        }

        IEnumerable<MailAddress> GetEmailAdressesFromUmbracoProperty(INode node, EnmUmbracoPropertyAlias alias, bool recursive = true)
        {
            bool atleastOneResult = false;
            var p = node.GetProperty(alias.ToString());
            if (p != null && !String.IsNullOrEmpty(p.Value))
            {
                var data = Helper.FromJSON<List<PerplexEmailadres>>(p.Value);
                if (data != null)
                    foreach (var email in data)
                    {
                        MailAddress m = null;
                        try
                        {
                            if (!String.IsNullOrEmpty(email.Address))
                            {
                                var emailaddress = ReplaceTags(email.Address);
                                if (!String.IsNullOrEmpty(emailaddress) && System.Text.RegularExpressions.Regex.IsMatch(emailaddress, Constants.REGEX_EMAIL))
                                    m = new MailAddress(emailaddress, ReplaceTags(email.DisplayName));
                            }
                        }
                        catch { } // "Jammer joh" exception
                        if (m != null)
                        {
                            atleastOneResult = true;
                            yield return m;
                        }
                    }
            }
            if (!atleastOneResult && recursive)
                foreach (var email in GetEmailAdressesFromUmbracoProperty(node.Parent, alias, false))
                    yield return email;
        }

        string GetContentAsGrid(Node nMail)
        {
            string gridHTML = null;
            // Try as grid
            var pc = Umbraco.Web.UmbracoContext.Current?.ContentCache?.GetById(nMail.Id);
            string alias = EnmUmbracoPropertyAlias.body.ToString();
            if (pc != null)
            {
                var p = pc.GetProperty(alias);
                if (p == null)
                    return null;
                var model = p.Value;
                if (model == null)
                    return null;
                var cc = new System.Web.Mvc.ControllerContext
                {
                    RequestContext = Umbraco.Web.UmbracoContext.Current.HttpContext.Request.RequestContext
                };

                var viewContext = new System.Web.Mvc.ViewContext(cc, new FakeView(), new System.Web.Mvc.ViewDataDictionary(model), new System.Web.Mvc.TempDataDictionary(), new StringWriter());
                if (!viewContext.RouteData.Values.ContainsKey("controller"))
                    viewContext.RouteData.Values.Add("controller", "cheese");

                var htmlHelper = new System.Web.Mvc.HtmlHelper(viewContext, new System.Web.Mvc.ViewPage());
                gridHTML = Umbraco.Web.GridTemplateExtensions.GetGridHtml(htmlHelper, pc, alias).ToHtmlString();
            }
            return gridHTML;
        }

        class FakeView : System.Web.Mvc.IView
        {
            public void Render(System.Web.Mvc.ViewContext viewContext, TextWriter writer)
            {
            }
        }

        Node GetTemplateNode(Node nMail)
        {
            // Determine if any basic template has been selected for this email
            string templateId = nMail.GetProperty(EnmUmbracoPropertyAlias.emailTemplate);
            int nodeId;
            if (int.TryParse(templateId, out nodeId))
                return new Node(nodeId);
            else
                return null;
        }

        bool IsInlinerDisabled(Node nMail)
        {
            var nTemplate = GetTemplateNode(nMail);
            if (nTemplate != null && nTemplate.Id > 0)
                return nTemplate.GetProperty(EnmUmbracoPropertyAlias.disableCSSInlining) == "1";
            else
                return false;
        }

        /// <summary>
        /// Loads the unparsed body text for the e-mail and returns it as a single (unparsed) string.
        /// </summary>
        /// <param name="nMail">The e-mail node</param>
        /// <returns>Unparsed body string</returns>
        string BuildEmailBody(Node nMail)
        {
            #region 1. Build the basic template of the email. Here we will determine where the email content will be placed and we will parse all email tags
            // Retrieve the template from the email node in Umbraco
            string bodyContent;
            try
            {
                // Try as grid
                bodyContent = GetContentAsGrid(nMail);
            }
            catch (Exception ex)
            {
                // Non-grid
                bodyContent = nMail.GetProperty(EnmUmbracoPropertyAlias.body);
                System.Diagnostics.Debug.WriteLine("Error loading 'Body' content: " + ex.ToString());
            }

            // Determine if any basic template has been selected for this email
            var nTemplate = GetTemplateNode(nMail);
            string CSS = String.Empty;
            if (nTemplate != null && nTemplate.Id > 0)
            {
                // Place the email body in the basic template. The special contenttag in the baisc template will be used for this
                var template = nTemplate.GetProperty(EnmUmbracoPropertyAlias.templateMail);
                if (String.IsNullOrEmpty(template))
                    // Perhaps this Umbraco installation still has the old template alias
                    template = nTemplate.GetProperty(EnmUmbracoPropertyAlias.template);
                if (!String.IsNullOrEmpty(template))
                    bodyContent = template.Replace(Constants.TEMPLATE_CONTENT_TAG, bodyContent);
                // Inline CSS?
                if (nTemplate.GetProperty(EnmUmbracoPropertyAlias.disableCSSInlining) != "1")
                    // We will inline the CCS!
                    CSS = nTemplate.GetProperty(EnmUmbracoPropertyAlias.css);
            }

            // Determine the ID of the email we are about to send.
            // This is tricky because the log ID is an identity column. Furthermore if two emails are sent at the same time, the ID might be lower then it should be.
            // For now we will make an assumption regarding the ID, but this should be done in a more reliable fashion in the future
            string newID = GetNextLogMailId();

            bodyContent = bodyContent.Replace("href=\"/umbraco/" + Constants.TAG_PREFIX, "href=\"" + Constants.TAG_PREFIX)
                                     .Replace("src=\"/umbraco/" + Constants.TAG_PREFIX, "src=\"" + Constants.TAG_PREFIX);

            // Parse e-mail body
            bodyContent = Helper.ParseText(bodyContent, _listOfTags);
            #endregion

            #region 2.We are done parsing the basic HTML template of the document. Now proces all links and images
            // Load the entire HTML body as an HTML document.
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(bodyContent);

            // Process all hyperlinks
            ProcessHyperlinks(ref doc, newID);

            // Process all images
            ProcessImages(ref doc);

            // Inline all specified CSS styles
            if (!String.IsNullOrEmpty(CSS))
                InlineCSS(CSS, ref doc);
            #endregion

            // We are done performing all the required HTML magic
            bodyContent = doc.DocumentNode.OuterHtml;

            // Perform an extra check to make sure that browser-specific comment tags are also parsed (which unfortunately cannot be handled by the HTML agility pack)
            bodyContent = bodyContent.Replace("src=\"/", "src=\"" + Helper.WebsiteUrl + "/")
                                     .Replace("href=\"/", "href=\"" + Helper.WebsiteUrl + "/")
                                     .Replace("background=\"/", "background=\"" + Helper.WebsiteUrl + "/");

            // Determine of there are any "view online" hyperlinks in the document. These require a special webversion URL.
            if (bodyContent.Contains(Constants.TEMPLATE_WEBVERSIONURL_TAG))
            {
                // Generate a simple authentication hash based on the ID of the email
                String authenticationHash = HttpUtility.UrlEncode(Security.Hash(newID));
                // Replace the webversion tag with the real URL. The client may open the URL and can then see the webversion of the email.
                bodyContent = bodyContent.Replace(Constants.TEMPLATE_WEBVERSIONURL_TAG, Helper.GenerateWebversionUrl(newID));
            }

            #region 3. Place a tracking tag at the bottom of the email. When the image is loaded from the server it will trigger a "view" for the email
            if (Helper.IsStatisticsModuleEnabled && !IsLoggingDisabled) // Both the module and logging need to be enabled for this feature to work
            {
                //var tag = doc.CreateElement("img");
                //tag.Attributes.Add("src", Helper.WebsiteUrl + Constants.STATISTICS_IMAGE + "?&" + Constants.STATISTICS_QUERYSTRINGPARAMETER_MAILID + "=" + newID + "&" + Constants.STATISTICS_QUERYSTRINGPARAMETER_ACTION + "=" + EnmAction.view.ToString());
                //tag.Attributes.Add("style", "opacity:0"); // Make the tag not "visible", but it still needs to be rendered by the client (else the image won't get loaded) so make sure not to use display:none
                //doc.DocumentNode.LastChild.AppendChild(tag);

                string statTracker = "<img style=\"opacity:0;\" src=\"" + Helper.WebsiteUrl + Constants.STATISTICS_IMAGE + "?" + Constants.STATISTICS_QUERYSTRINGPARAMETER_MAILID + "=" + newID + "&" + Constants.STATISTICS_QUERYSTRINGPARAMETER_ACTION + "=" + EnmAction.view.ToString() + "&ipignore=true" + "\" />";
                bodyContent += statTracker; // Place the stat tracker image at the end
            }
            #endregion

            // Parsinc complete
            return bodyContent;
        }

        /// <summary>
        /// This method takes raw CSS style code and an HTML document as input and embeds all the specified CSS code in the document.
        /// It attempts to apply the styles on the HTML elements in a similar way as you would expect to find in a modern browser.
        /// Please take note that CSS3 is not supported and the usage of psuedo elements (:after, :hover, etc) should be avoided.
        /// This is because all modern email clients (outlook, gmail) only render basic CSS2 styling as well.
        /// </summary>
        /// <param name="CSS">The raw CSS code to parse</param>
        /// <param name="doc">The document to apply the styling to</param>
        protected static void InlineCSS(String CSS, ref HtmlDocument doc)
        {
            // Remove all single line comments from the code (starts with //)
            var regels = CSS.Split('\n');
            for (int i = regels.Length - 1; i >= 0; i--)
                if (regels[i].Trim().StartsWith("//"))
                    regels[i] = String.Empty;

            // Collapse all the CSS into a single line (remove all enters)
            CSS = String.Join(String.Empty, regels).Replace("\r","");

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
            var list = new List<cssStyle>();
            foreach (Match matchCssStyle in _matchStyles.Matches(CSS))
                if (matchCssStyle.Success)
                {
                    var cssSelector = matchCssStyle.Groups["selector"].Value.Trim();
                    var cssStyle = matchCssStyle.Groups["style"].Value.Trim();
                    // In case multiple selectors of the same style
                    foreach (var subSelector in cssSelector.Split(','))
                    {
                        var selector = subSelector;
                        if (!String.IsNullOrEmpty(selector))
                            selector = selector.Trim();
                        
                        if (!String.IsNullOrEmpty(selector) &&
                            !selector.Contains('[') && // CSS styling through attributes are not supported
                            !selector.Contains(':')) // CSS-psuedo elements are not supported
                        {
                            var css = new cssStyle();
                            css.selector = selector.Trim();
                            css.xpath = Helper.CssToXpath(selector.Trim());
                            css.style = cssStyle.Replace("\"", "'");
                            list.Add(css);
                        }
                    }
                }

            // Determine the weight of every CSS rule specified
            int ordernumber = 0;
            foreach (var c in list)
            {
                // Count the number of classes and identifiers used (. and #)
                int numberOfCssClassesAndIdentifiers = c.selector.Count(cs => cs == '#' || cs == '.');
                // Determine the weight. Each ID selector counts as 100. Each class selector counts as 10
                c.weight = 0; // Begin with 0 weight
                c.weight += c.selector.Count(l => l == '#') * 100; // Each ID selector adds 100 points
                c.weight += c.selector.Count(l => l == '.') * 10; // Each class adds 10 points
                // Determine the number of keywords that are present in t  he CSS style
                int numberOfKeywords = c.selector.Split(new String[] { " ", ".", "#" }, StringSplitOptions.RemoveEmptyEntries).Count(woord =>
                                woord.Split(new [] { ':', '['})[0].All(l => char.IsLetterOrDigit(l) || l == '-' || l == '_'));
                // We will make a rough assumption: each class and id uses one keyword as well. So substract the total number of keywords with the number of classes and IDs.
                // The remainder is the number of "element selectors" used.
                int numberOfElementSelectors = numberOfKeywords - numberOfCssClassesAndIdentifiers;
                if (numberOfElementSelectors > 0) // Confirm we have a positive number of elements
                    c.weight += numberOfElementSelectors; // We will consider each element to add 1 point of weight.
                c.ordernumber = ordernumber++;
            }

            // Sort all the styles by weight. The selector with the highest weight will be processed first, and if they have equal weight, by last-to-first appearance order
            foreach (var c in list.OrderByDescending(cs => cs.weight).ThenByDescending(cs => cs.ordernumber))
            {
                try
                {
                    var query = doc.DocumentNode.SelectNodes(c.xpath);
                    if (query != null)
                        foreach (HtmlNode htmlElement in query)
                        {
                            if (htmlElement.Attributes["style"] == null)
                                // The element does not have any associated style attribute. Simply set the CSS rule's style value for the element's style.
                                htmlElement.Attributes.Add("style", c.style);
                            else if (String.IsNullOrEmpty(htmlElement.Attributes["style"].Value))
                                // The style tag was found, but it is empty. Simply set the CSS rule's style value for the element's style.
                                htmlElement.Attributes.Add("style", c.style);
                            else
                            {
                                // The style tag was found, and it already contains CSS style information. These values may NOT be overwritten. (Previously added styles always have a higher weight value). *** Unless the style contains the keyword "!important"
                                // Generate a list of CSS styles properties that are currently applied to the element
                                var currentStyles = htmlElement.Attributes["style"].Value.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(k => new CssStyleValue(k.Split(':')[0].Trim().ToLower(), k.Substring(k.IndexOf(':') + 1))).ToList();
                                // Iterate over every style definition
                                foreach (var cssStyle in c.style.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Reverse()) // From last to first in priority!
                                {
                                    // Get the css style name (for example width, height, etc...)
                                    string keyword = cssStyle.Split(':')[0].Trim().ToLower();
                                    // And the value (for example 1px)
                                    string value = cssStyle.Substring(cssStyle.IndexOf(':') + 1).Trim(); // The value is everything after the colon :
                                    if (String.IsNullOrEmpty(keyword) || String.IsNullOrEmpty(value))
                                        // If the keyowrd or value could not be deterined, skip it.
                                        continue;

                                    // Digest CCS by splitting and adding it as needed
                                    DigestCCS(currentStyles, keyword, value);
                                }
                                // Reapply the style to the element. Join all keys and values with : and join all styles with ;
                                htmlElement.Attributes["style"].Value = String.Join(";", currentStyles.Select(csv => csv.keyword + ":" + csv.value));
                            }
                        }
                }
                catch
                {
                }
            }

            try
            {
                // Final pass: parse all TD for (v)aligns and bgcolors
                foreach (HtmlNode htmlElement in doc.DocumentNode.SelectNodes("//table").Concat(doc.DocumentNode.SelectNodes("//td").Concat(doc.DocumentNode.SelectNodes("//tr")).Concat(doc.DocumentNode.SelectNodes("//p")))) // TODO: combine xpaths into one
                {
                    if (htmlElement.Attributes["style"] != null)
                    {
                        var currentStyles = htmlElement.Attributes["style"].Value.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(k => new CssStyleValue(k.Split(':')[0].Trim().ToLower(), k.Substring(k.IndexOf(':') + 1))).ToList();
                        for (int i = currentStyles.Count - 1; i >= 0; i--)
                        {
                            var style = currentStyles[i];
                            if (htmlElement.Name != "table")
                            {
                                if (htmlElement.Name != "tr")
                                {
                                    if (style.keyword.ToLower() == "vertical-align")
                                    {
                                        if (htmlElement.Attributes["valign"] == null)
                                            htmlElement.Attributes.Add("valign", style.value.Trim());
                                        else if (style.value.ToLower().Contains("!important"))
                                            htmlElement.Attributes["valign"].Value = style.value.Split('!')[0].Trim();
                                    }
                                }
                                if (style.keyword.ToLower() == "text-align")
                                {
                                    if (htmlElement.Attributes["align"] == null)
                                        htmlElement.Attributes.Add("align", style.value.Trim());
                                    else if (style.value.ToLower().Contains("!important"))
                                        htmlElement.Attributes["align"].Value = style.value.Split('!')[0].Trim();
                                }
                            }
                            if (style.keyword.ToLower() == "background" || style.keyword.ToLower() == "background-color")
                            {
                                if (htmlElement.Attributes["bgcolor"] == null)
                                    htmlElement.Attributes.Add("bgcolor", style.value.Trim());
                                else if (style.value.ToLower().Contains("!important"))
                                    htmlElement.Attributes["bgcolor"].Value = style.value.Split('!')[0].Trim();
                            }
                        }

                        // Reapply the style to the element. Join all keys and values with : and join all styles with ;
                        htmlElement.Attributes["style"].Value = String.Join(";", currentStyles.Select(csv => csv.keyword + ":" + csv.value));
                    }
                }
            }
            catch
            {

            }

            try
            {
                foreach (HtmlNode htmlElement in doc.DocumentNode.SelectNodes("//*"))
                    if (htmlElement.Attributes["style"] != null)
                        htmlElement.Attributes["style"].Value = htmlElement.Attributes["style"].Value.Replace("!important", "");
            }
            catch
            {
            }
        }

        static void DigestCCS(List<CssStyleValue> list, string keywordToAdd, string valueToAdd)
        {
            // Determine if the style has already been applied to the element
            var t = list.FirstOrDefault(k => k.keyword == keywordToAdd);
            
            // Has this style already been applied?
            if (t == null)
            {
                // Make sure no style preceeded this style that overrules it (for example padding > padding-top)
                var baseKeyword = keywordToAdd.Split('-')[0];

                var baseStyle = list.FirstOrDefault(x => x.keyword == baseKeyword);
                if (baseStyle != null)
                {
                    // A base style has been added before this one. Only overrule if the !important keyword is present 
                    if (!baseStyle.value.Contains("!important") && valueToAdd.Contains("!important"))
                        // Append the ipmortant element to the back
                        list.Add(new CssStyleValue(keywordToAdd, valueToAdd));
                }
                else
                    // Style not added yet, add the specified CSS style to the element
                    list.Insert(0, new CssStyleValue(keywordToAdd, valueToAdd));
            }
            else
            {
                if (t.value.IndexOf("!important", StringComparison.OrdinalIgnoreCase) == -1 &&  // ... and the current style does not have the !important keyword...
                    valueToAdd.IndexOf("!important", StringComparison.OrdinalIgnoreCase) >= 0) // ... and the current style we are trying to apply has the !important keyword... 
                    t.value = valueToAdd; // ... then overwrite the style!
            }
        }

        class CssStyleValue
        {
            public CssStyleValue(string keyword, string value)
            {
                this.keyword = keyword; this.value = value;
            }

            public string keyword { get; set; }
            public string value { get; set; }
        }

        class cssStyle
        {
            public int weight { get; set; }
            public int ordernumber { get; set; }
            public string selector { get; set; }
            public string style { get; set; }
            public string xpath { get; set; }
        }

        static void ProcessImages(ref HtmlDocument doc)
        {
            // Iterate over all src and background attributes used in the email
            var query = doc.DocumentNode.SelectNodes("//*[@src or @background]");
            if (query != null)
                foreach (HtmlNode element in query)
                {
                    // Does the attribute contain a relative URL (starts with a /)
                    var attribute = element.Attributes["src"];
                    if (element.Attributes["src"] == null)
                        attribute = element.Attributes["background"];
                    if (attribute != null)
                    {
                        if (attribute.Value.StartsWith("/"))
                            // Convert the URL to an absolute URL
                            attribute.Value = Helper.WebsiteUrl + attribute.Value;
                    }
                }
        }

        void ProcessHyperlinks(ref HtmlDocument doc, string mailID)
        {
            var moduleEnabled = Helper.IsStatisticsModuleEnabled && !IsLoggingDisabled; // Both the module and logging need to be enabled for this feature to work

            // Determine which hyperlinks we will check
            var query = doc.DocumentNode.SelectNodes("//a");
            if (query != null)
            {
                // Prepare the regex with which we will look for Umbraco RTE urls
                var regexUmbracoLink = new Regex(@"/{localLink:([^}]*)\}");
                var regexNumber = new Regex(@"\d+");
                // Iterate over all hyperlinks that we found
                foreach (HtmlNode hyperlink in query)
                {
                    // Ensure that the hyperlink element has a target = _blank attribute/value
                    if (hyperlink.Attributes["target"] != null)
                        hyperlink.Attributes["target"].Value = "_blank";
                    else
                        hyperlink.Attributes.Add("target", "_blank");

                    // Check if the hyperlink has a HREF attribute
                    if (hyperlink.Attributes["href"] != null)
                    {
                        // Does the HREF attribute value contain an Umbraco-RTE-style link?
                        string url = hyperlink.Attributes["href"].Value;
                        var m = regexUmbracoLink.Match(url);
                        int nodeID;

                        // Match found? Does the link contains an Umbraco node ID?
                        if (m.Success && int.TryParse(regexNumber.Match(m.Value).Value, out nodeID) && nodeID >= 1000)
                            // Replace the umbraco link HREF with the nice URL value of the Node
                            url = umbraco.library.NiceUrl(nodeID);

                        if (!String.IsNullOrEmpty(url))
                        {
                            // Verify the url does not contain a PerplexMail tag (even though this should already have been parsed earlier!)
                            if (url.Contains(Constants.TAG_PREFIX))
                                // The hyperlink still contains a PerplexMail tag for some reason. Remove all leading characters before the tag to be sure.
                                url = url.Substring(url.IndexOf('['));
                            else
                            {
                                // Sometimes the Umbraco RTE (Richtext Editor) has a bad habbit of placing an unnecessery slash '/' at the start of the href value.
                                // This is because the RTE tries to "make the url valid" by making the URL relative.
                                // Check if the unnecessery slash is present
                                if (url.StartsWith("//") || url.StartsWith("/http"))
                                    // Remove it!
                                    url = url.Substring(1);

                                // Is the PerplexMail module enabled?
                                if (moduleEnabled)
                                    // Convert the hyperlink URL to first visit our statistics URL, which will then redirect to the final target URL
                                    url = Helper.WebsiteUrl + "?i=" + mailID + // The Log ID of the email
                                                              "&a=" + EnmAction.click.ToString() + // The action is a link CLICK
                                                              "&v=" + HttpUtility.UrlEncode(HttpUtility.HtmlDecode(url)); // Url encode the (final) target URL
                                else
                                {
                                    // The statistics module is not enabled: Make sure we convert relative URL's to absolute URL's by prepending the protocol and the hostname
                                    if (url.Length > 0 && url[0] == '/')
                                        url = Helper.WebsiteUrl + url;
                                }
                            }
                            // Place the (modified) url back in the hyperlink
                            hyperlink.Attributes["href"].Value = url;
                        }
                    }
                }
            }
        }

        public void SetSmtpCredentials(string username, string password)
        {
            _smtp.Credentials = new System.Net.NetworkCredential(username, password);
            _smtp.UseDefaultCredentials = false;
            _smtpUser = username;
        }

        string SaveAttachmentsAndGenerateStorageString()
        {
            bool doNotSaveDynamicAttachments = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS] == "true";

            var sb = new System.Text.StringBuilder();
            string logId = GetNextLogMailId();
            for (int i = 0; i < _Attachments.Count; i++)
            {
                var a = _Attachments[i];
                if (!a.IsEmpty)
                {
                    if (i > 0)
                        sb.Append(',');

                    if (a.Stream != null)
                    {
                        // Indien er in de web.config is gespecificeerd dat attachments opgeslagen mogen worden, zet ze dan in de dynamische map
                        if (!doNotSaveDynamicAttachments)
                        {
                            // Determine the filename for the attachment
                            string filepath = a.FileDirectory + logId + "_" + a.FileName;
                            bool retry = true;
                        retry:
                            try
                            {
                                using (var fs = new FileStream(filepath, FileMode.Create))
                                {
                                    a.Stream.Seek(0, SeekOrigin.Begin);
                                    a.Stream.CopyTo(fs);
                                }
                            }
                            catch (DirectoryNotFoundException)
                            {
                                if (retry)
                                    // Could not find a part of the path
                                    try
                                    {
                                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath));
                                        retry = false;
                                        goto retry;
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        // We are not allowed to create the directory at this location :(
                                        continue;
                                    }
                                else
                                    throw;
                            }
                            sb.Append(filepath);
                        }
                    } else if (a.UmbracoMediaId > 0)
                        sb.Append(a.UmbracoMediaId);
                    else
                        sb.Append(a.FileDirectory + a.FileName);
                }
            }
            return sb.ToString();
        }

        int Log(Exception ex = null)
        {
            // Has logging been disabled through Umbraco?
            if (IsLoggingDisabled)
                // Do not log the e-mail
                return 0;
            bool retry = true;
            retry:
            try
            {
                // If the default encryptionkey is set, encrypt some PerplexLogMail values by default
                bool requiresEncryption = ConfigurationManager.AppSettings[Constants.WEBCONFIG_SETTING_DISABLE_ENCRYPTION] != "true";

                string To = this.To.ToString();
                string From = this.From.ToString();
                string Subject = this.Subject;
                string Body = this.Body;

                // Build ReplyTo/CC/BCC list
                string CC = null, BCC = null, ReplyTo = null;
                if (ReplyToList != null && ReplyToList.Count > 0)
                    ReplyTo = ReplyToList.ToString();
                if (this.CC != null && this.CC.Count > 0)
                    CC = this.CC.ToString();
                if (this.BCC != null && this.BCC.Count > 0)
                    BCC = this.BCC.ToString();
                ReplyTo = ReplyTo ?? String.Empty;
                CC = CC ?? String.Empty;
                BCC = BCC ?? String.Empty;

                if (requiresEncryption)
                {
                    if (!String.IsNullOrEmpty(To))
                        To = Security.Encrypt(To);
                    if (!String.IsNullOrEmpty(From))
                        From = Security.Encrypt(From);
                    if (!String.IsNullOrEmpty(Subject))
                        Subject = Security.Encrypt(Subject);
                    if (!String.IsNullOrEmpty(Body))
                        Body = Security.Encrypt(Body);
                    if (!String.IsNullOrEmpty(ReplyTo))
                        ReplyTo = Security.Encrypt(ReplyTo);
                    if (!String.IsNullOrEmpty(CC))
                        CC = Security.Encrypt(CC);
                    if (!String.IsNullOrEmpty(BCC))
                        BCC = Security.Encrypt(BCC);
                }
                string website = "", specificurl = "";
                if (HttpContext.Current != null)
                {
                    try
                    {
                        website = HttpContext.Current.Request.ServerVariables["server_name"];
                        specificurl = HttpContext.Current.Request.ServerVariables["URL"];

                    }
                    catch (Exception)
                    {
                    }
                }

                var parameters = new {
                    to = To,
                    from = From,
                    subject = Subject,
                    body = Body,
                    replyTo = ReplyTo,
                    cc = CC,
                    bcc = BCC,
                    attachment = Attachments.Count > 0 ? SaveAttachmentsAndGenerateStorageString() : "",
                    host = _smtp.Host ?? String.Empty,
                    userID = _smtpUser ?? String.Empty,
                    website = website,
                    specificurl = specificurl,
                    ip = Helper.GetIp(),
                    emailID = EmailId,
                    alternativeView = AlternativeView ?? String.Empty,
                    exception = ex != null ? ex.InnerException != null ? ex.Message + "; " + ex.InnerException.Message : ex.Message : "",
                    isEncrypted = requiresEncryption
                };
                return Sql.ExecuteSqlWithIdentity(Constants.SQL_QUERY_LOGMAIL, CommandType.Text, parameters);
            }
            catch (DbException sqlEx)
            {
                if (retry && Helper.HandleSqlException(sqlEx))
                {
                    retry = false;
                    goto retry;
                }
                else
                    throw;
            }
        }

        public static void ClearLog(int emailNodeId = 0)
        {
            if (emailNodeId == 0)
                Sql.ExecuteSql("DELETE " + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG, CommandType.Text);
            else
                Sql.ExecuteSql("DELETE " + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + " WHERE [emailID] = @id", CommandType.Text, new { id = emailNodeId });
        }

        #region Alle varianten van send e-mail

        /// <summary>
        /// Send the email. The email will also logged automtaically in this call (unless disabled from Umbraco)
        /// </summary>
        /// <returns>The log ID of the email that was sent</returns>
        public int SendEmail()
        {
            // Remove old log entries
            Helper.RemoveOldLogEntries(EmailId); // TODO: Replace with SQL trigger?

            // Has the email already been sent? We check this by checking the LOG status
            if (LogId > 0)
                // The email has already been sent
                return LogId;
            else
                if (To.Count == 0 && CC.Count == 0 && BCC.Count == 0)
                    throw new Exception("Could not determine the primary recipient for the email. Please specify a 'to', 'cc' or 'bcc' recipient.");

            try
            {
                if (Body == null || !Body.StartsWith("<!DOCTYPE"))
                    Body = "<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\"><html><head>" + _headTag + "</head><body>" + Body + "</body></html>";
                else
                {
                    int index = Body.IndexOf("</head>");
                    if (index >= 0)
                        Body = Body.Insert(index, _headTag);
                }

                if (String.IsNullOrEmpty(AlternativeView))
                    _mail.Body = Body;
                else
                {
                    AlternateView plainView = AlternateView.CreateAlternateViewFromString(AlternativeView, null, "text/plain");
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(Body, null, "text/html");
                    _mail.AlternateViews.Add(plainView);
                    _mail.AlternateViews.Add(htmlView);
                }

                _mail.Attachments.Clear();
                foreach (var a in Attachments)
                {
                    if (a.Stream != null)
                        // Use the supplied stream
                        _mail.Attachments.Add(new System.Net.Mail.Attachment(a.Stream, a.FileName));
                    else
                        // Find the file on the harddisk
                        _mail.Attachments.Add(new System.Net.Mail.Attachment(a.FileDirectory + a.FileName));
                }
                _smtp.Send(_mail);

                // Dispose of SmtpClient
                _smtp.Dispose();

                return Log();
            }
            catch (Exception ex)
            {
                try
                {
                    Log(ex);
                    throw;
                }
                catch (Exception exInner)
                {
                    string message = exInner.Message + " (logging error) <== " + ex.Message;
                    if (ex.InnerException != null)
                        message += " <== " + ex.InnerException.Message;
                    throw new Exception(message, exInner);
                }
            }
        }

        /// <summary>
        /// Resend any email that has previously been sent and logged. The email to resend is based on the email log ID, so only emails present in the log table can be resent.
        /// </summary>
        /// <param name="logmailID">The email log ID</param>
        /// <param name="recipientEmail">(optioneel) The alternative recipient of the email that will be resent. If left null or empty, the original recipient's emailaddress will be used</param>
        /// <param name="includeCC">Should the email also be sent to the original CC recipients?</param>
        /// <param name="includeBCC">Should the email also be sent to the original CC recipients?</param>
        /// <returns>The log ID of the email that was sent</returns>
        public static int ReSendEmail(int logmailID, string recipientEmail = null, bool includeCC = false, bool includeBCC = false)
        {
            var logMail = LogEmail.Get(logmailID);

            var m = new Email();
            m.To.Add(new MailAddress(recipientEmail ?? logMail.to));
            m.From = new MailAddress(logMail.from);
            if (!String.IsNullOrEmpty(logMail.replyTo))
                m.ReplyToList.Add(new MailAddress(logMail.replyTo));
            if (!String.IsNullOrEmpty(logMail.cc))
                m.CC.Add(new MailAddress(logMail.cc));
            if (!String.IsNullOrEmpty(logMail.bcc))
                m.BCC.Add(new MailAddress(logMail.bcc));
            if (!String.IsNullOrEmpty(logMail.host))
                m.SmtpHost = logMail.host;
            if (!String.IsNullOrEmpty(logMail.userID))
                m.SetSmtpCredentials(logMail.userID, null); // Dit is natuurlijk knudde want zonder het wachtwoord kom je niet ver
            m.Subject = logMail.subject;
            m.Body = logMail.body;
            m.EmailId = logMail.emailID;
            m.AlternativeView = logMail.alternativeView;
            if (!String.IsNullOrEmpty(logMail.attachment))
                foreach (String attachment in logMail.attachment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrEmpty(attachment)) // redundant
                    {
                        // Is het toevallig een Umbraco Media Node Id?
                        int tmp;
                        if (attachment.All(c => Char.IsDigit(c)))
                        {
                            if (int.TryParse(attachment, out tmp))
                            {
                                // Ah, dat is makkelijk, gewoon het bestand uit Umbraco vissen
                                string filename = Helper.GetUmbracoMediaFile(tmp, true);
                                if (!String.IsNullOrEmpty(filename))
                                    m.Attachments.Add(new Attachment(filename));
                            }
                        }
                        else
                            // Fysiek pad op de schijf
                            m.Attachments.Add(new Attachment(attachment));
                    }
            return m.SendEmail();
        }


        /// <summary>
        /// Send a email based on the configuration of an Umbraco email node.
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <returns>The ID of the logged email</returns>
        public static int SendUmbracoEmail(int umbracoEmailNodeId)
        {
            return SendUmbracoEmail(umbracoEmailNodeId, null, null);
        }

        /// <summary>
        /// Send a email based on the configuration of an Umbraco email node.
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <param name="values">List of tags render in the email</param>
        /// <returns>The ID of the logged email</returns>
        public static int SendUmbracoEmail(int umbracoEmailNodeId, List<EmailTag> values)
        {
            return SendUmbracoEmail(umbracoEmailNodeId, values, null);
        }

        /// <summary>
        /// Send a email based on the configuration of an Umbraco email node.
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <param name="values">List of tags render in the email</param>
        /// <param name="attachments">The list of attachments that are sent with the email</param>
        /// <returns>The ID of the logged email</returns>
        public static int SendUmbracoEmail(int umbracoEmailNodeId,List<EmailTag> values, IEnumerable<Attachment> attachments)
        {
            return CreateUmbracoEmail(umbracoEmailNodeId, values, attachments).SendEmail();
        }

        /// <summary>
        /// Send a email based on the configuration of an Umbraco email node.
        /// If any email adresses are specified in parameters from, replyto, to, cc or bcc, the email's Umbraco configuration is IGNORED and instead the provided paramter will be used.
        /// If any of the email parameters contains NULL, it will be skipped and the email's Umbraco settings will be used instead.
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <param name="values">List of tags render in the email</param>
        /// <param name="attachments">The list of attachments that are sent with the email</param>
        /// <param name="from"></param>
        /// <param name="replyto"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <returns></returns>
        public static int SendUmbracoEmail(int umbracoEmailNodeId, List<EmailTag> values, IEnumerable<Attachment> attachments, MailAddress from, MailAddressCollection replyto, MailAddressCollection to, MailAddressCollection cc, MailAddressCollection bcc)
        {
            var m = CreateUmbracoEmail(umbracoEmailNodeId, values, attachments);
            if (from != null)
                m.From = from;

            // Add [reply to] recipients
            if (replyto != null && replyto.Count > 0)
            {
                m.ReplyToList.Clear();
                foreach (var ma in replyto)
                    m.ReplyToList.Add(ma);
            }

            // Add [to] recipients
            if (to != null && to.Count > 0)
            {
                m.To.Clear();
                foreach (var ma in to)
                    m.To.Add(ma);
            }

            // Add [cc] recipients
            if (cc != null && cc.Count > 0)
            {
                m.CC.Clear();
                foreach (var ma in cc)
                    m.CC.Add(ma);
            }

            // Add [bcc] recipients
            if (bcc != null && bcc.Count > 0)
            {
                m.BCC.Clear();
                foreach (var ma in bcc)
                    m.BCC.Add(ma);
            }

            // Send the email
            return m.SendEmail();
        }

        /// <summary>
        /// Send a email based on the configuration of an Umbraco email node.
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <param name="recipient"> The recipient of the email. The recipient may also be specified on the Umbraco email node</param>
        /// <param name="values">List of tags render in the email</param>
        /// <param name="attachments">The list of attachments that are sent with the email</param>
        /// <returns>The ID of the logged email</returns>
        public static Email CreateUmbracoEmail(int umbracoEmailNodeId, List<EmailTag> values, IEnumerable<Attachment> attachments)
        {
            // Validate the ID
            if (umbracoEmailNodeId == 0)
                throw new ArgumentException("Invalid Umbraco Node Id (must be >= 1000)", "mailNodeID");

            // Validate e-mail node
            var nMail = new Node(umbracoEmailNodeId);
            if (nMail == null || nMail.Id == 0)
                throw new Exception("The specified Umbraco Node with id '" + umbracoEmailNodeId.ToString() + "' does not exist.");

            // Create an e-mail object based on the settings in the specified Umbraco email node
            var m = new Email(umbracoEmailNodeId, values);

            if (attachments != null)
                m.Attachments.AddRange(attachments);

            return m;
        }

        /// <summary>
        /// Send a test email based on the settings of the request object. This email is sent exclusively to the specified recipient parameter (CC and BCC are ignored)
        /// </summary>
        /// <param name="request">The object describing the test email to be sent</param>
        /// <returns>The ID of the logged email</returns>
        public static int SendUmbracoTestEmail(SendTestEmailRequest request)
        {
            if (request == null)
                return 0;
            else
                return SendUmbracoTestEmail(request.EmailNodeId, request.MailAddresses, request.Tags);
        }

        /// <summary>
        /// Send a test email based on the configuration of an Umbraco email node. This email is sent exclusively to the specified recipient parameter (CC and BCC are ignored)
        /// </summary>
        /// <param name="umbracoEmailNodeId">The ID of the configured Umbraco email node</param>
        /// <param name="recipients">The emailaddress to receive the test email</param>
        /// <param name="values">List of tags render in the email</param>
        /// <returns>The ID of the logged email</returns>
        public static int SendUmbracoTestEmail(int umbracoEmailNodeId, IEnumerable<MailAddress> recipients, List<EmailTag> values)
        {
            var m = new Email(umbracoEmailNodeId, values);

            m.To.Clear();
            foreach (var recipient in recipients)
                m.To.Add(recipient);
            m.CC.Clear();
            m.BCC.Clear();
            return m.SendEmail();
        }

        public static void DownloadEmail(int logmailID)
        {
            var logMail = LogEmail.Get(logmailID);

            var m = new Email();
            m.To.Add(new MailAddress(logMail.to));
            m.From = new MailAddress(logMail.from);
            if (!String.IsNullOrEmpty(logMail.replyTo))
                m.ReplyToList.Add(new MailAddress(logMail.replyTo));
            if (!String.IsNullOrEmpty(logMail.cc))
                m.CC.Add(new MailAddress(logMail.cc));
            if (!String.IsNullOrEmpty(logMail.bcc))
                m.BCC.Add(new MailAddress(logMail.bcc));
            if (!String.IsNullOrEmpty(logMail.host))
                m.SmtpHost = logMail.host;
            if (!String.IsNullOrEmpty(logMail.userID))
                m.SetSmtpCredentials(logMail.userID, null); // Dit is natuurlijk knudde want zonder het wachtwoord kom je niet ver
            m.Subject = logMail.subject;
            m.Body = logMail.body;
            m.EmailId = logMail.emailID;
            m.AlternativeView = logMail.alternativeView;
            if (!String.IsNullOrEmpty(logMail.attachment))
                foreach (String attachment in logMail.attachment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrEmpty(attachment)) // redundant
                    {
                        // Is the string actually an integer?
                        int tmp;
                        if (attachment.All(c => Char.IsDigit(c)))
                        {
                            if (int.TryParse(attachment, out tmp))
                            {
                                // Ah, that's convienent since it's an Umbraco Media Node Id. Try to get the media file from Umbraco
                                string filename = Helper.GetUmbracoMediaFile(tmp, true);
                                if (!String.IsNullOrEmpty(filename))
                                    m.Attachments.Add(new Attachment(filename));
                            }
                        }
                        else
                            // Fysical path on the harddisk
                            m.Attachments.Add(new Attachment(attachment));
                    }

            m._mail.Attachments.Clear();
            foreach (var a in m.Attachments)
            {
                if (a.Stream != null)
                    // Use the supplied stream
                    m._mail.Attachments.Add(new System.Net.Mail.Attachment(a.Stream, a.FileName));
                else
                    // Find the file on the harddisk
                    m._mail.Attachments.Add(new System.Net.Mail.Attachment(a.FileDirectory + a.FileName));
            }

            // Filename in the beowser as "nodename - logId - email"
            string downloadFilename = new Node(m.EmailId).Name + " - " + logmailID.ToString();

            if (m.To != null && m.To.Count > 0)
                downloadFilename += " - " + m.To[0].Address;

            downloadFilename = Helper.SanitizeFilename(downloadFilename);
            Helper.SendMailMessageToBrowser(m._mail, downloadFilename);
        }

        #endregion
    }
}