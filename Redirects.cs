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
			if ((Context.Item == null || AllowRedirectsOnFoundItem(Context.Database)) && args.LocalPath != "/layouts/system/visitoridentification" && Context.Database != null)
			{
				// Grab the actual requested path for use in both the item and pattern match sections.
				var requestedUrl = HttpContext.Current.Request.Url.ToString();
				var requestedPath = HttpContext.Current.Request.Url.AbsolutePath;
			    var requestedPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
				var db = Context.Database;

				// First, we check for exact matches because those take priority over pattern matches.
				if (Sitecore.Configuration.Settings.GetBoolSetting("SharedSource.RedirectModule.RedirectionType.ExactMatch", true))
				{
					// Loop through the exact match entries to look for a match.
					foreach (Item possibleRedirect in GetRedirects(db, "Redirect Url", Sitecore.Configuration.Settings.GetSetting("SharedSource.RedirectModule.QueryType.ExactMatch")))
					{						
						if (requestedUrl.Equals(possibleRedirect["Requested Url"], StringComparison.OrdinalIgnoreCase) ||
							 requestedPath.Equals(possibleRedirect["Requested Url"], StringComparison.OrdinalIgnoreCase))
						{
							var redirectToItem = db.GetItem(ID.Parse(possibleRedirect.Fields["redirect to"]));
							if (redirectToItem != null)
							{
								SendResponse(redirectToItem, HttpContext.Current.Request.Url.Query, args);
							}
						}						
					}
				}

				// Second, we check for pattern matches because we didn't hit on an exact match.
				if (Sitecore.Configuration.Settings.GetBoolSetting("SharedSource.RedirectModule.RedirectionType.Pattern", true))
				{
					// Loop through the pattern match items to find a match
					foreach (Item possibleRedirectPattern in GetRedirects(db, "Redirect Pattern", Sitecore.Configuration.Settings.GetSetting("SharedSource.RedirectModule.QueryType.ExactMatch")))
					{
						if (Regex.IsMatch(requestedUrl, possibleRedirectPattern["requested expression"], RegexOptions.IgnoreCase))
						{
							var redirectUri = new Uri(Regex.Replace(requestedUrl, possibleRedirectPattern["requested expression"], possibleRedirectPattern["source item"], RegexOptions.IgnoreCase));
							var redirectToItem = db.GetItem(redirectUri.AbsolutePath);
							if (redirectToItem != null)
							{
								SendResponse(redirectToItem, redirectUri.Query, args);
							}
						}
						else if (Regex.IsMatch(requestedPathAndQuery, possibleRedirectPattern["requested expression"], RegexOptions.IgnoreCase))
						{
                            var redirectPath = Regex.Replace(requestedPathAndQuery, possibleRedirectPattern["requested expression"], possibleRedirectPattern["source item"], RegexOptions.IgnoreCase);
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
                                SendResponse(redirectToItem, query, args);
							}
						}
					}
				}
			}
		}

		 private static bool AllowRedirectsOnFoundItem(Database db)
		 {
			  if (db == null)
					return false;
			  var redirectRoot = Sitecore.Configuration.Settings.GetSetting("SharedSource.RedirectModule.RedirectRootNode");
			  var redirectFolderRoot = db.SelectSingleItem(redirectRoot);
			  if (redirectFolderRoot == null)
					return false;
			  var allowRedirectsOnItemIDs = redirectFolderRoot["Items Which Always Process Redirects"];
			  return allowRedirectsOnItemIDs != null &&
						allowRedirectsOnItemIDs.Contains(Context.Item.ID.ToString());
		 }

		/// <summary>
		///  This method return all of the possible matches for either the exact matches or the pattern matches
		/// </summary>
		private static IEnumerable<Item> GetRedirects(Database db, string templateName, string queryType)
		{			
			// Based off the config file, we can run different types of queries. 
			IEnumerable<Item> ret = null;
			var redirectRoot = Sitecore.Configuration.Settings.GetSetting("SharedSource.RedirectModule.RedirectRootNode");
			switch (queryType)
			{
				case "fast": // fast query
				{
					ret = db.SelectItems(String.Format("fast:{0}//*[@@templatename='{1}']", redirectRoot, templateName));
					break;
				}
				case "query": // Sitecore query
				{
					ret = db.SelectItems(String.Format("{0}//*[@@templatename='{1}']", redirectRoot, templateName));
					break;
				}				
				default: // API LINQ
				{
					Item redirectFolderRoot = db.SelectSingleItem(redirectRoot);
					if (redirectFolderRoot != null)
						ret = redirectFolderRoot.Axes.GetDescendants().Where(i => i.TemplateName == templateName);
					break;
				}
			}	

			// make sure to return an empty list instead of null
			return ret ?? new Item[0];
		}

		/// <summary>
		///  Once a match is found and we have a Sitecore Item, we can send the 301 response.
		/// </summary>
		private static void SendResponse(Item redirectToItem, string queryString, HttpRequestArgs args)
		{
			var redirectToUrl = LinkManager.GetItemUrl(redirectToItem);
			args.Context.Response.Status = "301 Moved Permanently";
			args.Context.Response.StatusCode = 301;
			args.Context.Response.AddHeader("Location", redirectToUrl + queryString);
			args.Context.Response.End();
		}
	}
}

