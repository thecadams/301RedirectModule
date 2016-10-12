using System;
using System.Web;

namespace SharedSource.RedirectManager.Rules.Actions
{
    public class ReplaceString<T> : Sitecore.Rules.Actions.RuleAction<T> where T : Sitecore.Rules.RuleContext
    {
        public string Old { get; set; }
        public string New { get; set; }

        public override void Apply(T ruleContext)
        {
            if (ruleContext.Parameters["newUrl"] != null)                           
                ruleContext.Parameters["newUrl"] = Convert.ToString(ruleContext.Parameters["newUrl"]).Replace(Old, New);            
        }
    }
}