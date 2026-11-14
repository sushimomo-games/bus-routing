using Godot;

/// <summary>
/// A tile in the information panel for one bus stop upgrade. It shows the
/// upgrade's price and name. Clicking anywhere on the tile buys the upgrade.
/// The checkbox only displays whether the upgrade has been purchased.
/// </summary>
public partial class UpgradeTile : PanelContainer
{
    private const string NotInUseTooltip = "Add this stop to a bus line to upgrade it.";
    private static readonly Color UnavailableColor = new(1, 1, 1, 0.5f);

    /// <summary>
    /// The upgrade this tile buys.
    /// </summary>
    [Export] public BusStopUpgrade Upgrade { get; set; }

    private BusStop _stop;
    private Label PriceLabel => GetNode<Label>("VBoxContainer/PriceLabel");
    private Label NameLabel => GetNode<Label>("VBoxContainer/NameLabel");
    private CheckBox CheckBox => GetNode<CheckBox>("VBoxContainer/CheckBox");

    /// <summary>
    /// Whether clicking the tile can buy its upgrade: the stop is on a bus
    /// line and does not have the upgrade yet.
    /// </summary>
    public bool CanPurchase => _stop != null && !_stop.HasUpgrade(Upgrade) && _stop.IsInUse;

    /// <summary>
    /// Shows the state of this tile's upgrade for the given stop.
    /// </summary>
    public void ShowStop(BusStop stop)
    {
        _stop = stop;
        Refresh();
    }

    /// <summary>
    /// Updates the labels, checkbox, and tooltip from the current stop. The
    /// tile is dimmed while its upgrade can't be bought.
    /// </summary>
    public void Refresh()
    {
        PriceLabel.Text = "$" + BusStopUpgradeInfo.GetCost(Upgrade);
        NameLabel.Text = BusStopUpgradeInfo.GetDisplayName(Upgrade);
        if (_stop == null) return;

        CheckBox.SetPressedNoSignal(_stop.HasUpgrade(Upgrade));
        Modulate = CanPurchase || _stop.HasUpgrade(Upgrade) ? Colors.White : UnavailableColor;
        TooltipText = _stop.IsInUse ? "" : NotInUseTooltip;
    }

    private void _on_gui_input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TryPurchase();
        }
    }

    /// <summary>
    /// Tries to buy this tile's upgrade, showing an error if it fails.
    /// Does nothing if the upgrade is already purchased.
    /// </summary>
    private void TryPurchase()
    {
        if (_stop == null) return;

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

        Refresh();
    }

    private void ShowError(string message)
    {
        GetTree()?.CurrentScene?
            .GetNodeOrNull<ErrorMessage>(Path.ErrorMessageNode)?
            .DisplayMessage(message);
    }
}
