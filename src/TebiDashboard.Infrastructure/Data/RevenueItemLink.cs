namespace TebiDashboard.Infrastructure.Data;

/// <summary>
/// Demo revenue is stored by product group, not by inventory item.
/// These groups each stand in for one item so a customer-detail row can show that item's name.
/// </summary>
internal static class RevenueItemLink
{
    /// <summary>
    /// Returns the inventory item id for a known demo product group, or null when the group is unknown.
    /// Unknown groups are not guessed, so the caller can fall back to the product group id.
    /// </summary>
    public static string? TryGetItemId(string? productGroupId) => productGroupId switch
    {
        "pg-paving" or "pg1" => "i1",
        "pg-tiles" or "pg2" => "i3",
        "pg-edging" => "i5",
        _ => null
    };

    /// <summary>
    /// Resolves the name shown on a revenue row.
    /// Prefer the inventory item name. Never copy a product-group / subsidiary id into the
    /// item label — that is how both columns ended up showing a raw number such as "16".
    /// </summary>
    public static string ResolveItemName(
        string? productGroupId,
        IReadOnlyDictionary<string, string> itemNames,
        out string itemId)
    {
        var linkedItemId = TryGetItemId(productGroupId);
        if (linkedItemId is null)
        {
            itemId = string.Empty;
            return string.Empty;
        }

        itemId = linkedItemId;
        if (itemNames.TryGetValue(linkedItemId, out var itemName) && !string.IsNullOrWhiteSpace(itemName))
        {
            return itemName;
        }

        // The product group points at an item, but that item row is missing.
        return linkedItemId;
    }
}
