using Godot;

/// <summary>
/// A tile in the information panel for one bus stop upgrade. It shows the
/// upgrade's price and name, and a checkbox that is checked once the upgrade
/// has been purchased.
/// </summary>
public partial class UpgradeTile : PanelContainer
{
    private const string NotInUseTooltip = "Add this stop to a bus line to upgrade it.";

    /// <summary>
    /// The upgrade this tile buys.
    /// </summary>
    [Export] public BusStopUpgrade Upgrade { get; set; }

    private BusStop _stop;
    private Label PriceLabel => GetNode<Label>("VBoxContainer/PriceLabel");
    private Label NameLabel => GetNode<Label>("VBoxContainer/NameLabel");
    private CheckBox CheckBox => GetNode<CheckBox>("VBoxContainer/CheckBox");

    /// <summary>
    /// Shows the state of this tile's upgrade for the given stop.
    /// </summary>
    public void ShowStop(BusStop stop)
    {
        _stop = stop;
        Refresh();
    }

    /// <summary>
    /// Updates the labels, checkbox, and tooltip from the current stop.
    /// The checkbox is disabled once the upgrade is purchased, or while the
    /// stop is not on a bus line.
    /// </summary>
    public void Refresh()
    {
        PriceLabel.Text = "$" + BusStopUpgradeInfo.GetCost(Upgrade);
        NameLabel.Text = BusStopUpgradeInfo.GetDisplayName(Upgrade);
        if (_stop == null) return;

        var purchased = _stop.HasUpgrade(Upgrade);
        var inUse = _stop.IsInUse;
        CheckBox.SetPressedNoSignal(purchased);
        CheckBox.Disabled = purchased || !inUse;
        CheckBox.TooltipText = inUse ? "" : NotInUseTooltip;
    }

    private void _on_check_box_toggled(bool isToggledOn)
    {
        // The box is disabled once purchased, so only checking it buys.
        if (!isToggledOn || _stop == null) return;

        var result = _stop.TryPurchaseUpgrade(Upgrade);
        var name = BusStopUpgradeInfo.GetDisplayName(Upgrade).ToLower();
        switch (result)
        {
            case UpgradePurchaseResult.InsufficientBudget:
                ShowError($"Insufficient budget to purchase a {name}.");
                break;
            case UpgradePurchaseResult.StopNotInUse:
                ShowError("Bus stop must be part of a bus line to be upgraded.");
                break;
        }

        // Unchecks the box again if nothing was bought.
        Refresh();
    }

    private void ShowError(string message)
    {
        GetTree()?.CurrentScene?
            .GetNodeOrNull<ErrorMessage>(Path.ErrorMessageNode)?
            .DisplayMessage(message);
    }
}
