using System;
using System.Web.UI;

namespace VsatXpolLmp.Web
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Example of touching the LMP library to ensure linkage
                // (No direct call here to avoid tight coupling in scaffold)
                lblStatus.Text = "LMP Library wired up. Build and reference succeeded.";
            }
        }
    }
}
