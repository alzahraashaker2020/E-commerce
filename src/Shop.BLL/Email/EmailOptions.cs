namespace Shop.BLL.Email;

/// <summary>
/// Configuration for the email service. Bound from the <c>Email</c> config
/// section plus the host content-root path. When <see cref="Enabled"/> is false
/// (the default for the demo) messages are logged and written to disk instead
/// of being sent over SMTP.
/// </summary>
public class EmailOptions
{
    /// <summary>When true and <see cref="SmtpHost"/> is set, send via SMTP.</summary>
    public bool Enabled { get; set; }

    /// <summary>SMTP server host name.</summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>SMTP server port.</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>SMTP user name.</summary>
    public string SmtpUser { get; set; } = string.Empty;

    /// <summary>SMTP password.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>From address used on outgoing messages.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Host content-root path. In demo mode, message copies are written under
    /// <c>&lt;ContentRootPath&gt;/sent-emails/&lt;orderNumber&gt;.txt</c>.
    /// </summary>
    public string ContentRootPath { get; set; } = string.Empty;
}
