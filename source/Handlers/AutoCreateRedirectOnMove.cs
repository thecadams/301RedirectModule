using System;
using System.Linq;
using SharedSource.RedirectModule.Helpers;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.SecurityModel;

namespace SharedSource.RedirectModule.Handlers
{
    public class AutoCreateRedirectOnMove
    {
        protected void OnItemMoved(object sender, EventArgs args)
        {
            //ensures arguments aren't null
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            if (Settings.GetBoolSetting(Constants.Settings.AutoGenerateRedirectsOnMove, true))
            {
                var item = Event.ExtractParameter<Item>(args, 0);
                var oldParentID = Event.ExtractParameter<ID>(args, 1);

                using (new SecurityDisabler())
                {
                    CreateRedirectItem(item, item.Database.GetItem(oldParentID));
                }
            }
        }

        public virtual void CreateRedirectItem(Item item, Item oldParentItem)
        {
            // we only want a redirect on pages and media assets & Item's which has layout assigned
            if (!oldParentItem.Paths.IsContentItem && !oldParentItem.Paths.IsMediaItem && !item.HasLayout()) return;

            string redirectNode;
            string replaceText;


            var siteInfo = item.GetMatchingSites().FirstOrDefault(q => !string.IsNullOrWhiteSpace(q.GetRedirectNode()));
            if (siteInfo == null)
            {
                redirectNode = Constants.Paths.GlobalRedirectNode();
                replaceText = item.Paths.FullPath.Replace(item.Paths.ContentPath, string.Empty).ToLower();
            }
            else
            {
                redirectNode = siteInfo.GetRedirectNode();
                replaceText = $"{siteInfo.RootPath}{siteInfo.StartItem}".ToLower();
            }


            var masterDb = Factory.GetDatabase("master");

            // Get the generated folder underneath the redirect node.  It is a bucketed item.
            var generatedFolder = masterDb.GetItem($"{redirectNode}/Generated");
            if (generatedFolder == null) return;

            // generate proper Url
            var oldParentUrl = oldParentItem.GetNavigationUrl().Replace("/sitecore/shell", string.Empty).Replace(replaceText, string.Empty);

            // Replacing aspx extension on parent item if default URL options has it
            if (!oldParentUrl.Equals("/") && oldParentUrl.Length > 0)
                oldParentUrl = $"{oldParentUrl.Replace(".aspx", string.Empty)}/";

            var oldItemUrl = oldParentUrl + item.GetNavigationUrl().Split('/').Last();

            //Now we need to get the template from which the item is created (Redirect Url)
            var template = masterDb.GetTemplate(new ID("{B5967A68-7F70-42D3-9874-0E4D001DBC20}"));
            if (template == null) return;

            // Create the item
            var newItem = generatedFolder.Add(ItemUtil.ProposeValidItemName(oldItemUrl.Replace(".aspx", string.Empty).Replace('/', ' ')), template);

            newItem.Editing.BeginEdit();
            try
            {
                // Assign values to the fields of the new item
                newItem.Fields["Requested Url"].Value = oldItemUrl;
                newItem.Fields["Response Status Code"].Value = "{3184B308-C050-4A16-9F82-D77190A28F0F}"; // 301
                newItem.Fields["Redirect To Item"].Value = item.ID.ToString();
                newItem.Editing.EndEdit();
            }
            catch (Exception ex)
            {
                // The update failed, write a message to the log
                Log.Error("Could not update item " + newItem.Paths.FullPath + ": " + ex.Message, this);
                newItem.Editing.CancelEdit();
            }
        }
    }
}