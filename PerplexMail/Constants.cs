using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
        ActionEmail,
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
        template,
        css,
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
        /// The web.config AppSettings key that contains the vectorbytes string to use for logging encryption.
        /// </summary>
        public const string WEBCONFIG_SETTING_ENCRYPTION_VECTORBYTES = "perplexMailEncryptionInitVectorBytes";

        /// <summary>
        /// The web.config AppSettings key that contains the private key string to use for logging encryption.
        /// </summary>
        public const string WEBCONFIG_SETTING_ENCRYPTION_PRIVATEKEY = "perplexMailEncryptionKey";

        /// <summary>
        /// The folder that will contain all the attachments to be stored
        /// </summary>
        public const string ATTACHMENTS_FOLDER = "~/App_Data/PerplexMail/";

        /// <summary>
        /// The web.config AppSettings key that contains the private key string to use for hashing.
        /// </summary>
        public const string WEBCONFIG_SETTING_HASH_PRIVATEKEY = "perplexMailHashKey";

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
        /// SQL query that removes all traces of the PerplexMail package from the database.
        /// </summary>
        public const string SQL_QUERY_CLEANUP_DATABASE = "IF EXISTS(SELECT '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = '" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "') DROP TABLE " + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + " IF EXISTS(SELECT '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = '" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "') DROP TABLE " + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG;

        /// <summary>
        /// SQL query which removes all PerplexMail log entries that have expired according to the settings in Umbraco.
        /// </summary>
        public const string SQL_QUERY_REMOVE_OLDLOGS = "DECLARE @maximumLogDate DATETIME = NULL IF @numberOfWeeks IS NOT NULL SET @maximumLogDate = DATEADD(WEEK, -@numberOfWeeks, GETDATE()) ELSE IF @numberOfMonths IS NOT NULL SET @maximumLogDate = DATEADD(MONTH, -@numberOfMonths, GETDATE()) ELSE IF @numberOfYears IS NOT NULL SET @maximumLogDate = DATEADD(YEAR, -@numberOfYears, GETDATE()) IF @maximumLogDate IS NOT NULL AND @emailID > 0 DELETE [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] WHERE [dateSent] < @maximumLogDate AND [emailID] = @emailID END";

        /// <summary>
        /// SQL query that adds an entry to the PerplexMail table.
        /// </summary>
        public const string SQL_QUERY_LOGMAIL = "INSERT INTO [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] ([to],[from],replyTo,cc,bcc,[subject],[body],alternativeView,attachment,emailID,website,host,userID,specificurl,ip,exception, isEncrypted, salt) VALUES (@to,@from,@replyTo,@cc,@bcc,@subject,@body,@alternativeView,@attachment,@emailID,@website,@host,@userID,@specificurl,@ip,@exception, @isEncrypted, @salt) SELECT SCOPE_IDENTITY()";

        /// <summary>
        /// Sql query that gets statistics for the statistics overview page in Umbraco.
        /// </summary>
        public const string SQL_QUERY_GET_STATISTICS = "SELECT * FROM (SELECT p.[id], p.[to], p.[emailID], p.[dateSent], p.isEncrypted, p.salt, p.[from], p.[replyTo] ,p.[cc] ,p.[bcc] ,p.[subject] ,p.[body], (SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] WHERE emailID = p.id AND [action] = 'view') 'viewed', (SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] WHERE emailID = p.id AND [action] = 'click') 'clicked', (SELECT MAX(addDate) FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] WHERE emailID = p.id AND [action] = 'webversion') 'webversion', p.[exception], CASE WHEN n.[text] IS NULL THEN '-' ELSE n.[text] END 'emailName' FROM [" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] p LEFT JOIN [umbracoNode] n ON p.[emailID] = n.[id] WHERE ((@fromDate IS NULL OR @toDate IS NULL) OR (p.[dateSent] between @fromDate AND @toDate)) AND (@receiver IS NULL OR p.[to] LIKE '%' + @receiver + '%' OR p.[cc] like '%' + @receiver + '%' OR p.[bcc] like '%' + @receiver + '%') AND (@text IS NULL OR p.[subject] LIKE '%' + @text + '%' OR p.[body] LIKE '%' + @text + '%') AND (@emailID = p.emailID OR @emailID IS NULL) ) result WHERE @status IS NULL OR @status = -1 OR (@status = 0 AND result.Viewed IS NULL AND result.Clicked IS NULL AND result.Webversion IS NULL and result.exception IS NULL) OR (@status = 1 AND result.exception IS NOT NULL) OR (@status = 2 AND (result.Viewed IS NOT NULL OR result.Clicked IS NOT NULL OR result.Webversion IS NOT NULL)) OR (@status = 3 AND result.Clicked IS NOT NULL) OR (@status = 4 AND result.Webversion IS NOT NULL) ORDER BY CASE WHEN @orderBy = 'dateSent DESC' THEN [dateSent] ELSE NULL END DESC, CASE WHEN @orderBy = 'dateSent ASC' THEN [dateSent] ELSE NULL END ASC, CASE WHEN @orderBy = 'emailName DESC' THEN [emailName] ELSE NULL END DESC, CASE WHEN @orderBy = 'emailName ASC' THEN [emailName] ELSE NULL END ASC";

        /// <summary>
        /// Sql query that inserts an email statistic (event).
        /// </summary>
        public const string SQL_QUERY_ADD_STATISTICS = "INSERT INTO [" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] VALUES (@emailID, @action, @value, GETDATE(), @ip, @useragent)";

        /// <summary>
        /// Sql query that creates a table to store (log) statistics (events) that are triggered from the emails.
        /// </summary>
        public const string SQL_QUERY_CREATE_TABLE_PERPLEXMAILSTATISTICS = "IF NOT EXISTS(SELECT '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = '" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "') BEGIN CREATE TABLE [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "]([id] [int] IDENTITY(1,1) NOT NULL, [emailID] [bigint] NOT NULL, [action] [nvarchar](50) NOT NULL, [value] [nvarchar](255) NULL, [addDate] [datetime] NOT NULL, [ip] [nvarchar](30) NOT NULL, [useragent] [nvarchar](1024) NOT NULL, CONSTRAINT [PK_" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] PRIMARY KEY CLUSTERED ([id] ASC )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY] ALTER TABLE [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] ADD  CONSTRAINT [DF_" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "_addDate]  DEFAULT (getdate()) FOR [addDate] ALTER TABLE [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "]  WITH CHECK ADD  CONSTRAINT [FK_" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "_" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] FOREIGN KEY([emailID]) REFERENCES [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] ([id]) ON DELETE CASCADE ALTER TABLE [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "] CHECK CONSTRAINT [FK_" + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS + "_" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "] END";

        /// <summary>
        /// Sql query that creates a table to store (log) emails that are sent out by the PerplexMail package.
        /// </summary>
        public const string SQL_QUERY_CREATE_TABLE_PERPLEXLOGMAIL = "IF NOT EXISTS(SELECT '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = '" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "') BEGIN CREATE TABLE [dbo].[" + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG + "]([id] [bigint] IDENTITY(1,1) NOT NULL, [to] [nvarchar](255) NOT NULL, [from] [nvarchar](255) NOT NULL, [replyTo] [nvarchar](255) NULL, [cc] [nvarchar](255) NULL, [bcc] [nvarchar](255) NULL, [subject] [ntext] NOT NULL, [body] [ntext] NOT NULL, [alternativeView] [ntext] NULL, [attachment] [nvarchar](1000) NULL, [emailID] [int] NULL, [dateSent] [datetime] NOT NULL DEFAULT (getdate()), [website] [nvarchar](100) NOT NULL, [host] [nvarchar](100) NOT NULL, [userID] [nvarchar](100) NOT NULL, [specificUrl] [nvarchar](255) NOT NULL, [ip] [nvarchar](30) NOT NULL, [exception] [nvarchar](4000) NULL, [isEncrypted] [bit] NOT NULL, [salt] [nvarchar](255) NULL, PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY] END";
    }
}