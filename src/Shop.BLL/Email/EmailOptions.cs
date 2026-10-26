namespace Shop.BLL.Email;

/// <summary>
/// Configuration for the email service. Bound from the <c>Email</c> config
/// section plus the host content-root path. When <see cref="Enabled"/> is false
/// (the default for the demo) messages are logged and written to disk instead
/// of being sent over SMTP.
/// </summary>
public class EmailOptions
{
    /// <summary>Language of the request composing the message; durable retries retain its original text.</summary>
    public string Language { get; set; } = "en";
    /// <summary>Store-owned Arabic translations for product and status names.</summary>
    public Dictionary<string,string> Translations { get; set; } = new();
    /// <summary>When true and <see cref="SmtpHost"/> is set, send via SMTP.</summary>
    public bool Enabled { get; set; }
    /// <summary>Allow local message files in development only.</summary>
    public bool AllowDevelopmentSink { get; set; }
    /// <summary>Merchant notification destination, independent of the SMTP account.</summary>
    public string MerchantEmail { get; set; } = "";
    /// <summary>Send customer order confirmations.</summary>
    public bool CustomerOrdersEnabled { get; set; } = true;
    /// <summary>Send customer fulfillment updates.</summary>
    public bool CustomerStatusEnabled { get; set; } = true;
    /// <summary>Notify the merchant of new orders.</summary>
    public bool MerchantOrdersEnabled { get; set; } = true;

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
