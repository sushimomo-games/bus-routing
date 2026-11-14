using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
public class BusStopUpgradePanelTests
{
    private BusLine _line;
    private uint _previousBudget;
    private InformationPanel _previousPanel;

    [Before]
    public void SaveLevelState()
    {
        _previousBudget = LevelState.Budget;
        _previousPanel = LevelState.InformationPanel;
    }

    [After]
    public void RestoreLevelState()
    {
        LevelState.Budget = _previousBudget;
        LevelState.InformationPanel = _previousPanel;
    }

    [BeforeTest]
    public void ResetHistory()
    {
        EditorState.History.Clear();
    }

    [AfterTest]
    public void UnregisterLevelState()
    {
        LevelState.AllBusLines.Remove(_line);
        _line = null;
        EditorState.History.Clear();
    }

    // SCENARIO: The player selects a stop that is on a bus line.
    [TestCase]
    public void GivenStopOnLine_WhenShowBusStop_ThenBothTilesUncheckedAndClickable()
    {
        // GIVEN: A stop on a line and the information panel.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();

        // WHEN: The panel shows the stop.
        panel.ShowBusStop(stop, 0);

        // THEN: The upgrades section is visible with both tiles unchecked and clickable.
        AssertBool(GetUpgradesContainer(panel).Visible).IsTrue();
        foreach (var upgrade in new[] { BusStopUpgrade.Bench, BusStopUpgrade.Roof })
        {
            AssertBool(GetCheckBox(panel, upgrade).ButtonPressed).IsFalse();
            AssertBool(GetTile(panel, upgrade).CanPurchase).IsTrue();
        }
    }

    // SCENARIO: The player selects a stop that is on no bus line.
    [TestCase]
    public void GivenStopOnNoLine_WhenShowBusStop_ThenBothTilesNotClickable()
    {
        // GIVEN: A stop on no line.
        var stop = CreateStop(onLine: false);
        var panel = CreatePanel();

        // WHEN: The panel shows the stop.
        panel.ShowBusStop(stop, 0);

        // THEN: Neither tile is clickable.
        AssertBool(GetTile(panel, BusStopUpgrade.Bench).CanPurchase).IsFalse();
        AssertBool(GetTile(panel, BusStopUpgrade.Roof).CanPurchase).IsFalse();
    }

    // SCENARIO: The player clicks the bench tile with enough budget.
    [TestCase]
    public void GivenEnoughBudget_WhenBenchTileClicked_ThenBenchBoughtAndBoxChecked()
    {
        // GIVEN: A stop on a line, shown in the panel, and enough budget.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        LevelState.Budget = 50;
        panel.ShowBusStop(stop, 0);

        // WHEN: The player clicks the bench tile.
        ClickTile(panel, BusStopUpgrade.Bench);

        // THEN: The bench is bought, and the box shows checked and the tile can no longer be bought.
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsTrue();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed).IsTrue();
        AssertBool(GetTile(panel, BusStopUpgrade.Bench).CanPurchase).IsFalse();
    }

    // SCENARIO: The player clicks the bench tile without enough budget.
    [TestCase]
    public void GivenTooLittleBudget_WhenBenchTileClicked_ThenBoxUncheckedAndNothingBought()
    {
        // GIVEN: A stop on a line, shown in the panel, and a budget below the cost.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        LevelState.Budget = Cost.BenchUpgrade - 1;
        panel.ShowBusStop(stop, 0);

        // WHEN: The player clicks the bench tile.
        ClickTile(panel, BusStopUpgrade.Bench);

        // THEN: The box is unchecked again and the stop has no bench.
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsFalse();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed).IsFalse();
    }

    // SCENARIO: The player selects a house after a stop.
    [TestCase]
    public void GivenStopShown_WhenHouseShown_ThenUpgradesSectionHidden()
    {
        // GIVEN: The panel showing a stop on a line.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        panel.ShowBusStop(stop, 0);

        // WHEN: The panel shows a house.
        panel.ShowHouse(AutoFree(new HouseWithReachableBusStop()), "info");

        // THEN: The upgrades section is hidden.
        AssertBool(GetUpgradesContainer(panel).Visible).IsFalse();
    }

    // SCENARIO: The player undoes an upgrade while its stop is shown.
    [TestCase]
    public void GivenBenchBoughtInPanel_WhenUndo_ThenBenchTileUnchecked()
    {
        // GIVEN: A bench bought through the panel.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        LevelState.Budget = 50;
        panel.ShowBusStop(stop, 0);
        ClickTile(panel, BusStopUpgrade.Bench);

        // WHEN: The player undoes the purchase.
        EditorState.History.Undo();

        // THEN: The bench tile is unchecked and clickable again.
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed).IsFalse();
        AssertBool(GetTile(panel, BusStopUpgrade.Bench).CanPurchase).IsTrue();
    }

    private BusStop CreateStop(bool onLine)
    {
        var busStopScene = GD.Load<PackedScene>(Path.BusStopScene);
        var stop = AutoFree(busStopScene.Instantiate<BusStop>());
        if (onLine)
        {
            var other = AutoFree(busStopScene.Instantiate<BusStop>());
            other.GlobalPosition = new Vector2(100, 0);
            _line = AutoFree(new BusLine { Path = [stop, other] });
            LevelState.AllBusLines.Add(_line);
        }
        return stop;
    }

    private InformationPanel CreatePanel()
    {
        var levelUI = AutoFree(GD.Load<PackedScene>("res://ui/level-ui/level-ui.tscn").Instantiate<Control>());
        var panel = levelUI.GetNode<InformationPanel>("InfoPanelContainer/informationPanel");
        LevelState.InformationPanel = panel;
        return panel;
    }

    private static Control GetUpgradesContainer(InformationPanel panel) =>
        panel.GetNode<Control>("VBoxContainer/UpgradesContainer");

    private static UpgradeTile GetTile(InformationPanel panel, BusStopUpgrade upgrade) =>
        panel.GetNode<UpgradeTile>($"VBoxContainer/UpgradesContainer/{upgrade}Tile");

    private static CheckBox GetCheckBox(InformationPanel panel, BusStopUpgrade upgrade) =>
        GetTile(panel, upgrade).GetNode<CheckBox>("VBoxContainer/CheckBox");

    /// <summary>
    /// Sends a left click to the tile of the given upgrade.
    /// </summary>
    private static void ClickTile(InformationPanel panel, BusStopUpgrade upgrade)
    {
        var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
        GetTile(panel, upgrade).EmitSignal(Control.SignalName.GuiInput, click);
    }
}
