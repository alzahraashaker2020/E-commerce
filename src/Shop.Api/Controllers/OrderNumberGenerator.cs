using System.Security.Cryptography;
using System.Text;

namespace Shop.Api.Controllers;

/// <summary>
/// Generates human-readable order numbers in the form
/// <c>ORD-{yyyyMMdd}-{6 Crockford base32 chars}</c>. The database unique index
/// on <c>Order.OrderNumber</c> is the real guarantee; the random suffix keeps
/// collisions rare so the checkout retry loop almost never fires.
/// </summary>
public static class OrderNumberGenerator
{
    // Crockford base32 alphabet: digits 0-9 and A-Z excluding I, L, O, U.
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Builds a new order number stamped with <paramref name="date"/> and a
    /// random 6-character Crockford base32 suffix.
    /// </summary>
    /// <param name="date">The UTC date to stamp into the order number.</param>
    public static string Generate(DateTime date)
    {
        var suffix = new StringBuilder(6);
        for (int i = 0; i < 6; i++)
        {
            // RandomNumberGenerator gives an unbiased index into the 32-char set.
            int index = RandomNumberGenerator.GetInt32(CrockfordAlphabet.Length);
            suffix.Append(CrockfordAlphabet[index]);
        }

        return $"ORD-{date:yyyyMMdd}-{suffix}";
    }
}
