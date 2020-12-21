using System;

namespace PerplexMail
{
    /// <summary>
    /// An Enumeration that determines the status of the email that was sent.
    /// </summary>
    public enum EnmStatus
    {
        All = -1,
        Sent = 0,
        Error = 1,
        Viewed = 2,
        Clicked = 3,
        Webversion = 4
    }

    /// <summary>
    /// An Enumeration containing all the Umbraco document types used by the PerplexMail.
    /// </summary>
    public enum EnmUmbracoDocumentTypeAlias
    {
        PerplexMail,
        EmailBase,
        EmailFolder,
        PerplexMailFolder,
        EMailTemplate,
        EMailTemplateFolder,
    }

    /// <summary>
    /// An Enumeration containing all Umbraco property aliases that are used (from various document types) by the PerplexMail.
    /// </summary>
    public enum EnmUmbracoPropertyAlias
    {
        umbracoNaviHide,
        internalRemarks,
        emailTemplate,
        subject,
        body,
        textVersion,
        from,
        replyTo,
        sendTestEMail,
        statistics,
        attachments,
        pDF,
        to,
        cc,
        bcc,
        disableAutomatedLogging,
        logExpiration,
        template, // Only kept for backwards compatibility, use templateMail
        templateMail,
        css,
        disableCSSInlining,
    }

    /// <summary>
    /// Contains the actions that have been observed for an email sent by PerplexMail.
    /// </summary>
    public enum EnmAction
    {
        view,
        click,
        webversion
    }

    /// <summary>
    /// Contains all the application-wide constants that are used by PerplexMail.
    /// </summary>
    internal static partial class Constants
    {
        /// <summary>
        /// The name of the SQL database table that is used to store email log entries.
        /// </summary>
        public const string SQL_TABLENAME_PERPLEXMAIL_LOG = "perplexMailLog";

        /// <summary>
        /// The name of the SQL database table that is used to store email statistic entries.
        /// </summary>
        public const string SQL_TABLENAME_PERPLEXMAIL_STATISTICS = "perplexMailStatistics";

        /// <summary>
        /// The content tag used in the basic email template that will be used to determine where the email content should be rendered
        /// </summary>
        public const string TEMPLATE_CONTENT_TAG = TAG_PREFIX + "content" + TAG_SUFFIX;

        /// <summary>
        /// The PerplexMail tag to be used to specify the webversion URL
        /// </summary>
        public const string TEMPLATE_WEBVERSIONURL_TAG = TAG_PREFIX + "webversion" + TAG_SUFFIX;
        
        /// <summary>
        /// The virtual URL to the email package folder.
        /// </summary>
        public const string PACKAGE_ROOT = "/App_Plugins/PerplexMail/";

        /// <summary>
        /// The name of the hidden image embedded in emails sent by PerplexMail. Whenver it is loadded, a view is registered for the email.
        /// </summary>
        public const string STATISTICS_IMAGE = PACKAGE_ROOT + "s.gif";
        
        /// <summary>
        /// A basic regular expression to validate an e-mailadres
        /// </summary>
        public const string REGEX_EMAIL = "\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*";

        /// <summary>
        /// The name of the querystring parameter that contains the querystring security hash.
        /// </summary>
        public const string STATISTICS_QUERYSTRINGPARAMETER_AUTH = "auth";

        /// <summary>
        /// The name of the querystring parameter that contains the logging ID of the email to register the action for.
        /// </summary>
        public const string STATISTICS_QUERYSTRINGPARAMETER_MAILID = "i";

        /// <summary>
        /// The name of the querystring parameter that contains the name of the action associated with the event.
        /// </summary>
        public const string STATISTICS_QUERYSTRINGPARAMETER_ACTION = "a";

        /// <summary>
        /// The name of the querystring paramter that contains the custom value of the action (if applicable).
        /// </summary>
        public const string STATISTICS_QUERYSTRINGPARAMETER_VALUE = "v";

        /// <summary>
        /// The opening tag for any tags in the email templates.
        /// </summary>
        public const string TAG_PREFIX = "[#";

        /// <summary>
        /// The closing tag for any tags in the email templates.
        /// </summary>
        public const string TAG_SUFFIX = "#]";

        /// <summary>
        /// The folder that will contain all the attachments to be stored
        /// </summary>
        public const string ATTACHMENTS_FOLDER = "~/App_Data/PerplexMail/";

        /// <summary>
        /// The key name of the "secret" AppSettings entry used to encrypt all data for the PerplexMail package.
        /// </summary>
        public const string WEBCONFIG_SETTING_PERPLEXMAIL_KEY = "PerplexMailMasterKey";

        /// <summary>
        /// The web.config AppSettings key that determines if dynamic attachments should be stored.
        /// </summary>
        public const string WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS = "perplexMailDisableDynamicAttachmentStorage";

        /// <summary>
        /// The web.config AppSettings key that determines if logging is globally disabled
        /// </summary>
        public const string WEBCONFIG_SETTING_DISABLE_LOG = "perplexMailDisableLog";

        /// <summary>
        /// The web.config AppSettings key that determines if encryption is globally disabled
        /// </summary>
        public const string WEBCONFIG_SETTING_DISABLE_ENCRYPTION = "perplexMailDisableEncryption";

        /// <summary>
        /// The name of the PerplexMail statistics HttpModule in the web.config
        /// </summary>
        public const string WEBCONFIG_MODULE_NAME = "PerplexMailStatisticsHttpModule";

        /// <summary>
        /// The classname of the PerplexMail statistics module (including the namespace)
        /// </summary>
        public const string PERPLEXMAIL_STATISTICSMODULE_CLASS = "PerplexMail.HttpStatisticsModule";

        /// <summary>
        /// This query returns "1" if a table with the name @tblName exists
        /// </summary>
        public const string SQL_QUERY_TABLE_EXISTS = "SELECT TOP 1 '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = @tblName";

        /// <summary>
        /// This query returns "1" if a stored procedure with the name @spName exists
        /// </summary>
        public const string SQL_QUERY_SP_EXISTS = "SELECT TOP 1 '1' FROM sys.procedures WHERE name = @spName";

        /// <summary>
        /// SQL query which removes all PerplexMail log entries that have expired according to the settings in Umbraco.
        /// </summary>
        public const string SQL_QUERY_REMOVE_OLDLOGS = "DELETE [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] WHERE[dateSent] < @maximumLogDate AND[emailID] = @emailID";

        /// <summary>
        /// SQL query that adds an entry to the PerplexMail table.
        /// </summary>
        public const string SQL_QUERY_LOGMAIL = "INSERT INTO [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] ([to],[from],replyTo,cc,bcc,[subject],[body],alternativeView,attachment,emailID,website,host,userID,specificurl,ip,exception, isEncrypted) VALUES (@to,@from,@replyTo,@cc,@bcc,@subject,@body,@alternativeView,@attachment,@emailID,@website,@host,@userID,@specificurl,@ip,@exception, @isEncrypted)";

        /// <summary>
        /// Gets the log entry for a sent e-mail based on it's id (parameter 'emailName')
        /// </summary>
        public const string SQL_QUERY_GET_LOGMAIL = "SELECT TOP 1 l.*, n.[text] AS [emailName] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] l LEFT JOIN [umbracoNode] n ON l.[emailID] = n.[id] WHERE l.[id] = @id";

        /// <summary>
        /// Gets the viewcount total for all e-mails.
        /// </summary>
        public const string SQL_QUERY_GET_VIEWCOUNT = "SELECT COUNT(*) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] p WHERE [id] IN (SELECT DISTINCT [emailID] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] s WHERE p.[id] = s.[emailID] AND s.[action] IS NOT NULL)";

        /// <summary>
        /// Gets the viewcount total for a specific e-mail type (parameter 'emailID')
        /// </summary>
        public const string SQL_QUERY_GET_VIEWCOUNT_BYTYPE = SQL_QUERY_GET_VIEWCOUNT + " AND p.[emailID] = @emailID";

        public const string SQL_QUERY_SUM_EMAILS = "SELECT COUNT(*) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "]";
        public const string SQL_QUERY_SUM_EMAILS_BYTYPE = SQL_QUERY_SUM_EMAILS + " WHERE [emailID] = @templateId";
        public const string SQL_QUERY_GET_ATTACHMENT = "SELECT [attachment] from [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] WHERE [id] = @id";

        /// <summary>
        /// Sql query that gets statistics for the statistics overview page in Umbraco.
        /// </summary>
        public const string SQL_QUERY_GET_STATISTICS =
@"SELECT 
	result.*, v.[viewed], c.[clicked], w.[webversion]
FROM 
	(SELECT 
		p.[id], p.[to], p.[emailID], p.[dateSent], p.isEncrypted, p.[from], p.[replyTo] ,p.[cc] ,p.[bcc] ,p.[subject] ,p.[body], p.[exception], 
		CASE WHEN n.[text] IS NULL THEN '-' ELSE n.[text] END AS [emailName]
	FROM 
		[" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + @"] p 
    LEFT JOIN 
		[umbracoNode] n 
	ON 
		p.[emailID] = n.[id]
	WHERE 
		(p.[dateSent] between @fromDate AND @toDate)
	AND 
		(@receiver = '%%' OR (p.[to] LIKE @receiver OR p.[cc] LIKE @receiver OR p.[bcc] LIKE @receiver))
	AND 
		(@text = '%%' OR (p.[subject] LIKE @text OR p.[body] LIKE @text))
	AND 
		(@emailID = 0 OR @emailID = p.emailID)
	) result 
LEFT JOIN 
    (SELECT [emailID], MAX(addDate) AS [viewed] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE [action] = 'view' GROUP BY [emailID]) v 
ON 
    result.[id] = v.[emailID]
LEFT JOIN 
    (SELECT [emailID], MAX(addDate) AS [clicked] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE [action] = 'click' GROUP BY [emailID]) c 
ON 
    result.[id] = c.[emailID]
LEFT JOIN 
    (SELECT [emailID], MAX(addDate) AS [webversion] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE [action] = 'webversion' GROUP BY [emailID]) w
ON 
    result.[id] = w.[emailID]
WHERE 
	(@status = -1)
OR 
	(@status = 0 AND v.[viewed] IS NULL AND c.[clicked] IS NULL AND w.[webversion] IS NULL AND ISNULL(result.[exception],'') = '') 
OR 
	(@status = 1 AND ISNULL(result.[exception],'') <> '') 
OR 
	(@status = 2 AND (v.[viewed] IS NOT NULL OR c.[clicked] IS NOT NULL OR w.[webversion] IS NOT NULL)) 
OR 
	(@status = 3 AND c.[clicked] IS NOT NULL)
OR 
	(@status = 4 AND w.[webversion] IS NOT NULL) 
ORDER BY 
	CASE WHEN @orderBy = 'dateSent DESC' THEN [dateSent] ELSE NULL END DESC, 
	CASE WHEN @orderBy = 'dateSent ASC' THEN [dateSent] ELSE NULL END ASC, 
	CASE WHEN @orderBy = 'emailName DESC' THEN [emailName] ELSE NULL END DESC, 
	CASE WHEN @orderBy = 'emailName ASC' THEN [emailName] ELSE NULL END ASC";
        /*
@"SELECT * FROM (SELECT p.[id], p.[to], p.[emailID], p.[dateSent], p.isEncrypted, p.[from], p.[replyTo] ,p.[cc] ,p.[bcc] ,p.[subject] ,p.[body],"
            //(SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE emailID = p.id AND [action] = 'view') as [viewed],
            //(SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE emailID = p.id AND [action] = 'click') as [clicked],
            //(SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] WHERE emailID = p.id AND [action] = 'webversion') as [webversion],
            + @" p.[exception], CASE WHEN n.[text] IS NULL THEN '-' ELSE n.[text] END as [emailName] FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + @"] p 
            LEFT JOIN [umbracoNode] n ON p.[emailID] = n.[id] WHERE 
((@fromDate IS NULL OR @toDate IS NULL) OR (p.[dateSent] between @fromDate AND @toDate)) AND (@receiver IS NULL OR p.[to] LIKE '%' + @receiver + '%' OR p.[cc] like '%' + @receiver + '%' OR p.[bcc] like '%' + @receiver + '%') AND (@text IS NULL OR p.[subject] LIKE '%' + @text + '%' OR p.[body] LIKE '%' + @text + '%') AND (@emailID = p.emailID OR @emailID IS NULL) ) result WHERE @status IS NULL OR @status = -1 OR (@status = 0 AND result.Viewed IS NULL AND result.Clicked IS NULL AND result.Webversion IS NULL and result.exception IS NULL) OR (@status = 1 AND result.exception IS NOT NULL) OR (@status = 2 AND (result.Viewed IS NOT NULL OR result.Clicked IS NOT NULL OR result.Webversion IS NOT NULL)) OR (@status = 3 AND result.Clicked IS NOT NULL) OR (@status = 4 AND result.Webversion IS NOT NULL) ORDER BY CASE WHEN @orderBy = 'dateSent DESC' THEN [dateSent] ELSE NULL END DESC, CASE WHEN @orderBy = 'dateSent ASC' THEN [dateSent] ELSE NULL END ASC, CASE WHEN @orderBy = 'emailName DESC' THEN [emailName] ELSE NULL END DESC, CASE WHEN @orderBy = 'emailName ASC' THEN [emailName] ELSE NULL END ASC";
*/
        /// <summary>
        /// Sql query that inserts an email statistic (event).
        /// </summary>
        public const string SQL_QUERY_ADD_STATISTICS = "INSERT INTO [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] (emailID, action, value, addDate, ip, useragent) VALUES (@emailID, @action, @value, GETDATE(), @ip, @useragent)";

        /// <summary>
        /// Note: It's not actually possible to get the next ID, we simply make a "guess" what the next ID will be based on the seed + autoincrement value.
        ///       The query is slightly more complicated due to the fact that if a table is empty the current identity returned defaults to the seed (1) + increment(1) = 2 instead of 1.
        /// </summary>
        public const string SQL_QUERY_GET_NEXT_LOGID = "SELECT IDENT_CURRENT('" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "') + CASE WHEN(SELECT COUNT(1) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "]) = 0 THEN 0 ELSE IDENT_INCR('" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "') END";

        /// <summary>
        /// The SQL query to create the PerplexMail logging table
        /// </summary>
        public const string SQL_QUERY_CREATE_TABLE_LOG =
            @"CREATE TABLE [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + @"] (
	            [id] [bigint] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + @"] PRIMARY KEY,
	            [to] [nvarchar](255) NOT NULL,
	            [from] [nvarchar](255) NOT NULL,
	            [replyTo] [nvarchar](255) NULL,
	            [cc] [nvarchar](255) NULL,
	            [bcc] [nvarchar](255) NULL,
	            [subject] [ntext] NOT NULL,
	            [body] [ntext] NOT NULL,
	            [alternativeView] [ntext] NULL,
	            [attachment] [nvarchar](1000) NULL,
	            [emailID] [int] NULL,
	            [dateSent] [datetime] NOT NULL DEFAULT (getdate()),
	            [website] [nvarchar](100) NOT NULL,
	            [host] [nvarchar](100) NOT NULL,
	            [userID] [nvarchar](100) NOT NULL,
	            [specificUrl] [nvarchar](255) NOT NULL,
	            [ip] [nvarchar](30) NOT NULL,
	            [exception] [nvarchar](4000) NULL,
	            [isEncrypted] [bit] NOT NULL
            )";

        /// <summary>
        /// The SQL query to create the PerplexMail statistics table
        /// </summary>
        public const string SQL_QUERY_CREATE_TABLE_STATISTICS =
            @"CREATE TABLE [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] (
	            [id] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + @"] PRIMARY KEY,
	            [emailID] [bigint] NOT NULL REFERENCES [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + @"] ([id]) ON DELETE CASCADE,
	            [action] [nvarchar](50) NOT NULL,
	            [value] [nvarchar](255) NULL,
	            [addDate] [datetime] NOT NULL DEFAULT getdate(),
	            [ip] [nvarchar](30) NOT NULL,
	            [useragent] [nvarchar](1024) NOT NULL
            )";
    }
}