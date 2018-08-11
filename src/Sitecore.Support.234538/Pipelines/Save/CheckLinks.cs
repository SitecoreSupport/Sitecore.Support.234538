using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.Save;
using System;
using System.Linq;
using System.Text;

namespace Sitecore.Support.Pipelines.Save
{
    public class CheckLinks
    {
        public void Process(SaveArgs args)
        {
            if (args.HasSheerUI)
            {
                if ((args.Result == "no") || (args.Result == "undefined"))
                {
                    args.AbortPipeline();
                }
                else
                {
                    int @int = 0;
                    if (args.Parameters["LinkIndex"] == null)
                    {
                        args.Parameters["LinkIndex"] = "0";
                    }
                    else
                    {
                        @int = MainUtil.GetInt(args.Parameters["LinkIndex"], 0);
                    }
                    for (int i = 0; i < args.Items.Length; i++)
                    {
                        if (i >= @int)
                        {
                            @int++;
                            SaveArgs.SaveItem item = args.Items[i];
                            Item item2 = Context.ContentDatabase.Items[item.ID, item.Language, item.Version];
                            if (item2 != null)
                            {
                                item2.Editing.BeginEdit();
                                foreach (SaveArgs.SaveField field in item.Fields)
                                {
                                    Field field2 = item2.Fields[field.ID];
                                    if (field2 != null)
                                    {
                                        if (!string.IsNullOrEmpty(field.Value))
                                        {
                                            field2.Value = field.Value;
                                        }
                                        else
                                        {
                                            field2.Value = null;
                                        }
                                    }
                                }
                                bool allVersions = false;
                                ItemLink[] brokenLinks = item2.Links.GetBrokenLinks(allVersions);
                                if (brokenLinks.Length != 0)
                                {
                                    ShowDialog(item2, brokenLinks);
                                    args.WaitForPostBack();
                                    break;
                                }
                                item2.Editing.CancelEdit();
                            }
                        }
                    }
                    args.Parameters["LinkIndex"] = @int.ToString();
                }
            }
        }

        private static void ShowDialog(Item item, ItemLink[] links)
        {
            object[] parameters = new object[] { item.DisplayName };
            StringBuilder builder = new StringBuilder(Translate.Text("The item \"{0}\" contains broken links in these fields:\n\n", parameters));
            bool flag = false;
            if (links.Count<ItemLink>() > 0)
            {
                builder.Append("<table style='word-break:break-all;'>");
                builder.Append("<tbody>");
                foreach (ItemLink link in links)
                {
                    if (!link.SourceFieldID.IsNull)
                    {
                        builder.Append("<tr>");
                        builder.Append("<td style='width:70px;vertical-align:top;padding-bottom:5px;padding-right:5px;'>");
                        if (item.Fields.Contains(link.SourceFieldID))
                        {
                            builder.Append(item.Fields[link.SourceFieldID].DisplayName);
                        }
                        else
                        {
                            object[] objArray2 = new object[] { link.SourceFieldID.ToString() };
                            builder.Append(Translate.Text("[Unknown field: {0}]", objArray2));
                        }
                        builder.Append("</td>");
                        builder.Append("<td style='vertical-align:top;padding-bottom:5px;'>");
                        if (!string.IsNullOrEmpty(link.TargetPath) && !ID.IsID(link.TargetPath))
                        {
                            builder.Append(link.TargetPath);
                        }
                        builder.Append("</td>");
                        builder.Append("</tr>");
                    }
                    else
                    {
                        flag = true;
                    }
                }
                builder.Append("</tbody></table>");
            }
            if (flag)
            {
                builder.Append("<br />");
                builder.Append(Translate.Text("The template or branch for this item is missing."));
            }
            builder.Append("<br />");
            builder.Append(Translate.Text("Do you want to save anyway?"));
            Context.ClientPage.ClientResponse.Confirm(builder.ToString());
        }
    }
}