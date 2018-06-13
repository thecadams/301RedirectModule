using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using SharedSource.RedirectModule.Classes;
using SharedSource.RedirectModule.Helpers;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Resources.Media;
using Sitecore.Rules;
using Constants = SharedSource.RedirectModule.Helpers.Constants;

namespace SharedSource.RedirectModule.Processors
{
    /// <summary>
    ///     Redirection Module which handles redirects.  Both exact matches and regular expression pattern matches are
    ///     supported.
    /// </summary>
    public class RedirectProcessor : HttpRequestProcessor
    {
        /// <summary>
        ///     The main method for the processor.  It simply overrides the Process method.
        /// </summary>
        public override void Process(HttpRequestArgs args)
        {
            // This processor is added to the pipeline after the Sitecore Item Resolver.  We want to skip everything if the item resolved successfully.
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
                if (Settings.GetBoolSetting(Constants.Settings.RedirExactMatch, true)) CheckForDirectMatch(db, requestedUrl, requestedPath, args);

                // Next, we check for pattern matches because we didn't hit on an exact match.
                if (Settings.GetBoolSetting(Constants.Settings.RedirPatternMatch, true)) CheckForRegExMatch(db, requestedUrl, requestedPathAndQuery, args);

                // Next, we check for rule matches because we didn't hit on an exact match or pattern match.
                if (Settings.GetBoolSetting(Constants.Settings.RedirRuleMatch, true)) CheckForRulesMatch(db, requestedUrl, requestedPathAndQuery, args);
            }
        }

        private void CheckForDirectMatch(Database db, string requestedUrl, string requestedPath, HttpRequestArgs args)
        {
            // Loop through the exact match entries to look for a match.
            foreach (var possibleRedirect in GetRedirects(db, Constants.Templates.RedirectUrl, Constants.Templates.VersionedRedirectUrl, Settings.GetSetting(Constants.Settings.QueryExactMatch)))
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

        private void CheckForRegExMatch(Database db, string requestedUrl, string requestedPathAndQuery, HttpRequestArgs args)
        {
            // Loop through the pattern match items to find a match
            foreach (var possibleRedirectPattern in GetRedirects(db, Constants.Templates.RedirectPattern, Constants.Templates.VersionedRedirectPattern, Settings.GetSetting(Constants.Settings.QueryExactMatch)))
            {
                var redirectPath = string.Empty;
                if (Regex.IsMatch(requestedUrl, possibleRedirectPattern[Constants.Fields.RequestedExpression], RegexOptions.IgnoreCase))
                    redirectPath = Regex.Replace(requestedUrl, possibleRedirectPattern[Constants.Fields.RequestedExpression],
                        possibleRedirectPattern[Constants.Fields.SourceItem], RegexOptions.IgnoreCase);
                else if (Regex.IsMatch(requestedPathAndQuery, possibleRedirectPattern[Constants.Fields.RequestedExpression], RegexOptions.IgnoreCase))
                    redirectPath = Regex.Replace(requestedPathAndQuery,
                        possibleRedirectPattern[Constants.Fields.RequestedExpression],
                        possibleRedirectPattern[Constants.Fields.SourceItem], RegexOptions.IgnoreCase);
                if (string.IsNullOrEmpty(redirectPath)) continue;

                // Query portion gets in the way of getting the sitecore item.
                var pathAndQuery = redirectPath.Split('?');
                var path = pathAndQuery[0];
#if (SC82)
                if (LinkManager.Provider != null &&
                    LinkManager.Provider.GetDefaultUrlOptions() != null &&
                    LinkManager.Provider.GetDefaultUrlOptions().EncodeNames)
#else
                if (LinkManager.GetDefaultUrlOptions() != null &&
                    LinkManager.GetDefaultUrlOptions().EncodeNames)
#endif
                    path = MainUtil.DecodeName(path);
                var redirectToItem = db.GetItem(path);
                if (redirectToItem != null)
                {
                    var query = pathAndQuery.Length > 1 ? "?" + pathAndQuery[1] : "";
                    var responseStatus = GetResponseStatus(possibleRedirectPattern);

                    SendResponse(redirectToItem, query, responseStatus, args);
                }
            }
        }

        private void CheckForRulesMatch(Database db, string requestedUrl, string requestedPathAndQuery, HttpRequestArgs args)
        {
            // Loop through the pattern match items to find a match
            foreach (var possibleRedirectRule in GetRedirects(db, Constants.Templates.RedirectRule, Constants.Templates.VersionedRedirectRule, Settings.GetSetting(Constants.Settings.QueryExactMatch)))
            {
                var ruleContext = new RuleContext();
                ruleContext.Parameters.Add("newUrl", requestedUrl);

                foreach (var rule in RuleFactory.GetRules<RuleContext>(new Item[1] {possibleRedirectRule}, "Redirect Rule").Rules)
                {
                    if (rule.Condition != null)
                    {
                        var stack = new RuleStack();
                        rule.Condition.Evaluate(ruleContext, stack);
                        if (!ruleContext.IsAborted && stack.Count != 0 && (bool) stack.Pop())
                            foreach (var action in rule.Actions)
                            {
                                action.Apply(ruleContext);
                            }
                    }
                }

                if (ruleContext.Parameters["newUrl"] != null && ruleContext.Parameters["newUrl"].ToString() != string.Empty && ruleContext.Parameters["newUrl"].ToString() != requestedUrl)
                {
                    var responseStatus = GetResponseStatus(possibleRedirectRule);
                    // The query string will be in the URL already, so don't break it apart.
                    SendResponse(ruleContext.Parameters["newUrl"].ToString(), string.Empty, responseStatus, args);
                }
            }
        }

        private static bool AllowRedirectsOnFoundItem(Database db)
        {
            if (db == null)
                return false;
            var redirectRoot = Settings.GetSetting(Constants.Settings.RedirectRootNode);
            var redirectFolderRoot = db.SelectSingleItem(redirectRoot);
            if (redirectFolderRoot == null)
                return false;
            var allowRedirectsOnItemIDs = redirectFolderRoot[Constants.Fields.ItemProcessRedirects];
            return allowRedirectsOnItemIDs != null &&
                   allowRedirectsOnItemIDs.Contains(Context.Item.ID.ToString());
        }

        /// <summary>
        ///     This method return all of the possible matches for either the exact matches or the pattern matches
        ///     Note: Because Fast Query does not guarantee to return items in the current language context
        ///     (e.g. while in US/English, results may include other language items as well, even if the
        ///     US/EN language has no active versions), an additional LINQ query has to be run to filter for language.
        ///     Choose your query type appropriately.
        /// </summary>
        public virtual IEnumerable<Item> GetRedirects(Database db, string templateName, string versionedTemplateName, string queryType)
        {
            var redirectsList = new List<Item>();
            AddRedirectItemsFromNode(Context.Site.GetRedirectNode());
            AddRedirectItemsFromNode(Constants.Paths.GlobalRedirectNode());

            void AddRedirectItemsFromNode(string redirectNode)
            {
                if (string.IsNullOrWhiteSpace(redirectNode)) return;

                // process shared template items
                // Based off the config file, we can run different types of queries.

                switch (queryType)
                {
                    case "fast": // fast query
                    {
                        var items = db.SelectItems($"fast:{redirectNode}//*[@@templatename='{templateName}']");

                        //because fast query requires to check for active versions in the current language
                        //run a separate query for versioned items to see if this is even necessary.
                        //if only shared templates exist in System/Modules, this step is extraneous and unnecessary.
                        var itemsVersioned = db.SelectItems($"fast:{redirectNode}//*[@@templatename='{versionedTemplateName}']");

                        items = itemsVersioned.Any(q => q.Versions.Count > 0)
                            ? items.Union(itemsVersioned.Where(q => q.Versions.Count > 0)).ToArray()
                            : items;

                        redirectsList.AddRange(items);

                        break;
                    }
                    case "query": // Sitecore query
                    {
                        var items = db.SelectItems($"{redirectNode}//*[@@templatename='{templateName}' or @@templatename='{versionedTemplateName}']");
                        redirectsList.AddRange(items);

                        break;
                    }
                    default: // API LINQ
                    {
                        var redirectFolderRoot = db.SelectSingleItem(redirectNode);

                        if (redirectFolderRoot != null)
                            redirectsList.AddRange(redirectFolderRoot.Axes
                                .GetDescendants()
                                .Where(i => i.TemplateName == templateName || i.TemplateName == versionedTemplateName));
                        break;
                    }
                }
            }

            return redirectsList;
        }

        /// <summary>
        ///     Once a match is found and we have a Sitecore Item, we can send the response.
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
                var mediaItem = (MediaItem) redirectToItem;
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
                StatusCode = 301
            };

            var responseStatusCodeId = redirectItem?.Fields[Constants.Fields.ResponseStatusCode];

            if (string.IsNullOrEmpty(responseStatusCodeId?.ToString())) return result;

            var responseStatusCodeItem = redirectItem.Database.GetItem(ID.Parse(responseStatusCodeId));

            if (responseStatusCodeItem != null)
            {
                result.Status = responseStatusCodeItem.Name;
                result.StatusCode = responseStatusCodeItem.GetIntegerFieldValue(Constants.Fields.StatusCode, result.StatusCode);
            }

            return result;
        }
    }
}