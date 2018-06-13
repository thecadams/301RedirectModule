using System.Collections.Generic;
using System.Linq;
using SharedSource.RedirectModule.Helpers;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Sites;

namespace SharedSource.RedirectModule.Processors
{
    public class ContentEditorRedirectNotification
    {
        public void Process(GetContentEditorWarningsArgs args)
        {
            Assert.IsNotNull(args, "args");
            var currentItem = args.Item;
            Assert.IsNotNull(currentItem, "args.Item");

            var redirects = GetRedirectsForItem(currentItem.ID);

            // If this item is redirect to from another item, show the notification
            foreach (var redirectitem in redirects)
            {
                AddNotification(string.Empty, currentItem, args, redirectitem);
            }

            // If this is a redirect item link to the item in the notification bar.
            if (currentItem["Redirect To Item"] != null && currentItem["Redirect To Item"] != string.Empty)
                AddLinkToRedirectItem(string.Empty, currentItem, args);
        }

        public virtual List<Item> GetRedirectsForItem(ID itemID)
        {
            // Based off the config file, we can run different types of queries.
            var db = Database.GetDatabase("master");
            var result = new List<Item>();

            // Get Redirect Node from All Sites, Can implement Cache if needed
            var redirectNodes = SiteContextFactory.Sites
                .Where(q => !string.IsNullOrWhiteSpace(q.RootPath)
                            && !string.IsNullOrWhiteSpace(q.StartItem)
                            && !string.IsNullOrWhiteSpace(q.GetRedirectNode()))
                .Select(q => q.GetRedirectNode())
                .ToList();

            if (!string.IsNullOrWhiteSpace(Constants.Paths.GlobalRedirectNode()))
                redirectNodes.Add(Constants.Paths.GlobalRedirectNode());

            foreach (var redirectNode in redirectNodes)
            {
                var redirects = db.SelectItems($"{redirectNode}//*[@Redirect To Item = '{itemID}']");
                if (redirects?.Length > 0)
                    result.AddRange(redirects);
            }

            return result;
        }

        protected void AddNotification(string message, Item item, GetContentEditorWarningsArgs args, Item redirectitem)
        {
            var note = args.Add();
            note.Title = Translate.Text("Redirect Manager");
            note.Text = string.Format(Translate.Text("The URL \"{0}\" is currently redirecting to this page."), redirectitem["Requested Url"]);
            note.AddOption(Translate.Text("Review the redirect definition"), $"item:load(id={redirectitem.ID}, language={redirectitem.Language}, version={redirectitem.Version})");
            note.AddOption(Translate.Text("Delete the redirect definition"), $"redirectmanager:delete(id={redirectitem.ID}, language={redirectitem.Language}, version={redirectitem.Version}, item={item.ID})");
        }

        protected void AddLinkToRedirectItem(string message, Item item, GetContentEditorWarningsArgs args)
        {
            var db = Database.GetDatabase("master");
            var target = db.GetItem(item["Redirect To Item"]);
            if (target != null)
            {
                var note = args.Add();
                note.Title = Translate.Text("Redirect Manager");
                note.Text = Translate.Text("This item is a redirect item.  Would you like to manage the redirect target?");
                note.AddOption(target.DisplayName, $"item:load(id={target.ID}, language={target.Language}, version={target.Version})");
            }
        }
    }
}