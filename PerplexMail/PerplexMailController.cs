using PerplexMail.Models;
using System;
using System.Linq;
using Umbraco.Web.BaseRest;

namespace PerplexMail
{
    [RestExtension("PerplexMail")]
    public class PerplexMailController
    {
        [RestExtensionMethod(ReturnXml=false)]
        public static string SendTestMail()
        {
            if (!IsUmbracoAuthenticated) return null;
            try
            {
                var request = FromBody<SendTestEmailRequest>();
                PerplexMail.Email.SendUmbracoTestEmail(request);
                var response = new AjaxResponse { Success = true, Message = "Testmail sent to " + request.EmailAddress };
                return Helper.ToJSON(response);
            }
            catch (Exception ex)
            {
                var result = new AjaxResponse { Success = false, Message = "An error occured while sending the testmail. Please try again: " + ex.Message };
                return Helper.ToJSON(result);
            }
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string GetMailStatistics()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromBody<GetMailStatisticsRequest>();
            var response = new MailStatisticsResponse();
            try
            {
                // Get all emails from the log that meet our search criteria
                response.Emails = LogEmail.Search(request);

                // Get the statistics
                response.TotalSent = LogEmail.GetEmailSendCount(request.CurrentNodeId);
                response.TotalRead = LogEmail.GetEmailViewCount(request.CurrentNodeId);
                response.SelectionCount = (response.Emails == null) ? 0 : response.Emails.Count();

                // Pagination stuff
                response.Emails = response.Emails.Skip((request.CurrentPage - 1) * request.AmountPerPage).Take(request.AmountPerPage);
            }
            catch
            {

            }
            if (response != null)
                foreach (var m in response.Emails)
                    m.body = null;

            return Helper.ToJSON(response);
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string GetLogEmail()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromBody<LogMailRequest>();
            if (request != null)
            {
                if (!IsUmbracoAuthenticated) return null;
                var result = LogEmail.Get(request.logEmailId, true);
                return Helper.ToJSON(result);
            }
            else
                return null;
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string ResendEmail()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromBody<ResendEmailRequest>();
            return Email.ReSendEmail(request.EmailLogId, request.EmailAddress).ToString();
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string DownloadExcel()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromURI<GetMailStatisticsRequest>();
            LogEmail.Download(request);
            return null;
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string DownloadAttachment()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromBody<RequestAttachment>();
            var result = LogEmail.Download(request);
            // Als je hier komt dan is er iets verkeerd gegaan
            var response = new AjaxResponse()
            {
                Message = result,
                Success = false,
            };
            return Helper.ToJSON(response);
        }

        [RestExtensionMethod(ReturnXml=false)]
        public static string DownloadOutlookEmail()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromURI<DownloadEmailRequest>();
            Email.DownloadEmail(request.logMailid);
            return null;
        }

        [RestExtensionMethod(ReturnXml = false)]
        public static string ResetLog()
        {
            if (!IsUmbracoAuthenticated) return null;
            var request = FromURI<ResetLogRequest>();
            Email.DownloadEmail(request.id);
            return null;
        }

        static bool IsUmbracoAuthenticated
        {
            get
            {
                return System.Web.HttpContext.Current.User != null && System.Web.HttpContext.Current.User.Identity.GetType().Name == "UmbracoBackOfficeIdentity";
            }
        }

        /// <summary>
        /// Fetches the body of the posted content and attempts to convert it from JSON to a C# object
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>The object, or NULL if conversion failed</returns>
        static T FromURI<T>() where T : class, new()
        {
            var result = new T();
            var t = typeof(T);
            foreach (string q in System.Web.HttpContext.Current.Request.QueryString.AllKeys)
                try
                {
                    var value = System.Web.HttpContext.Current.Request.QueryString[q];
                    var p = t.GetProperty(q);
                    if (p != null)
                        if (p.PropertyType == typeof(int) || p.PropertyType.IsEnum) {
                            int tmp;
                            if (int.TryParse(value, out tmp))
                                p.SetValue(result, tmp);
                        } else if (p.PropertyType == typeof(DateTime)) {
                            DateTime tmp;
                            if (DateTime.TryParse(value, out tmp))
                                p.SetValue(result, tmp);
                        }
                        else
                            p.SetValue(result, value);
                }
                catch
                {
                }
            return result;
        }

        /// <summary>
        /// Fetches the body of the posted content and attempts to convert it from JSON to a C# object
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>The object, or NULL if conversion failed</returns>
        static T FromBody<T>() where T : class
        {
            T result = default(T);
            try
            {
                var s = new System.Web.Script.Serialization.JavaScriptSerializer();
                using (var sr = new System.IO.StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    var data = sr.ReadToEnd();
                    if (data != null)
                        result = s.Deserialize<T>(data);
                }
                
            }
            catch
            {
            }
            return result;
        }

        public class LogMailRequest
        {
            public int logEmailId { get; set; }
        }

        public class DownloadEmailRequest
        {
            public int logMailid { get; set; }
        }

        public class ResetLogRequest
        {
            public int id { get; set; }
        }
    }

    //// De onderstaande code is afhankelijk van de MVC library 
    //// Namespace ==> using Umbraco.Web.WebApi;
    //// Library ==> PerplexMail\packages\Microsoft.AspNet.Mvc.5.2.3\lib\net45\System.Web.Mvc.dll
    //[Umbraco.Web.Mvc.PluginController("package")]
    //public class PerplexMailController : UmbracoAuthorizedApiController
    //{
    //    [HttpPost]
    //    public AjaxResponse SendTestMail(SendTestEmailRequest request)
    //    {
    //        try
    //        {
    //            if (System.Web.HttpContext.Current != null)
    //            {
    //                var test = System.Web.HttpContext.Current.User.Identity.Name;
    //            }
    //            PerplexMail.Email.SendUmbracoTestEmail(request);
    //            return new AjaxResponse { Success = true, Message = "Testmail sent to " + request.EmailAddress };
    //        }
    //        catch (Exception ex)
    //        {
    //            return new AjaxResponse { Success = false, Message = "An error occured while sending the testmail. Please try again: " + ex.Message };
    //        }
    //    }

    //    [HttpPost]
    //    public MailStatisticsResponse GetMailStatistics(GetMailStatisticsRequest request)
    //    {
    //        var response = new MailStatisticsResponse();

    //        try
    //        {            
    //            // Get all emails from the log that meet our search criteria
    //            response.Emails = LogEmail.Search(request);
                      
    //            // Get the statistics
    //            response.TotalSent = LogEmail.GetEmailSendCount(request.CurrentNodeId);
    //            response.TotalRead = LogEmail.GetEmailViewCount(request.CurrentNodeId);
    //            response.SelectionCount = (response.Emails == null) ? 0 : response.Emails.Count();

    //            // Pagination stuff
    //            response.Emails = response.Emails.Skip((request.CurrentPage - 1) * request.AmountPerPage).Take(request.AmountPerPage);
    //        }
    //        catch
    //        {

    //        }

    //        return response;
    //    }

    //    [HttpPost]
    //    public LogEmail GetLogEmail(int logEmailId)
    //    {
    //        return LogEmail.Get(logEmailId, true);
    //    }

    //    [HttpPost]
    //    public int ResendEmail(ResendEmailRequest request)
    //    {
    //        return Email.ReSendEmail(request.EmailLogId, request.EmailAddress);
    //    }

    //    [HttpGet]
    //    public AjaxResponse DownloadExcel([FromUri] GetMailStatisticsRequest request)
    //    {
    //        LogEmail.Download(request);
    //        return new AjaxResponse()
    //            {
    //                Message = "Success",
    //                Success = true,
    //            };
    //    }

    //    [HttpGet]
    //    public AjaxResponse DownloadAttachment([FromUri] RequestAttachment request)
    //    {
    //        var result = LogEmail.Download(request);
    //        if (!String.IsNullOrEmpty(result))
    //            return new AjaxResponse()
    //            {
    //                Message = result,
    //                Success = false,
    //            };
    //        else
    //            return new AjaxResponse()
    //            {
    //                Message = "Success",
    //                Success = true,
    //            };
    //    }

    //    [HttpGet]
    //    public AjaxResponse DownloadOutlookEmail([FromUri] DownloadEmailRequest request)
    //    {
    //        Email.DownloadEmail(request.logMailid);
    //        return null;
    //    }
    //    public class DownloadEmailRequest
    //    {
    //        public int logMailid { get; set; }
    //    }
    //}
}