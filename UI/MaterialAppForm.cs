using MaterialSkin.Controls;

namespace ScholarwaveCBTApp.UI
{
    /// <summary>
    /// Base form wired to MaterialSkin.2 (Material Design) theming.
    /// </summary>
    public class MaterialAppForm : MaterialForm
    {
        protected MaterialAppForm()
        {
            MaterialSkinService.RegisterForm(this);
        }
    }
}
