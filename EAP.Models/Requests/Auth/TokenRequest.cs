using System.ComponentModel.DataAnnotations;

namespace EAP.Models.Requests.Auth;

public sealed class TokenRequest
{
    [Required]
    [MaxLength(128)]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string ClientSecret { get; set; } = string.Empty;
}
