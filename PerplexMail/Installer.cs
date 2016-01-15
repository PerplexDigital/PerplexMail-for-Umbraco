using System;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;
using System.Xml;
using umbraco.interfaces;

namespace PerplexMail.Installer
{
    public class UmbracoInstaller : IPackageAction
    {
        public string Alias()
        {
            return "InstallPerplexMail";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            UpgradeWebConfig();

            return true;
        }

        bool IsLegacyInstallation()
        {
            try
            {
                // Bepaal hier vanaf waar de NIEUWE versie gebruikt mag worden. Alles hiervoor = legacy
                Version vMinimum = new Version("7.3.0");
                Version vCurrent = null;
                string current = ConfigurationManager.AppSettings["umbracoConfigurationStatus"];
                // Het kan zijn dat iemand niet met een relaese versie werkt, b.v. een beta ==> 7.3.0-beta3
                if (current != null && current.Contains('-'))
                    current = current.Split('-')[0];
                if (!String.IsNullOrEmpty(current)) vCurrent = new Version(current);

                if (vCurrent != null)
                    // Indien de current version lager is dan de minimum version dan is het legacy
                    return vCurrent.CompareTo(vMinimum) < 0;
                else
                    return false;
            }
            catch
            {
                // Ga er voor nu maar uit dat het een legacy installation is...
                return true;
            }
        }

        void UpgradeWebConfig()
        {
            try
            {
                Configuration webConfig = WebConfigurationManager.OpenWebConfiguration("~");
                bool hasChanged = false;

                // Remove AppSettings keys
                if (!webConfig.AppSettings.Settings.AllKeys.Contains(Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY))
                {
                    webConfig.AppSettings.Settings.Add(Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY, Security.GeneratePassword(16, minSpecialCharCount: 0));
                    hasChanged = true;
                }
                if (!webConfig.AppSettings.Settings.AllKeys.Contains(Constants.WEBCONFIG_SETTING_DISABLE_LOG))
                {
                    webConfig.AppSettings.Settings.Add(Constants.WEBCONFIG_SETTING_DISABLE_LOG, "false");
                    hasChanged = true;
                }
                if (!webConfig.AppSettings.Settings.AllKeys.Contains(Constants.WEBCONFIG_SETTING_DISABLE_ENCRYPTION))
                {
                    webConfig.AppSettings.Settings.Add(Constants.WEBCONFIG_SETTING_DISABLE_ENCRYPTION, "false");
                    hasChanged = true;
                }
                if (!webConfig.AppSettings.Settings.AllKeys.Contains(Constants.WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS))
                {
                    webConfig.AppSettings.Settings.Add(Constants.WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS, "true");
                    hasChanged = true;
                }

                // Add Module to web.config ==> /system.web/httpModules/
                // Take note: this is the module configuration for OLDER versions of IIS
                var sectionHttpModules = webConfig.GetSection("system.web/httpModules") as HttpModulesSection;
                if (sectionHttpModules != null)
                {
                    bool exists = false;
                    foreach (HttpModuleAction ma in sectionHttpModules.Modules)
                        if (ma.Name == Constants.WEBCONFIG_MODULE_NAME)
                        {
                            exists = true; break;
                        }
                    if (!exists)
                    {
                        sectionHttpModules.Modules.Add(new HttpModuleAction(Constants.WEBCONFIG_MODULE_NAME, Constants.PERPLEXMAIL_STATISTICSMODULE_CLASS));
                        hasChanged = true;
                    }
                }

                // Add Module to web.config ==> /system.webServer/modules/ 
                var section = webConfig.GetSection("system.webServer");
                if (section != null && section.SectionInformation != null)
                {
                    // Manipulating XML is a terrible way of modifying the web.config as it is prone to errors.
                    // However, the system.webServer/modules section is new in IIS7+ and cannot be manipulated via objects/properties (an extra library must be referenced for this)
                    var config = section.SectionInformation.GetRawXml();
                    if (config != null && !config.Contains(Constants.WEBCONFIG_MODULE_NAME))
                    {
                        string xmlModule = String.Format("<add name=\"{0}\" type=\"{1}\" />", Constants.WEBCONFIG_MODULE_NAME, Constants.PERPLEXMAIL_STATISTICSMODULE_CLASS);
                        if (config.Contains("</modules>"))
                            config = config.Replace("</modules>", xmlModule + "</modules>");
                        else
                            config = config.Replace("</system.webServer>", "<modules runAllManagedModulesForAllRequests=\"true\">" + xmlModule + "</modules></system.webServer>");
                        section.SectionInformation.SetRawXml(config);
                        hasChanged = true;
                    }
                }

                if (hasChanged)
                    webConfig.Save();
            }
            catch
            {

            }
        }

        void DowngradeWebConfig()
        {
            try
            {
                Configuration webConfig = WebConfigurationManager.OpenWebConfiguration("~");
                bool hasChanged = false;

                // Remove AppSettings keys
                if (webConfig.AppSettings.Settings[Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY] != null)
                {
                    webConfig.AppSettings.Settings.Remove(Constants.WEBCONFIG_SETTING_PERPLEXMAIL_KEY);
                    hasChanged = true;
                }
                if (webConfig.AppSettings.Settings[Constants.WEBCONFIG_SETTING_DISABLE_LOG] != null)
                {
                    webConfig.AppSettings.Settings.Remove(Constants.WEBCONFIG_SETTING_DISABLE_LOG);
                    hasChanged = true;
                }
                if (webConfig.AppSettings.Settings[Constants.WEBCONFIG_SETTING_DISABLE_ENCRYPTION] != null)
                {
                    webConfig.AppSettings.Settings.Remove(Constants.WEBCONFIG_SETTING_DISABLE_ENCRYPTION);
                    hasChanged = true;
                }
                if (webConfig.AppSettings.Settings[Constants.WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS] != null)
                {
                    webConfig.AppSettings.Settings.Remove(Constants.WEBCONFIG_SETTING_DISABLE_DYNAMICATTACHMENTS);
                    hasChanged = true;
                }

                // Remove Module from web.config ==> /system.web/httpModules/ section
                var sectionHttpModules = webConfig.GetSection("system.web/httpModules") as HttpModulesSection;
                if (sectionHttpModules != null && sectionHttpModules.Modules != null)
                    for (int i = 0; i < sectionHttpModules.Modules.Count; i++)
                        if (sectionHttpModules.Modules[i].Name == Constants.WEBCONFIG_MODULE_NAME)
                        {
                            sectionHttpModules.Modules.Remove(Constants.WEBCONFIG_MODULE_NAME);
                            hasChanged = true;
                            break;
                        }

                // Remove Module from web.config ==> /system.webServer/modules/ 
                var section = webConfig.GetSection("system.webServer");
                if (section != null && section.SectionInformation != null)
                {
                    // Manipulating XML is a terrible way of modifying the web.config as it is prone to errors.
                    // However, the system.webServer/modules section is new in IIS7+ and cannot be manipulated via objects/properties (an extra library must be referenced for this)
                    var config = section.SectionInformation.GetRawXml();

                    string xmlModule = String.Format("<add name=\"{0}\" type=\"{1}\" />", Constants.WEBCONFIG_MODULE_NAME, Constants.PERPLEXMAIL_STATISTICSMODULE_CLASS);

                    if (config != null && config.Contains(xmlModule))
                    {
                        config = config.Replace(xmlModule, String.Empty);
                        section.SectionInformation.SetRawXml(config);
                        hasChanged = true;
                    }
                }

                if (hasChanged)
                    webConfig.Save();

                string perplexMailDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + Constants.PACKAGE_ROOT);
                if (System.IO.Directory.Exists(perplexMailDirectory))
                    System.IO.Directory.Delete(perplexMailDirectory);
            }
            catch
            {

            }
        }

        void CleanUpDatabase()
        {
            // Remove all email package related tables and stored procedures from the database
            var db = Umbraco.Core.ApplicationContext.Current.DatabaseContext.Database;
            db.OpenSharedConnection();
            try
            {
                using (var command = db.CreateCommand(db.Connection, Constants.SQL_QUERY_TABLE_EXISTS, new { tblName = Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS }))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    var result = command.ExecuteScalar();
                    if (result != null && result.ToString() == "1")
                    {
                        command.CommandText = "DROP TABLE " + Constants.SQL_TABLENAME_PERPLEXMAIL_STATISTICS;
                        command.ExecuteNonQuery();
                    }
                }
                using (var command = db.CreateCommand(db.Connection, Constants.SQL_QUERY_TABLE_EXISTS, new { tblName = Constants.SQL_TABLENAME_PERPLEXMAIL_LOG }))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    var result = command.ExecuteScalar();
                    if (result != null && result.ToString() == "1")
                    {
                        command.CommandText = "DROP TABLE " + Constants.SQL_TABLENAME_PERPLEXMAIL_LOG;
                        command.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                db.CloseSharedConnection();
            }
        }

        public XmlNode SampleXml()
        {
            var d = new XmlDocument();
            var xml = string.Format("<Action runat=\"install\" alias=\"{0}\" />", Alias());
            d.LoadXml(xml);
            return d.SelectSingleNode("Action");
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            DowngradeWebConfig();
            CleanUpDatabase();

            return true;
        }
    }
}