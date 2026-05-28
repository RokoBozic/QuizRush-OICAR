using Microsoft.Maui.Devices;
using QuizRush.Mobile.Configuration;

namespace QuizRush.Mobile.Services;

public class ApiEndpointProvider
{
    public string GetBaseUrl()
    {
        return DeviceInfo.Current.Platform == DevicePlatform.Android
            ? ApiSettings.AndroidEmulatorBaseUrl
            : ApiSettings.DesktopBaseUrl;
    }

    public string GetHubUrl()
    {
        return $"{GetBaseUrl().TrimEnd('/')}/hub/game";
    }
}
