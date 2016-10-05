using System.Web;
using Sitecore;
using Sitecore.Data;
using System;
using Sitecore.Data.Items;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Sitecore.Links;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;

namespace SharedSource.RedirectModule
{
    /// <summary>
    ///  Redirection Module which handles 301 redirects.  Both exact matches and regular expression pattern matches are supported.
    /// </summary>
    public class Redirects : HttpRequestProcessor
    {
        /// <summary>
        ///  The main method for the processor.  It simply overrides the Process method.
        /// </summary>
        public override void Process(HttpRequestArgs args)
        {
            // This processer is added to the pipeline after the Sitecore Item Resolver.  We want to skip everything if the item resolved successfully.
            // Also, skip processing for the visitor identification items related to DMS.
            Assert.ArgumentNotNull(args, "args");
            if ((Context.Item == null || AllowRedirectsOnFoundItem(Context.Database)) && args.LocalPath != Constants.Paths.VisitorIdentification && Context.Database != null)
            {
                // Grab the actual requested path for use in both the item and pattern match sections.
                var requestedUrl = HttpContext.Current.Request.Url.ToString();
                var requestedPath = HttpContext.Current.Request.Url.AbsolutePath;
                var requestedPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
                var db = Context.Database;

                // First, we check for exact matches because those take priority over pattern matches.
                if (Sitecore.Configuration.Settings.GetBoolSetting(Constants.Settings.RedirExactMatch, true))
                {
                    // Loop through the exact match entries to look for a match.
                    foreach (Item possibleRedirect in GetRedirects(db, Constants.Templates.RedirectUrl, Constants.Templates.VersionedRedirectUrl, Sitecore.Configuration.Settings.GetSetting(Constants.Settings.QueryExactMatch)))
                    {
                        if (requestedUrl.Equals(possibleRedirect[Constants.Fields.RequestedUrl], StringComparison.OrdinalIgnoreCase) ||
                             requestedPath.Equals(possibleRedirect[Constants.Fields.RequestedUrl], StringComparison.OrdinalIgnoreCase))
                        {
                            var redirectToItemId = possibleRedirect.Fields[Constants.Fields.RedirectToItem];
                            var redirectToUrl = possibleRedirect.Fields[Constants.Fields.RedirectToUrl];

                            if (redirectToItemId.HasValue && !string.IsNullOrEmpty(redirectToItemId.ToString()))
                            {
                                var redirectToItem = db.GetItem(ID.Parse(redirectToItemId));

                                if (redirectToItem != null)
                                {
                                    var responseStatus = GetResponseStatus(possibleRedirect);

                                    SendResponse(redirectToItem, HttpContext.Current.Request.Url.Query, responseStatus, args);
                                }
                            }
                            else if (redirectToUrl.HasValue && !string.IsNullOrEmpty(redirectToUrl.ToString()))
                            {
                                var responseStatus = GetResponseStatus(possibleRedirect);

                                SendResponse(redirectToUrl.Value, HttpContext.Current.Request.Url.Query, responseStatus, args);
                            }
                        }
                    }
                }

                // Finally, we check for pattern matches because we didn't hit on an exact match.
                if (Sitecore.Configuration.Settings.GetBoolSetting(Constants.Settings.RedirPatternMatch, true))
                {
                    // Loop through the pattern match items to find a match
                    foreach (Item possibleRedirectPattern in GetRedirects(db, Constants.Templates.RedirectPattern, Constants.Templates.VersionedRedirectPattern, Sitecore.Configuration.Settings.GetSetting(Constants.Settings.QueryExactMatch)))
                    {
                        var redirectPath = string.Empty;
                        if (Regex.IsMatch(requestedUrl, possibleRedirectPattern[Constants.Fields.RequestedExpression], RegexOptions.IgnoreCase))
                        {
                            redirectPath = Regex.Replace(requestedUrl, possibleRedirectPattern[Constants.Fields.RequestedExpression],
                                                         possibleRedirectPattern[Constants.Fields.SourceItem], RegexOptions.IgnoreCase);
                        }
                        else if (Regex.IsMatch(requestedPathAndQuery, possibleRedirectPattern[Constants.Fields.RequestedExpression], RegexOptions.IgnoreCase))
                        {
                            redirectPath = Regex.Replace(requestedPathAndQuery,
                                                         possibleRedirectPattern[Constants.Fields.RequestedExpression],
                                                         possibleRedirectPattern[Constants.Fields.SourceItem], RegexOptions.IgnoreCase);
                        }
                        if (string.IsNullOrEmpty(redirectPath)) continue;

                        // Query portion gets in the way of getting the sitecore item.
                        var pathAndQuery = redirectPath.Split('?');
                        var path = pathAndQuery[0];
                        if (LinkManager.Provider != null &&
                            LinkManager.Provider.GetDefaultUrlOptions() != null &&
                            LinkManager.Provider.GetDefaultUrlOptions().EncodeNames)
                        {
                            path = MainUtil.DecodeName(path);
                        }
                        var redirectToItem = db.GetItem(path);
                        if (redirectToItem != null)
                        {
                            var query = pathAndQuery.Length > 1 ? "?" + pathAndQuery[1] : "";
                            var responseStatus = GetResponseStatus(possibleRedirectPattern);

                            SendResponse(redirectToItem, query, responseStatus, args);
                        }
                    }
                }
            }
        }

        private static bool AllowRedirectsOnFoundItem(Database db)
        {
            if (db == null)
                return false;
            var redirectRoot = Sitecore.Configuration.Settings.GetSetting(Constants.Settings.RedirectRootNode);
            var redirectFolderRoot = db.SelectSingleItem(redirectRoot);
            if (redirectFolderRoot == null)
                return false;
            var allowRedirectsOnItemIDs = redirectFolderRoot[Constants.Fields.ItemProcessRedirects];
            return allowRedirectsOnItemIDs != null &&
                      allowRedirectsOnItemIDs.Contains(Context.Item.ID.ToString());
        }

        /// <summary>
        ///  This method return all of the possible matches for either the exact matches or the pattern matches
        ///  Note: Because Fast Query does not guarantee to return items in the current language context
        ///  (e.g. while in US/English, results may include other language items as well, even if the 
        ///  US/EN language has no active versions), an additional LINQ query has to be run to filter for language.
        ///  Choose your query type appropriately.
        /// </summary>
        private static IEnumerable<Item> GetRedirects(Database db, string templateName, string versionedTemplateName, string queryType)
        {
            // Based off the config file, we can run different types of queries. 
            IEnumerable<Item> ret = null;
            var redirectRoot = Sitecore.Configuration.Settings.GetSetting(Constants.Settings.RedirectRootNode);
            switch (queryType)
            {
                case "fast": // fast query
                    {
                        //process shared template items
                        ret = db.SelectItems(String.Format("fast:{0}//*[@@templatename='{1}']", redirectRoot, templateName));

                        //because fast query requires to check for active versions in the current language
                        //run a separate query for versioned items to see if this is even necessary.
                        //if only shared templates exist in System/Modules, this step is extraneous and unnecessary.
                        IEnumerable<Item> versionedItems = db.SelectItems(String.Format("fast:{0}//*[@@templatename='{1}']", redirectRoot, versionedTemplateName));

                        //if active versions of items in the current context exist, union the two IEnumerable lists together.
                        ret = versionedItems.Any(i => i.Versions.Count > 0)
                            ? ret.Union(versionedItems.Where(i => i.Versions.Count > 0))
                            : ret;
                        break;
                    }
                case "query": // Sitecore query
                    {
                        ret = db.SelectItems(String.Format("{0}//*[@@templatename='{1}' or @@templatename='{2}']", redirectRoot, templateName, versionedTemplateName));
                        break;
                    }
                default: // API LINQ
                    {
                        Item redirectFolderRoot = db.SelectSingleItem(redirectRoot);
                        if (redirectFolderRoot != null)
                            ret = redirectFolderRoot.Axes.GetDescendants().Where(i => i.TemplateName == templateName || i.TemplateName == versionedTemplateName);
                        break;
                    }
            }

            // make sure to return an empty list instead of null
            return ret ?? new Item[0];
        }

        public static List<Item> GetRedirectsForItem(ID itemID)
        {
            // Based off the config file, we can run different types of queries. 
            Sitecore.Data.Database db = Database.GetDatabase("master");
            IEnumerable<Item> ret = null;
            var redirectRoot = Sitecore.Configuration.Settings.GetSetting(Constants.Settings.RedirectRootNode);
            ret = db.SelectItems(String.Format("{0}//*[@Redirect To Item = '{1}']", redirectRoot, itemID.ToString()));

            // make sure to return an empty list instead of null
            if (ret == null)
                return new List<Item>();
            return ret.ToList<Item>();
        }

        /// <summary>
        ///  Once a match is found and we have a Sitecore Item, we can send the response.
        /// </summary>
        private static void SendResponse(Item redirectToItem, string queryString, ResponseStatus responseStatus, HttpRequestArgs args)
        {
            var redirectToUrl = GetRedirectToItemUrl(redirectToItem);
            SendResponse(redirectToUrl, queryString, responseStatus, args);
        }

        private static void SendResponse(string redirectToUrl, string queryString, ResponseStatus responseStatusCode, HttpRequestArgs args)
        {
            args.Context.Response.Status = responseStatusCode.Status;
            args.Context.Response.StatusCode = responseStatusCode.StatusCode;
            args.Context.Response.AddHeader("Location", redirectToUrl + queryString);
            args.Context.Response.End();
        }

        private static string GetRedirectToItemUrl(Item redirectToItem)
        {
            if (redirectToItem.Paths.Path.StartsWith(Constants.Paths.MediaLibrary))
            {
                var mediaItem = (MediaItem)redirectToItem;
                var mediaUrl = MediaManager.GetMediaUrl(mediaItem);
                var redirectToUrl = StringUtil.EnsurePrefix('/', mediaUrl);
                return redirectToUrl;
            }

            return LinkManager.GetItemUrl(redirectToItem);
        }

        private static ResponseStatus GetResponseStatus(Item redirectItem)
        {
            var result = new ResponseStatus
            {
                Status = "301 Moved Permanently",
                StatusCode = 301,
            };

            if (redirectItem != null)
            {
                var responseStatusCodeId = redirectItem.Fields[Constants.Fields.ResponseStatusCode];

                if (responseStatusCodeId != null && responseStatusCodeId.HasValue && !string.IsNullOrEmpty(responseStatusCodeId.ToString()))
                {      
                    var responseStatusCodeItem = redirectItem.Database.GetItem(ID.Parse(responseStatusCodeId));

                    if (responseStatusCodeItem != null)
                    {
                        result.Status = responseStatusCodeItem.Name;
                        result.StatusCode = responseStatusCodeItem.GetIntegerFieldValue(Constants.Fields.StatusCode, result.StatusCode);
                    }
                }
            }

            return result;
        }
    }
}

