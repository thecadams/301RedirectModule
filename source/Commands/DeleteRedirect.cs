using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedSource.RedirectModule.Commands
{
    class DeleteRedirect : Command
    {
        public override void Execute(CommandContext context)
        {
            // execute the delete
            if (context.Items.Length == 0)
            {
                SheerResponse.Alert("The selected item could not be found.\n\nIt may have been deleted by another user.\n\nSelect another item.");
            }
            else
            {
                if (context.Items.Length == 1)
                    Sitecore.Data.Database.GetDatabase("master").GetItem(((Item)context.Items[0]).ID).Delete();
            }

            // force the item to refresh
            Sitecore.Data.Items.Item myItem = Sitecore.Data.Database.GetDatabase("master").GetItem(new ID(context.Parameters[3]));
            if (myItem != null)
            {
                string load = String.Concat(new object[] { "item:load(id=", myItem.ID, ",language=", myItem.Language, ",version=", myItem.Version, ")" });
                Sitecore.Context.ClientPage.SendMessage(this, load);
            }
        }
    }
}
