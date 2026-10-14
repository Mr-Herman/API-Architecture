using EAP.Application.Localization;
using EAP.Models.Resources;
using Microsoft.Extensions.Localization;

namespace EAP.Api.Localization;

public sealed class ResourceMessageLocalizer : IMessageLocalizer
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    public ResourceMessageLocalizer(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string this[string key]
    {
        get
        {
            return _localizer[key];
        }
    }
}
