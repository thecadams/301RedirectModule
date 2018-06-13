using SharedSource.RedirectModule.Helpers;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedSource.RedirectModule.Processors
{
    public class ContentEditorRedirectNotification
    {
        public void Process(Sitecore.Pipelines.GetContentEditorWarnings.GetContentEditorWarningsArgs args)
        {
            Sitecore.Diagnostics.Assert.IsNotNull(args, "args");
            Sitecore.Data.Items.Item currentItem = args.Item;
            Sitecore.Diagnostics.Assert.IsNotNull(currentItem, "args.Item");

            List<Item> redirects = GetRedirectsForItem(currentItem.ID);

            // If this item is redirect to from another item, show the notification
            foreach (Item redirectitem in redirects)
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
            Sitecore.Data.Database db = Database.GetDatabase("master");
            IEnumerable<Item> ret = null;
            var redirectRoot = Sitecore.Configuration.Settings.GetSetting(Constants.Settings.RedirectRootNode);
            ret = db.SelectItems($"{redirectRoot}//*[@Redirect To Item = '{itemID}']");

            // make sure to return an empty list instead of null
            if (ret == null)
                return new List<Item>();
            return ret.ToList<Item>();
        }

        protected void AddNotification(string message, Sitecore.Data.Items.Item item, Sitecore.Pipelines.GetContentEditorWarnings.GetContentEditorWarningsArgs args, Item redirectitem)
        {
            Sitecore.Pipelines.GetContentEditorWarnings.GetContentEditorWarningsArgs.ContentEditorWarning note = args.Add();
            note.Title = Translate.Text("Redirect Manager");
            note.Text = String.Format(Translate.Text("The URL \"{0}\" is currently redirecting to this page."), redirectitem["Requested Url"]);
            note.AddOption(Translate.Text("Review the redirect definition"), $"item:load(id={redirectitem.ID}, language={redirectitem.Language}, version={redirectitem.Version})");
            note.AddOption(Translate.Text("Delete the redirect definition"), $"redirectmanager:delete(id={redirectitem.ID}, language={redirectitem.Language}, version={redirectitem.Version}, item={item.ID})");
        }

        protected void AddLinkToRedirectItem(string message, Sitecore.Data.Items.Item item, Sitecore.Pipelines.GetContentEditorWarnings.GetContentEditorWarningsArgs args)
        {
            Database db = Sitecore.Data.Database.GetDatabase("master");
            Item target = db.GetItem(item["Redirect To Item"]);
            if (target != null)
            {
                Sitecore.Pipelines.GetContentEditorWarnings.GetContentEditorWarningsArgs.ContentEditorWarning note = args.Add();
                note.Title = Translate.Text("Redirect Manager");
                note.Text = Translate.Text("This item is a redirect item.  Would you like to manage the redirect target?");
                note.AddOption(target.DisplayName, $"item:load(id={target.ID}, language={target.Language}, version={target.Version})");
            }
        }
    }
}