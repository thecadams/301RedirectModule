using System.ComponentModel;

namespace SharedSource.RedirectModule
{
    public static class Constants
    {
        public static class Paths
        {
            public static string VisitorIdentification = "/layouts/system/visitoridentification";
        }

        public static class Types
        { 
            public static string RedirExactMatch = "SharedSource.RedirectModule.RedirectionType.ExactMatch";
            public static string RedirPatternMatch = "SharedSource.RedirectModule.RedirectionType.Pattern";
            public static string QueryExactMatch = "SharedSource.RedirectModule.QueryType.ExactMatch";
            public static string QueryPatternMatch = "SharedSource.RedirectModule.QueryType.PatternMatch";
            public static string RedirectRootNode = "SharedSource.RedirectModule.RedirectRootNode";

        }
        public static class Templates
        { 
            public static string RedirectUrl = "Redirect Url";
            public static string VersionedRedirectUrl = "Versioned Redirect Url";
            public static string RedirectPattern = "Redirect Pattern";
            public static string VersionedRedirectPattern = "Versioned Redirect Pattern";
        }
        public static class Fields
        { 
            public static string RequestedUrl = "Requested Url";
            public static string RedirectTo = "redirect to";
            public static string RequestedExpression = "requested expression";
            public static string SourceItem = "souce item";
            public static string ItemProcessRedirects = "Items Which Always Process Redirects";
        }

        public static class Settings
        {
            
        }

    }
}
