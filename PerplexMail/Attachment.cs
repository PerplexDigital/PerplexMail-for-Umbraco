using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PerplexMail
{
    /// <summary>
    /// This class is used to add attachments of any type to the e-mail.
    /// </summary>
    public class Attachment
    {
        /// <summary>
        /// Determines whether this attachment contains any file.
        /// </summary>
        public bool IsEmpty { get { return String.IsNullOrEmpty(FileName); } }

        /// <summary>
        /// The filename associated with the attachment
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The directory where the attachment is/will be located. 
        /// - If it is an existing file (such as an Umbraco Media item), this property contains the physical path to the directory containing that file.
        /// - If the attachment is a filestream, this property will contain the absolute physical path to the e-mailpackage attachment folder.
        /// </summary>
        public string FileDirectory { get; private set; }

        /// <summary>
        /// The stream that contains the file to attacht to the e-mail. This property is only set if the filestream constructor is used.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// If the attachment is an existing Umbraco Media item, this will contain the media node ID. Only applicable if the umbracoMediaId constructor is used.
        /// </summary>
        public int UmbracoMediaId { get; private set; }

        /// <summary>
        /// Create an attachment file from an Umbraco Media node containing a file.
        /// </summary>
        /// <param name="umbracoMediaId">The Umbraco Media Node Id containing the file to attach.</param>
        public Attachment(int umbracoMediaId)
        {
            this.UmbracoMediaId = umbracoMediaId;
            if (UmbracoMediaId == 0)
                throw new ArgumentException("Error executing constructor 'new PerplexMail.Attachment(umbracoMediaId)': Invalid Umbraco Media Id (" + umbracoMediaId.ToString() + ")", "umbracoMediaId");
            
            string absolutePath = Helper.GetUmbracoMediaFile(umbracoMediaId, true);
            if (String.IsNullOrEmpty(absolutePath))
                throw new ArgumentException("Error executing constructor 'new PerplexMail.Attachment(umbracoMediaId)': Media Node with id " + umbracoMediaId.ToString() + " contains no file", "umbracoMediaId");

            FileName = System.IO.Path.GetFileName(absolutePath);
            FileDirectory = System.IO.Path.GetDirectoryName(absolutePath) + '\\';
        }

        /// <summary>
        /// Create an attachment from an existing file on the physical harddrive.
        /// </summary>
        /// <param name="filePath">The relative or absolute physical path to the file to be attached.</param>
        public Attachment(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filename");
            if (!System.IO.Path.HasExtension(filePath))
                throw new ArgumentException("Error executing constructor 'new PerplexMail.Attachment(stream,filename)': '" + filePath + "' is not a valid filename", "filename");

            FileName = System.IO.Path.GetFileName(filePath);
            FileDirectory = System.IO.Path.GetDirectoryName(filePath) + '\\';
        }

        /// <summary>
        /// Create an attachment from an input filestream. The name for the file also needs to be provided
        /// </summary>
        /// <param name="filestream">The stream containing the file to attach.</param>
        /// <param name="filename">The name of the file, including the file extension and excluding any folder/path, as it will appear in the attachment of the e-mail</param>
        public Attachment(Stream filestream, string filename)
        {

            if (filestream == null)
                throw new ArgumentNullException("filestream");
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");
            if (!System.IO.Path.HasExtension(filename))
                throw new ArgumentException("Error executing constructor 'new PerplexMail.Attachment(filestream,filename)': '" + filename + "' is not a valid filename", "filename");

            FileName = filename;
            FileDirectory = System.Web.Hosting.HostingEnvironment.MapPath(Constants.ATTACHMENTS_FOLDER);
            Stream = filestream;
        }
    }
}