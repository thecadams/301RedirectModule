using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Resources.Media;
using Sitecore.Sites;
using Sitecore.Web;

namespace SharedSource.RedirectModule.Helpers
{
    /// <summary>
    /// Item Extension class
    /// </summary>
    public static class Extensions
    {
        public static int GetIntegerFieldValue(this Item item, string fieldName, int defaultValue)
        {
            if (item == null)
            {
                return defaultValue;
            }

            var field = item.Fields[fieldName];

            if (field != null && field.HasValue)
            {
                if (int.TryParse(field.Value, out var result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        public static string GetConfigValue(this string configName) => Sitecore.Configuration.Settings.GetSetting(configName) ?? string.Empty;

        public static bool HasLayout(this Item item)
        {
            Assert.IsNotNull(item, "Item cannot be null");

            return item?.Visualization?.Layout != null;
        }

        public static string GetNavigationUrl(this Item item)
        {
            Assert.IsNotNull(item, "Item cannot be null");

            var urlOptions = LinkManager.GetDefaultUrlOptions();

            var mediaOptions = new MediaUrlOptions
            {
                // Customize parameters as per need
                LowercaseUrls = true
            };

            return !item.Paths.IsMediaItem ? LinkManager.GetItemUrl(item, urlOptions).ToLower() : MediaManager.GetMediaUrl(item, mediaOptions).ToLower();
        }

        /// <summary>
        ///     Return the Site Redirect Node
        /// </summary>
        /// <returns></returns>
        public static string GetRedirectNode(this SiteContext site)
        {
            try
            {
                var redirectNode = site.Properties["redirectNode"];
                return !string.IsNullOrEmpty(redirectNode) ? redirectNode : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string GetRedirectNode(this SiteInfo site)
        {
            try
            {
                var redirectNode = site.Properties["redirectNode"];
                return !string.IsNullOrEmpty(redirectNode) ? redirectNode : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the site info from the <see cref="SiteContextFactory"/> based on the item's path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The <see cref="SiteInfo"/>.</returns>
        public static IEnumerable<SiteInfo> GetMatchingSites(this Item item)
        {
            var contentPath = item.Paths.ContentPath.ToLower();

            return SiteContextFactory.Sites
                .Where(q => !string.IsNullOrWhiteSpace(q.RootPath)
                            && !string.IsNullOrWhiteSpace(q.StartItem)
                            && q.VirtualFolder.Equals("/")
                            && contentPath.StartsWith(q.StartItem.ToLower()))
                .OrderBy(q => q.Name);
        }
    }
}
