// <copyright file="ExchangeController.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.MSExchange
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Net;
    using Microsoft.Exchange.WebServices.Data;

    /// <summary>
    /// Uses Microsoft Exchange Web Services Managed API to connect to Microsoft Exchange and retrieve email, etc.
    /// </summary>
    public class ExchangeController
    {
        /// <summary>
        /// The main object for the service.
        /// </summary>
        private ExchangeService service = null;

        /// <summary>
        /// Instantiates the service object and attempts to connect to the Exchange server.
        /// </summary>
        /// <param name="server">The url of the Exchange server.</param>
        /// <param name="user">The Exchange username.</param>
        /// <param name="password">The password for the account.</param>
        public void Authenticate(string server, string user, string password)
        {
            // Don't really expect errors here because don't connect until need to - perhaps we should force it.
            this.service = new ExchangeService(ExchangeVersion.Exchange2010);
            this.service.Credentials = new NetworkCredential(user, password, string.Empty);
            string serviceUrl = server + "/EWS/Exchange.asmx";
            this.service.Url = new Uri(serviceUrl);
        }

        /// <summary>
        /// Reads the Inbox and returns it in a list of EmailItem objects.
        /// <para>Exceptions:
        /// ServiceRequestException (can't connect to server).</para>
        /// </summary>
        /// <returns>List of EmailItem objects.</returns>
        public Collection<EmailItem> ReadInbox()
        {
            Collection<EmailItem> list = new Collection<EmailItem>();

            // TODO: Rewrite this so that you can limit how much of the inbox is read at one time for large inboxes (if even necessary).
            FolderId folderId = Folder.Bind(this.service, WellKnownFolderName.Inbox).Id;
            foreach (Item item in this.GetItems(folderId))
            {
                EmailItem emailItem = new EmailItem();
                emailItem.Id = item.Id.UniqueId;
                emailItem.Subject = item.Subject;

                // Retrieve the message body (not available in FindItem results).
                Item fullItem = Item.Bind(this.service, item.Id);
                emailItem.Body = fullItem.Body;
                list.Add(emailItem);
            }

            return list;

            // TODO: Catch errors and throw ones that don't require reference to Exchange Web Services Managed API??
            //// ServiceObjectPropertyException - error reading properties of the email messages.
        }

        /// <summary>
        /// Moves a given mail item from its current location to the specified destination.
        /// </summary>
        /// <param name="id">Id provided when read from the inbox or elsewhere.</param>
        /// <param name="destinationFolderPath">Path to folder (e.g. /inbox/processed).</param>
        public void MoveMailItem(string id, string destinationFolderPath)
        {
            Item item = Item.Bind(this.service, new ItemId(id));
            
            // TODO: Process other folders than inbox.
            FolderId folderId = this.GetFolderId(destinationFolderPath);

            item.Move(folderId);
        }

        /// <summary>
        /// Creates a new ContactGroup in a folder.
        /// </summary>
        /// <param name="contactGroupName">The name to use for the ContactGroup.</param>
        /// <param name="emailAddressList">The list of email addresses to put in the ContactGroup.</param>
        /// <param name="destinationFolderPath">The path to the folder where the ContactGroup will be stored.</param>
        public void CreateContactGroup(string contactGroupName, Collection<string> emailAddressList, string destinationFolderPath)
        {
            // See if can find existing group.
            FolderId folderId = this.GetFolderId(destinationFolderPath);
            ContactGroup group = null;
            bool groupExists = false;
            foreach (Item item in this.GetItems(folderId))
            {
                if (item.GetType().Equals(typeof(ContactGroup)) && item.Subject.Equals(contactGroupName, StringComparison.InvariantCultureIgnoreCase))
                {
                    group = (ContactGroup)item;
                    group.Members.Clear();  // TODO: Figure out why this isn't working consistently.
                    groupExists = true;
                    break;
                }
            }

            // Create new group if needed.
            if (!groupExists)
            {
                group = new ContactGroup(this.service);
                group.DisplayName = contactGroupName;
            }

            // Add the members.
            foreach (string address in emailAddressList)
            {
                group.Members.AddOneOff(string.Empty, address);
            }

            // Can't use save method if group already exists.
            if (groupExists)
            {
                group.Update(ConflictResolutionMode.AlwaysOverwrite);
            }
            else
            {
                group.Save(folderId);
            }
        }

        /// <summary>
        /// Gets the FolderId for a given folder path.
        /// </summary>
        /// <param name="destinationFolderPath">Path to folder (e.g. /inbox/processed).</param>
        /// <returns>Id of the destination folder.</returns>
        private FolderId GetFolderId(string destinationFolderPath)
        {
            // Check for valid path.
            string[] folders = destinationFolderPath.Split('/');
            if (folders.Length < 2)
            {
                throw new ArgumentException("Invalid path specified: " + destinationFolderPath);
            }

            // TODO: Decide if want to support all WellKnownFolderName values using Enum.GetName(typeOf(WellKnownFolderName), (byte)Enum.GetValues(typeof(WellKnowFolderName))[0])
            // Check for supported main folder name.
            string mainFolderName = folders[1];
            WellKnownFolderName wellKnownFolder;
            if (mainFolderName.Equals("INBOX", StringComparison.InvariantCultureIgnoreCase))
            {
                wellKnownFolder = WellKnownFolderName.Inbox;
            }
            else if (mainFolderName.Equals("PUBLIC FOLDERS", StringComparison.InvariantCultureIgnoreCase))
            {
                wellKnownFolder = WellKnownFolderName.PublicFoldersRoot;
            }
            else
            {
                throw new ArgumentException("Sorry, access to '" + mainFolderName + "' is not currently supported.  The full path specified was: " + destinationFolderPath);
            }

            // See if just want main folder.
            FolderId nextFolderId = Folder.Bind(this.service, wellKnownFolder).Id;
            int position = 2;
            if (position >= folders.Length)
            {
                return nextFolderId;
            }

            // Go through subfolders until you find the folder - go in there and continue until you find the id of the dest folder.
            int offset = 0;
            const int PageSize = 100;
            FindFoldersResults result;
            bool switchingFolders = false;
            bool isLastFolder = false;
            do
            {
                switchingFolders = false;
                FolderView view = new FolderView(PageSize, offset);
                result = this.service.FindFolders(nextFolderId, view);
                foreach (Folder foundFolder in result)
                {
                    // We start at inbox level with list of folders - if any of them match the first folders array item, move there and search for the next item.
                    if (foundFolder.DisplayName.Equals(folders[position], StringComparison.InvariantCultureIgnoreCase))
                    {
                        nextFolderId = foundFolder.Id;
                        position++;
                        offset = -PageSize;
                        switchingFolders = true;
                        isLastFolder = position >= folders.Length;
                        break;
                    }
                }

                offset += PageSize;
            }
            while (!isLastFolder && (result.MoreAvailable || switchingFolders));

            if (position < folders.Length)
            {
                throw new ArgumentException("Folder '" + folders[position] + "' not found in " + destinationFolderPath);
            }

            return nextFolderId;
        }

        /// <summary>
        /// Gets all items from a given folder.
        /// </summary>
        /// <param name="folderId">The FolderId of the folder where the items are located.</param>
        /// <returns>A collection of found items.</returns>
        private Collection<Item> GetItems(FolderId folderId)
        {
            Collection<Item> list = new Collection<Item>();
            int offset = 0;
            const int PageSize = 100;
            FindItemsResults<Item> result;
            do
            {
                ItemView view = new ItemView(PageSize, offset);
                result = this.service.FindItems(folderId, view);    // Only reads a chunk of the inbox based on page size.
                foreach (Item item in result)
                {
                    list.Add(item);
                }

                offset += PageSize;
            }
            while (result.MoreAvailable);

            return list;
        }
    }
}