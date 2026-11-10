using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BusStop : RoadNode
{
    [Export] public Godot.Collections.Array<RoadEdge> ConnectedEdges = [];
    public Area2D BusStopArea => GetNode<Area2D>("BusStopArea");
    public Area2D WalkRadius => GetNode<Area2D>("WalkRadius");
    private Sprite2D _walkRadiusSprite => WalkRadius.GetNode<Sprite2D>("WalkRadiusSprite");

    private readonly HashSet<BusStopUpgrade> _purchasedUpgrades = [];

    /// <summary>
    /// The upgrades that have been purchased for this bus stop.
    /// </summary>
    public IReadOnlyCollection<BusStopUpgrade> PurchasedUpgrades => _purchasedUpgrades;

    /// <summary>
    /// Whether this stop is on at least one finished bus line.
    /// </summary>
    public bool IsInUse => LevelState.AllBusLines.Any(line => line.ContainsNode(this));

    /// <summary>
    /// The sum of the comfort bonuses of the purchased upgrades.
    /// </summary>
    public float ComfortBonus => _purchasedUpgrades.Sum(BusStopUpgradeInfo.GetComfortBonus);

    /// <summary>
    /// Fired whenever upgrades are added to or removed from this stop.
    /// </summary>
    public event Action UpgradesChanged;

    public override void _Ready()
    {
        base._Ready();
        LevelState.AllBusStops.Add(this);
    }

    /// <summary>
    /// Returns all bus stops that are within walking distance of this bus stop.
    /// Uses the WalkRadius Area2D to detect nearby BusStopArea collisions.
    /// </summary>
    public List<BusStop> GetNearbyBusStops()
    {
        return WalkRadius.GetOverlappingAreas()
            .Select(area => area.GetParent())
            .OfType<BusStop>()
            .Where(stop => stop != this)
            .ToList();
    }

    /// <summary>
    /// Returns whether the given upgrade has been purchased for this stop.
    /// </summary>
    public bool HasUpgrade(BusStopUpgrade upgrade) => _purchasedUpgrades.Contains(upgrade);

    /// <summary>
    /// Tries to buy an upgrade for this stop, paying from the budget and
    /// logging an edit so the purchase can be undone.
    /// </summary>
    /// <remarks>
    /// Does not show error messages; the caller turns the result into feedback.
    /// </remarks>
    /// <returns>Whether the upgrade was purchased, and if not, why.</returns>
    public UpgradePurchaseResult TryPurchaseUpgrade(BusStopUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return UpgradePurchaseResult.AlreadyPurchased;
        if (!IsInUse) return UpgradePurchaseResult.StopNotInUse;

        var cost = BusStopUpgradeInfo.GetCost(upgrade);
        if (LevelState.Budget < cost) return UpgradePurchaseResult.InsufficientBudget;

        var budgetBefore = LevelState.Budget;
        LevelState.Budget -= cost;
        _purchasedUpgrades.Add(upgrade);
        EditorState.History.LogEdit(new BusStopUpgradeEdit(this, upgrade, budgetBefore));
        LevelState.UpdateAllHouseStatuses();
        UpgradesChanged?.Invoke();
        return UpgradePurchaseResult.Purchased;
    }

    /// <summary>
    /// Replaces this stop's upgrades without charging or logging an edit.
    /// Used by undo to restore a deleted stop.
    /// </summary>
    internal void SetUpgrades(IEnumerable<BusStopUpgrade> upgrades)
    {
        _purchasedUpgrades.Clear();
        _purchasedUpgrades.UnionWith(upgrades);
        UpgradesChanged?.Invoke();
    }

    /// <summary>
    /// Removes an upgrade without refunding or logging an edit.
    /// Used by undo.
    /// </summary>
    internal void RemoveUpgrade(BusStopUpgrade upgrade)
    {
        if (_purchasedUpgrades.Remove(upgrade)) UpgradesChanged?.Invoke();
    }

    // Signals
    private void _on_bus_stop_area_mouse_entered()
    {
        _walkRadiusSprite.Visible = true;

    }

    private void _on_bus_stop_area_mouse_exited()
    {
        _walkRadiusSprite.Visible = false;
    }


    /// <summary>
    /// Counts how many houses are able to access bus stop.
    /// Uses ReachableBusStop to check which houses are in radius of current Bus Stop.
    /// Output is currently to terminal - will be hooked up with Information Panel.
    /// </summary>
    private void _on_bus_stop_clicked(Node viewport,
        InputEvent @event,
        long shapeIdx)
    {

        int _houseCount = 0;

        foreach (House house in LevelState.AllHouses)
        {
            if (house.ReachableBusStop == this)
            {
                _houseCount++;
            }
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            House.DeselectHouses();
            LevelState.InformationPanel.ShowBusStop(this, _houseCount);
        }

    }
    public int GetHouseCount() /// allows outside classes to use this method and logic of counting the houses. Done by Malachi
    {
        int houseCount = 0;

        foreach (House house in LevelState.AllHouses)
        {
            if (house.ReachableBusStop == this)
            {
                houseCount++;
            }
        }

        return houseCount;
    }

    public override void _ExitTree()
    {
        LevelState.AllBusStops.Remove(this);
        LevelState.AllRoadNodes.Remove(this);
        LevelState.Budget -= Cost.BusStopRemoval;
    }


}
