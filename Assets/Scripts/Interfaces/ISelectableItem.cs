/// <summary>
/// Interface for items that can be selected by the player.
/// This allows for a unified selection mechanism for both weapons and buildables.
/// </summary>
public interface ISelectableItem
{
    /// <summary>
    /// Selects the item. This method encapsulates the specific logic
    /// for what happens when an item is chosen by the player.
    /// </summary>
    void Select();
}
