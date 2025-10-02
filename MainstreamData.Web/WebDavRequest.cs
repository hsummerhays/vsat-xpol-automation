// <copyright file="WebDavRequest.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Security.Authentication;
    using System.Text;
    using System.Web;
    using System.Xml;

    /// <summary>
    /// Class for retrieving data from MS Exchange via WebDAV.
    /// </summary>
    public class WebDavRequest
    {
        /// <summary>
        /// Path used to access the email inbox.
        /// </summary>
        private string inboxPath = string.Empty;

        /// <summary>
        /// Cookie container that holds authentication information for Exchange server.
        /// </summary>
        private CookieContainer cookies = null;

        /// <summary>
        /// Authenticate and store the authorization cookie so we can use it for future requests.
        /// </summary>
        /// <param name="server">Http path to server (e.g. https://webmail.mainstreamdata.com).</param>
        /// <param name="user">Mailbox username (e.g. prnmonitoring).</param>
        /// <param name="password">Mailbox password.</param>
        public void Authenticate(string server, string user, string password)
        {
            this.inboxPath = server + "/exchange/" + user + "/inbox";
            string authUrl = server + "/exchweb/bin/auth/owaauth.dll";

            // Create the web request body.
            string body = string.Format(CultureInfo.InvariantCulture, "destination={0}&username={1}&password={2}", this.inboxPath, user, password);
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            
            // Create the web request.
            HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(authUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            request.ContentLength = bytes.Length;

            // Create the web request content stream.
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            // Get the response and store the authentication cookies.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.Cookies.Count < 2)
            {
                throw new AuthenticationException("Login failed for user " + user + " at " + server);
            }

            this.cookies = new CookieContainer();
            foreach (Cookie cookie in response.Cookies)
            {
                this.cookies.Add(cookie);
            }

            response.Close();
        }

        /// <summary>
        /// Reads all of the email messages in the inbox.
        /// </summary>
        /// <returns>XmlDocument containing the email messages in the inbox.</returns>
        public XmlDocument ReadInbox()
        {
            // Build the SQL query.
            string query = "<?xml version=\"1.0\"?><D:searchrequest xmlns:D = \"DAV:\">" + 
                "<D:sql>SELECT \"urn:schemas:httpmail:sendername\" , \"urn:schemas:httpmail:subject\"," + 
                " \"urn:schemas:mailheader:from\", \"urn:schemas:httpmail:datereceived\" ," + 
                " \"urn:schemas:httpmail:date\", \"urn:schemas:httpmail:textdescription\" ," + 
                " \"urn:schemas:httpmail:htmldescription\", \"DAV:id\"" + 
                ", \"DAV:href\"" + 
                " FROM \"" + this.inboxPath + "\"" + 
                " WHERE \"DAV:ishidden\" = false AND \"DAV:isfolder\" = false" + 
                " </D:sql></D:searchrequest>";         

            // Send the search request.
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(this.inboxPath);
            request.CookieContainer = this.cookies;
            request.Method = "SEARCH";
            request.ContentType = "text/xml";
            byte[] bytes = Encoding.UTF8.GetBytes(query);
            request.ContentLength = bytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string sa = string.Empty;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.ASCII))
            {
                sa = sr.ReadToEnd();
            }

            response.Close();

            // Return the xml document
            XmlDocument doc = new XmlDocument();
            doc.InnerXml = sa;
            return doc;

            /*
            XmlNodeList mailId = doc.GetElementsByTagName("a:id");
            XmlNodeList mailFrom = doc.GetElementsByTagName("e:from");
            XmlNodeList mailReceivedDate = doc.GetElementsByTagName("d:datereceived");
            XmlNodeList mailSubject = doc.GetElementsByTagName("d:subject");
            XmlNodeList mailText = doc.GetElementsByTagName("d:textdescription");
            Console.WriteLine(mailId.Count);
            for (int i = 0; i < mailId.Count; ++i)
            {
                Console.WriteLine("-------------------------------\n{0}: E-mail ID: {1}", i + 1, mailId[i].InnerText);
                Console.WriteLine("From:\t{0}", mailFrom[i].InnerText);
                Console.WriteLine("Received:\t{0}", mailReceivedDate[i].InnerText);
                Console.WriteLine("Subject:\t{0}", mailSubject[i].InnerText);
                //// Console.WriteLine("Content:\n{0}", mailText[i].InnerText);
            } */   
        }

        /// <summary>
        /// Moves email message from one location to another.
        /// </summary>
        /// <param name="sourceUrl">The location of the email message (e.g. https://webmail.mainstreamdata.com/exchange/prnmonitoring/inbox/test%20email.eml )</param>
        /// <param name="destinationUrl">The location to move the email message (e.g. ./Processed/test%20email.eml )</param>
        public void MoveMailItem(string sourceUrl, string destinationUrl)
        {
            // Build the request.
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(sourceUrl);
            request.CookieContainer = this.cookies;
            request.Method = "MOVE";
            request.Headers.Add("Destination", destinationUrl);
            request.Headers.Add("Allow-rename", "T");
            request.Headers.Add("Overwrite", "F");

            // Send the request.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();
        }
    }
}
