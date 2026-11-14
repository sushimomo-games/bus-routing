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
    public void GivenStopOnLine_WhenShowBusStop_ThenBothTilesUncheckedAndEnabled()
    {
        // GIVEN: A stop on a line and the information panel.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();

        // WHEN: The panel shows the stop.
        panel.ShowBusStop(stop, 0);

        // THEN: The upgrades section is visible with both tiles unchecked and enabled.
        AssertBool(GetUpgradesContainer(panel).Visible).IsTrue();
        foreach (var upgrade in new[] { BusStopUpgrade.Bench, BusStopUpgrade.Roof })
        {
            AssertBool(GetCheckBox(panel, upgrade).ButtonPressed).IsFalse();
            AssertBool(GetCheckBox(panel, upgrade).Disabled).IsFalse();
        }
    }

    // SCENARIO: The player selects a stop that is on no bus line.
    [TestCase]
    public void GivenStopOnNoLine_WhenShowBusStop_ThenBothTilesDisabled()
    {
        // GIVEN: A stop on no line.
        var stop = CreateStop(onLine: false);
        var panel = CreatePanel();

        // WHEN: The panel shows the stop.
        panel.ShowBusStop(stop, 0);

        // THEN: Both tiles are disabled.
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).Disabled).IsTrue();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Roof).Disabled).IsTrue();
    }

    // SCENARIO: The player checks the bench box with enough budget.
    [TestCase]
    public void GivenEnoughBudget_WhenBenchToggled_ThenBenchBoughtAndTileDisabled()
    {
        // GIVEN: A stop on a line, shown in the panel, and enough budget.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        LevelState.Budget = 50;
        panel.ShowBusStop(stop, 0);

        // WHEN: The player checks the bench box.
        GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed = true;

        // THEN: The bench is bought, and the box stays checked and disabled.
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsTrue();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed).IsTrue();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).Disabled).IsTrue();
    }

    // SCENARIO: The player checks the bench box without enough budget.
    [TestCase]
    public void GivenTooLittleBudget_WhenBenchToggled_ThenBoxUncheckedAndNothingBought()
    {
        // GIVEN: A stop on a line, shown in the panel, and a budget below the cost.
        var stop = CreateStop(onLine: true);
        var panel = CreatePanel();
        LevelState.Budget = Cost.BenchUpgrade - 1;
        panel.ShowBusStop(stop, 0);

        // WHEN: The player checks the bench box.
        GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed = true;

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
        GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed = true;

        // WHEN: The player undoes the purchase.
        EditorState.History.Undo();

        // THEN: The bench tile is unchecked and enabled again.
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).ButtonPressed).IsFalse();
        AssertBool(GetCheckBox(panel, BusStopUpgrade.Bench).Disabled).IsFalse();
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

    private static CheckBox GetCheckBox(InformationPanel panel, BusStopUpgrade upgrade) =>
        panel.GetNode<CheckBox>(
            $"VBoxContainer/UpgradesContainer/{upgrade}Tile/VBoxContainer/CheckBox");
}
