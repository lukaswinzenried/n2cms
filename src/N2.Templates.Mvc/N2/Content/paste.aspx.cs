using System;

namespace N2.Edit
{
    [NavigationLinkPlugin("Paste", "paste", "../paste.aspx?selected={selected}&memory={memory}&action={action}", Targets.Preview, "~/N2/Resources/Img/ico/png/page_paste.png", 60, GlobalResourceClassName = "Navigation")]
    [ToolbarPlugin("PASTE", "paste", "paste.aspx?selected={selected}&memory={memory}&action={action}", ToolbarArea.Operations, Targets.Preview, "~/N2/Resources/Img/Ico/png/page_paste.png", 50, ToolTip = "paste", GlobalResourceClassName = "Toolbar")] 
	public partial class paste : Web.EditPage
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			string selected = Request.QueryString["selected"];
			string memory = Request.QueryString["memory"];
			string action = Request.QueryString["action"];
			if (string.IsNullOrEmpty(memory) || string.IsNullOrEmpty(action))
			{
				Title = "You must select what to paste and click on the appropriate action first.";
			}
			else
			{
				string url = string.Format("~/N2/Content/{0}.aspx?selected={1}&memory={2}", action, Server.UrlEncode(selected), Server.UrlEncode(memory));
				Response.Redirect(url);
			}
		}
	}
}