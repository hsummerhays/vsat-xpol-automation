// <copyright file="WebPageParser.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Web
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using MainstreamData.Utility;

    // TODO: Add exception handling code and throw custom exception.

    /// <summary>
    /// Gets data from web pages (e.g. Skystream web page).
    /// </summary>
    public class WebpageParser
    {
        /// <summary>
        /// List of singleton tags that don't require separate closing tags.
        /// </summary>
        private static List<string> singletonTags = new List<string> { "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "link", "meta", "param", "source" };

        /// <summary>
        /// Stores HTML data from web page.
        /// </summary>
        private StringBuilder html = new StringBuilder();

        /// <summary>
        /// Holds cookies to allow for authentication and sessions.
        /// </summary>
        private CookieContainer cookies = new CookieContainer();

        /// <summary>
        /// Holds the URL from the last response to help sort out when redirects happen.
        /// </summary>
        private string lastResponseUrl = string.Empty;

        /// <summary>
        /// The value to store for the User-Agent http header when sending requests (e.g. FireFox might use: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:7.0.1) Gecko/20100101 Firefox/7.0.1).
        /// </summary>
        private string userAgent = string.Format(
            "{0}-{1}/{2} ({3})",
            ApplicationInfo.Company.Replace(" ", string.Empty),
            ApplicationInfo.Name.Replace(" ", string.Empty),
            ApplicationInfo.Version,
            ComputerInfo.WindowsVersionString);

        /// <summary>
        /// Initializes a new instance of the WebpageParser class.
        /// </summary>
        public WebpageParser()
        {
            // No code needed here - simply making default constructor public.
        }

        /// <summary>
        /// Gets the Html retrieved from the web page.
        /// </summary>
        public string Html
        {
            get
            {
                return this.html.ToString();
            }
        }

        /// <summary>
        /// Gets the response from the web server with the HTML stripped away.
        /// </summary>
        public string ResponseText
        {
            get
            {
                return Regex.Replace(this.html.ToString(), "<(.|\n)*?>", string.Empty).Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Trim();
            }
        }

        /// <summary>
        /// Gets the URL from the last response to help sort out when redirects happen.
        /// </summary>
        public string LastResponseUrl
        {
            get
            {
                return this.lastResponseUrl;
            }
        }

        /// <summary>
        /// Gets or sets the cookies used for authentication and sessions.
        /// </summary>
        public CookieContainer Cookies
        {
            get
            {
                return this.cookies;
            }

            set
            {
                this.cookies = value;
            }
        }

        /// <summary>
        /// Converts html escape sequences to their unescaped values (e.g. nbsp becomes a space character)
        /// </summary>
        /// <param name="html">Html data to convert.</param>
        /// <returns>Decoded html data with spaces trimmed.</returns>
        public static string Decode(string html)
        {
            return HttpUtility.HtmlDecode(html).Trim();
        }

        /// <summary>
        /// Retrieves data within a tag specified by an HTML path.
        /// </summary>
        /// <param name="html">HTML data to look inside of.</param>
        /// <param name="htmlPath">Path to data (e.g. /html/table[2]/tr[2]/td).</param>
        /// <returns>Data within the specified tag.</returns>
        public static string GetHtmlElementData(string html, string htmlPath)
        {
            return GetHtmlElementData(new StringBuilder(html), htmlPath);
        }

        /// <summary>
        /// Retrieves data within a tag specified by an HTML path.
        /// </summary>
        /// <param name="html">HTML data to look inside of.</param>
        /// <param name="htmlPath">Path to data (e.g. /html/table[2]/tr[2]/td).</param>
        /// <returns>Data within the specified tag.</returns>
        public static string GetHtmlElementData(StringBuilder html, string htmlPath)
        {
            //// TODO: Use HTML Agility Pack instead of parser borrowed from Newscom WebDataRetriever

            StringBuilder childData = new StringBuilder();
            int len = html.Length;
            int pos = 0;

            string[] pathNodes = htmlPath.Split('/');
            int nodeCount = pathNodes.Length;

            // Ignore first node if empty (i.e. slash (/) was the first character.
            int startNode = nodeCount > 0 && string.IsNullOrEmpty(pathNodes[0]) ? 1 : 0;
            for (int i = startNode; i < nodeCount; i++)
            {
                string nodeString = pathNodes[i];

                // See if there is a repetition along with the tag
                string[] nodeNameAndRepetition = nodeString.Split('[', ']');
                int nodeRepetition = 0;
                string nodeName;
                if (nodeNameAndRepetition.Length > 1)
                {
                    nodeName = nodeNameAndRepetition[0];
                    nodeRepetition = Convert.ToInt32(nodeNameAndRepetition[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    nodeName = nodeString;
                }

                // Setup variables for state machine
                StringBuilder curTagName = new StringBuilder();
                StringBuilder tagText = new StringBuilder();
                int tagState = 0;
                int dataState = 0;
                int curRep = 0;
                bool isClosingTag = false;
                bool moveToNextNode = false;
                string drillingTag = string.Empty;
                int skip = 0;

                // Go through each character until entire text is parsed or ending tag is found.
                while (pos < len && !moveToNextNode)
                {
                    char curChar = html[pos];

                    /* -------------------
                     * Data state machine.
                     * -------------------*/

                    // IF dataState = 0 - waiting for end node to be found
                    if (dataState == 1 && tagState == 0 && curChar != '<')
                    {
                        // Capture data from element.
                        childData.Append(curChar);
                    }

                    /* ------------------
                     * Tag state machine.
                     * ------------------*/

                    switch (tagState)
                    {
                        case 0:  // Looking for tag.
                            if (curChar == '<')
                            {
                                tagState++;
                                tagText.Append(curChar);
                            }

                            break;

                        case 1:   // Tag found - building tag name.
                            tagText.Append(curChar);
                            if (curChar == ' ')
                            {
                                // Tag has attributes.
                                tagState++;
                                goto case 2;
                            }
                            else if (curChar == '>')
                            {
                                // Tag has ended.
                                tagState = 3;
                                goto case 3;
                            }
                            else if (tagText.ToString().Substring(0, 2).Equals("<!", StringComparison.Ordinal) &&
                                nodeString.Substring(0, 1) != "!")
                            {
                                // Is comment or DOCTYPE tag - ignore unless node starts with exclamation (!).
                                tagState = 1;
                                break;
                            }
                            else if (curChar == '/')
                            {
                                isClosingTag = true;
                            }
                            else
                            {
                                curTagName.Append(curChar);
                            }

                            break;

                        case 2:     // Tag has attributes - skipping them.
                            while (pos < len && curChar != '>')
                            {
                                curChar = html[++pos];
                                tagText.Append(curChar);
                            }

                            tagState++;
                            goto case 3;

                        case 3:     // Tag has ended.
                            // Check for singleton tags that won't have a separate closing tag (e.g. <br>)
                            if (!isClosingTag &&
                                WebpageParser.singletonTags.FindIndex(x => x.Equals(curTagName.ToString(), StringComparison.OrdinalIgnoreCase)) != -1)
                            {
                                isClosingTag = true;
                            }

                            bool isFinalOpeningTag = false;

                            /* Where are we - 
                             * 1. We are drilling, continue until done.
                             * 2. Tag and repetition match, go to next node.
                             * 3. Tag or repetition doesn't match, start drilling. */

                            // For each tag on a given level that doesn't match the node, we have to drill into it and back out.
                            if (!string.IsNullOrEmpty(drillingTag))
                            {
                                if (drillingTag.Equals(curTagName.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (isClosingTag)
                                    {
                                        // Compare skip using <= to avoid problems with extra closing tags.
                                        if (skip <= 0)
                                        {
                                            // We are done drilling
                                            drillingTag = string.Empty;

                                            if (dataState == 1)
                                            {
                                                // We are done getting data
                                                dataState = 0;
                                                moveToNextNode = true;
                                            }
                                        }
                                        else
                                        {
                                            // Continue drilling
                                            skip--;
                                        }
                                    }
                                    else
                                    {
                                        // Found another opening tag - continue drilling
                                        skip++;
                                    }
                                }
                                else
                                {
                                    // Tag is underneath drilling tag - carry on
                                }
                            }
                            else if (curTagName.ToString().Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Tag matches node - check repetition
                                if (curRep == nodeRepetition)
                                {
                                    if (i == nodeCount - 1)
                                    {
                                        // We are on the last node, time to drill into it and capture its data.
                                        dataState = 1;
                                        isFinalOpeningTag = true;
                                        drillingTag = nodeName;
                                    }
                                    else
                                    {
                                        moveToNextNode = true;
                                    }
                                }
                                else
                                {
                                    // Found another repetition to skip
                                    curRep++;
                                    drillingTag = nodeName;
                                }
                            }
                            else
                            {
                                // Tag doesn't match, so start drilling, but not on closing tags
                                if (!isClosingTag)
                                {
                                    drillingTag = curTagName.ToString();
                                }
                            }

                            // Capture tag text if needed.
                            if (dataState == 1 && !isFinalOpeningTag)
                            {
                                childData.Append(tagText);
                            }

                            // Reset StringBuilders
                            tagText.Length = 0;
                            curTagName.Length = 0;

                            isClosingTag = false;
                            tagState = 0;
                            break;
                    }

                    pos++;
                }
            }

            return childData.ToString();
        }

        /// <summary>
        /// Retrieves data within a tag specified by an HTML path using WebPageParser.Html.
        /// </summary>
        /// <param name="htmlPath">Path to data (e.g. /html/table[2]/tr[2]/td).</param>
        /// <returns>Data within the specified tag.</returns>
        public string GetHtmlElementData(string htmlPath)
        {
            return GetHtmlElementData(this.html, htmlPath);
        }

        /// <summary>
        /// Retrieves decoded html data within a tag specified by an HTML path using WebPageParser.Html.
        /// </summary>
        /// <param name="htmlPath">Path to data (e.g. /html/table[2]/tr[2]/td).</param>
        /// <returns>Decoded html data within the specified tag.</returns>
        public string GetDecodedHtmlElementData(string htmlPath)
        {
            return WebpageParser.Decode(this.GetHtmlElementData(htmlPath));
        }

        /// <summary>
        /// Retrieves HTML data from specified URL.
        /// </summary>
        /// <param name="url">Web browser address of page to retrieve data from.</param>
        public void Retrieve(string url)
        {
            this.Retrieve(url, string.Empty, string.Empty);
        }

        /// <summary>
        /// Retrieves HTML data from specified URL.
        /// </summary>
        /// <param name="url">Web browser address of page to retrieve data from.</param>
        /// <param name="userName">Basic Authentication UserName to use to login to the web page.</param>
        /// <param name="password">Basic Authentication Password to use to login to the web page.</param>
        public void Retrieve(string url, string userName, string password)
        {
            this.Retrieve(new Uri(url), userName, password, 60);
        }

        /// <summary>
        /// Retrieves HTML data from specified URI.  For sessions, you must pass WebPageParser.Cookies
        /// each time you call Retrieve, once you have logged in.
        /// </summary>
        /// <param name="uri">Web browser address of page to retrieve data from.</param>
        /// <param name="userName">Basic Authentication UserName to use to login to the web page.</param>
        /// <param name="password">Basic Authentication Password to use to login to the web page.</param>
        /// <param name="timeoutSeconds">Number of seconds to read stream before timing out.</param>
        public void Retrieve(Uri uri, string userName, string password, int timeoutSeconds)
        {
            //// Note: Error handling is done by caller

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = this.userAgent;
            if (timeoutSeconds > int.MaxValue / 1000)
            {
                request.Timeout = int.MaxValue;
            }
            else
            {
                request.Timeout = timeoutSeconds * 1000;
            }

            request.Credentials = new NetworkCredential(userName, password);    // Basic authentication

            // Save cookie so don't get caught in redirect loop on login pages and so can have sessions.
            request.CookieContainer = this.cookies;

            this.GetResponse(request);
        }

        /// <summary>
        /// Posts parameters (via HTTP) to the specified URL.
        /// </summary>
        /// <param name="url">The URL to post to.</param>
        /// <param name="parameters">Parameters to post (e.g. name1[equals]value1[ampersand]name2[equals]value2)</param>
        public void Post(string url, string parameters)
        {
            // Create the request and stream it out.
            HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(url);
            request.UserAgent = this.userAgent;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = this.Cookies;
            byte[] bytes = Encoding.UTF8.GetBytes(parameters);
            request.ContentLength = bytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            this.GetResponse(request);
        }

        /// <summary>
        /// Confirms whether or not the url returns a valid image (e.g. jpeg).
        /// </summary>
        /// <param name="url">Web browser address to retrieve the image from.</param>
        /// <returns>Boolean indicating whether or not a valid image was returned.</returns>
        public bool VerifyImage(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.UserAgent = this.userAgent;
            request.Timeout = 60000;    // 60 seconds
            request.CookieContainer = this.cookies;
            this.html.Length = 0;   // Clear the html string - going to load it up in case html is returned instead of image.

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream resStream = response.GetResponseStream())
                    {
                        string tempString = null;
                        int count = 0;
                        byte[] buf = new byte[8192];
                        do
                        {
                            // fill the buffer with data
                            count = resStream.Read(buf, 0, buf.Length);

                            // make sure we read some data
                            if (count != 0)
                            {
                                // Write bytes to memory stream so can verify image.
                                ms.Write(buf, 0, count);

                                // Translate from bytes to text, then save to string in case contains HTML.
                                tempString = Encoding.UTF8.GetString(buf, 0, count);
                                this.html.Append(tempString);
                            }
                        }
                        while (count > 0); // any more data to read?
                    }

                    try
                    {
                        // Load from MemoryStream into Bitmap object to see if valid image - will throw argument exception if not
                        using (Bitmap bitmap = new Bitmap(ms))
                        {
                            // Nothing to do here - just want to make sure it gets disposed.
                        }
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets response from the provided request.
        /// </summary>
        /// <param name="request">Reqest to get the response from.</param>
        private void GetResponse(HttpWebRequest request)
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    this.html = new StringBuilder(reader.ReadToEnd());
                }

                this.lastResponseUrl = response.ResponseUri.ToString();

                // Save new cookies from response
                if (response.Cookies.Count > 0)
                {
                    this.cookies.Add(response.Cookies);
                }
            }
        }
    }
}
