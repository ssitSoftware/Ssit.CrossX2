using Samples.UI.ViewModels;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Views;

namespace Samples.UI.Pages;

public class MainPage: PageWithTranslator<MainPageViewModel>
{
    protected override View CreateView()
    {
        return new Container
        {
            BackgroundColor = RgbaColor.CornflowerBlue
        };
    }
}