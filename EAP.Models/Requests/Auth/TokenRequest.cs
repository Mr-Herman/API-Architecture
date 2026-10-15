using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Auth;

public sealed class TokenRequest
{
    [Required, StringLength(128), RegularExpression("^[A-Za-z0-9_-]+$")]
    [DefaultValue("NCL02-0667")]
    public string ClientId { get; set; } = string.Empty;

    [Required, StringLength(512)]
    public string ClientSecret { get; set; } = string.Empty;
}
