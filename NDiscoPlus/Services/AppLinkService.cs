using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Services;
internal class AppLinkService
{
    public void OnAppLinkReceived(string? relativePath, IDictionary<string, string> queryParameters)
    {
        Application.Current?.Windows[0].Page!.DisplayAlert("App link received", relativePath + "\n\n" + string.Join('\n', queryParameters.Select(x => $"{x.Key}: {x.Value}")), "OK");
    }
}
