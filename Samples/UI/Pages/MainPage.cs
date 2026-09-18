using Samples.UI.ViewModels;
using Ssit.CrossX2;
using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Views;

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