using System.ComponentModel;

namespace SharedSource.RedirectModule
{
    public static class Constants
    {
        public static class Paths
        {
            public static string VisitorIdentification = "/layouts/system/visitoridentification";
            public static string MediaLibrary = "/sitecore/media library/";
        }

        public static class Settings
        {
            public static string RedirExactMatch = "SharedSource.RedirectModule.RedirectionType.ExactMatch";
            public static string RedirPatternMatch = "SharedSource.RedirectModule.RedirectionType.Pattern";
            public static string QueryExactMatch = "SharedSource.RedirectModule.QueryType.ExactMatch";
            public static string QueryPatternMatch = "SharedSource.RedirectModule.QueryType.PatternMatch";
            public static string RedirectRootNode = "SharedSource.RedirectModule.RedirectRootNode";
            public static string AutoGenerateRedirectsOnMove = "SharedSource.RedirectModule.AutoGenerateRedirectsOnMove";

        }
        public static class Templates
        { 
            public static string RedirectUrl = "Redirect Url";
            public static string VersionedRedirectUrl = "Versioned Redirect Url";
            public static string RedirectPattern = "Redirect Pattern";
            public static string VersionedRedirectPattern = "Versioned Redirect Pattern";
            public static string ResponseStatusCodeFolder = "Response Status Code Folder";
            public static string ResponseStatusCode = "Response Status Code";
        }
        public static class Fields
        { 
            public static string RequestedUrl = "Requested Url";
            public static string RedirectToItem = "redirect to item";
            public static string RedirectToUrl = "redirect to url";
            public static string RequestedExpression = "requested expression";
            public static string SourceItem = "source item";
            public static string ItemProcessRedirects = "Items Which Always Process Redirects";
            public static string ResponseStatusCode = "Response Status Code";
            public static string StatusCode = "Status Code";
        }

    }
}
