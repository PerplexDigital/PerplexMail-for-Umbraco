using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PerplexMail
{
    /// <summary>
    /// This class is used as a model class to specify which tag (in the email) should be replaced with which value
    /// </summary>
    public class EmailTag
    {
        string _tag;
        string _value;
        public string Tag { get { return _tag; } set { _tag = sanitizeTag(value); } }
        public string Value { get { return _value; } set { _value = value; } }
        public string FullTag { get { return Constants.TAG_PREFIX + Tag + Constants.TAG_SUFFIX; } }

        /// <summary>
        /// Creates a basic email tag. This tag can be used when sending a PerplexMail email to replace tags with text values.
        /// </summary>
        public EmailTag(){ }

        /// <summary>
        /// Creates a basic email tag. This tag can be used when sending a PerplexMail email to replace tags with text values.
        /// </summary>
        /// <param name="tag">The tagname, without any prefix [# or suffix #]</param>
        /// <param name="value">The value to replace the tags with (in the email)</param>
        public EmailTag(string tag, string value)
        {
            Tag = sanitizeTag(tag);
            Value = value;
        }

        /// <summary>
        /// Creates a basic if tag. This tag can be used to show or hide specific content in emails sent by the email package.
        /// For example the text with tags: [#IsVehicle#]Text about vehicle details[#/IsVehicle#]
        /// is VISIBLE if the tag "IsVehicle" is TRUE and HIDDEN when FALSE.
        /// </summary>
        /// <param name="tag">The tagname, without any prefix [# or suffix #]</param>
        /// <param name="state">True (to show the content) or False (to hide the content)</param>
        public EmailTag(string tag, bool state)
        {
            Tag = sanitizeTag(tag);
            Value = state ? "true" : "false";
        }

        /// <summary>
        /// This function strips all opening [# and closing #] characters for a tag. The email pacakge will already handle this for us.
        /// </summary>
        /// <param name="tag">The tagname to sanitize</param>
        /// <returns>A sanitized tagname</returns>
        static string sanitizeTag(string tag)
        {
            if (String.IsNullOrEmpty(tag))
                throw new ArgumentNullException("tag");

            if (tag.StartsWith("[#"))
                if (tag.EndsWith("#]"))
                    return tag.Substring(2, tag.Length - 4);
                else
                    return tag.Substring(2);
            else if (tag.EndsWith("#]"))
                return tag.Substring(0, tag.Length - 2);
            // Default
            return tag;
        }
        
    }
}