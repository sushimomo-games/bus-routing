using GdUnit4;
using Godot;
using System.Linq;
using static GdUnit4.Assertions;

[TestSuite]
public class BusStopUpgradeTests
{
    private const float Tolerance = 0.0001f;

    private BusLine _line;
    private Node _previousLevel;
    private uint _previousBudget;

    [Before]
    public void SaveLevelState()
    {
        _previousBudget = LevelState.Budget;
        _previousLevel = LevelState.CurrentLevel;
    }

    [After]
    public void RestoreLevelState()
    {
        LevelState.Budget = _previousBudget;
        LevelState.CurrentLevel = _previousLevel;
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

    // SCENARIO: The player buys a bench for a stop that is on a bus line.
    [TestCase]
    public void GivenStopOnLine_WhenPurchaseBench_ThenPurchasedAndBudgetDrops()
    {
        // GIVEN: A stop on a finished line and enough budget.
        var stop = CreateStopOnLine();
        LevelState.Budget = 50;

        // WHEN: The player buys a bench.
        var result = stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // THEN: The bench is purchased and its cost is deducted.
        AssertThat(result).IsEqual(UpgradePurchaseResult.Purchased);
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsTrue();
        AssertThat(LevelState.Budget).IsEqual(50u - Cost.BenchUpgrade);
    }

    // SCENARIO: The player tries to upgrade a stop that is on no bus line.
    [TestCase]
    public void GivenStopOnNoLine_WhenPurchaseBench_ThenStopNotInUse()
    {
        // GIVEN: A stop on no line and enough budget.
        var stop = AutoFree(GD.Load<PackedScene>(Path.BusStopScene).Instantiate<BusStop>());
        LevelState.Budget = 50;

        // WHEN: The player buys a bench.
        var result = stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // THEN: The purchase is refused and nothing changes.
        AssertThat(result).IsEqual(UpgradePurchaseResult.StopNotInUse);
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsFalse();
        AssertThat(LevelState.Budget).IsEqual(50u);
    }

    // SCENARIO: The player cannot afford an upgrade.
    [TestCase]
    public void GivenBudgetBelowCost_WhenPurchaseBench_ThenInsufficientBudget()
    {
        // GIVEN: A stop on a line and a budget one below the bench's cost.
        var stop = CreateStopOnLine();
        LevelState.Budget = Cost.BenchUpgrade - 1;

        // WHEN: The player buys a bench.
        var result = stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // THEN: The purchase is refused and nothing changes.
        AssertThat(result).IsEqual(UpgradePurchaseResult.InsufficientBudget);
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsFalse();
        AssertThat(LevelState.Budget).IsEqual(Cost.BenchUpgrade - 1);
    }

    // SCENARIO: The player buys the same upgrade twice.
    [TestCase]
    public void GivenBenchPurchased_WhenPurchaseBenchAgain_ThenAlreadyPurchasedAndChargedOnce()
    {
        // GIVEN: A stop that already has a bench.
        var stop = CreateStopOnLine();
        LevelState.Budget = 50;
        stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // WHEN: The player buys a bench again.
        var result = stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // THEN: The second purchase is refused and the cost was charged once.
        AssertThat(result).IsEqual(UpgradePurchaseResult.AlreadyPurchased);
        AssertThat(LevelState.Budget).IsEqual(50u - Cost.BenchUpgrade);
    }

    // SCENARIO: The comfort bonus of a stop depends on its upgrades.
    [TestCase]
    public void GivenStopUpgrades_WhenReadComfortBonus_ThenSumOfPurchasedBonuses()
    {
        // GIVEN: A stop on a line with no upgrades.
        var stop = CreateStopOnLine();
        LevelState.Budget = 50;

        // WHEN / THEN: With no upgrades the bonus is 0.
        AssertFloat(stop.ComfortBonus).IsEqual(0f);

        // WHEN: The player buys both upgrades.
        stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);
        stop.TryPurchaseUpgrade(BusStopUpgrade.Roof);

        // THEN: The bonus is the sum of both bonuses.
        AssertFloat(stop.ComfortBonus).IsEqualApprox(
            GlobalConstants.BenchComfortBonus + GlobalConstants.RoofComfortBonus, Tolerance);
    }

    // SCENARIO: The player undoes an upgrade purchase.
    [TestCase]
    public void GivenPurchasedBench_WhenUndo_ThenUpgradeRemovedAndBudgetRestored()
    {
        // GIVEN: A stop with a purchased bench.
        var stop = CreateStopOnLine();
        LevelState.Budget = 50;
        stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // WHEN: The player undoes the purchase.
        EditorState.History.Undo();

        // THEN: The bench is gone and the budget is back.
        AssertBool(stop.HasUpgrade(BusStopUpgrade.Bench)).IsFalse();
        AssertThat(LevelState.Budget).IsEqual(50u);
    }

    // SCENARIO: The player deletes a stop that has a roof, then undoes it.
    [TestCase]
    public void GivenDeletedStopWithRoof_WhenUndoRemoval_ThenNewStopHasRoof()
    {
        // GIVEN: A level with a stop, between two road nodes, that has a roof.
        var level = AutoFree(new Node2D());
        LevelState.CurrentLevel = level;
        var nodeA = AutoFree(new RoadNode());
        var nodeB = AutoFree(new RoadNode());
        var upgrades = new[] { BusStopUpgrade.Roof };

        // WHEN: The removal of that stop is undone.
        var edit = new BusStopEdit(false, 50, new Vector2(10, 0), nodeA, nodeB, null, upgrades);
        edit.Revert();

        // THEN: The restored stop has the roof.
        var restored = level.GetChildren().OfType<BusStop>().Single();
        AssertBool(restored.HasUpgrade(BusStopUpgrade.Roof)).IsTrue();
    }

    // SCENARIO: Other parts of the game listen for upgrade changes.
    [TestCase]
    public void GivenSubscribedStop_WhenPurchaseAndUndo_ThenUpgradesChangedFiresEachTime()
    {
        // GIVEN: A stop on a line with a listener on UpgradesChanged.
        var stop = CreateStopOnLine();
        LevelState.Budget = 50;
        var fired = 0;
        stop.UpgradesChanged += () => fired++;

        // WHEN: The player buys a bench.
        stop.TryPurchaseUpgrade(BusStopUpgrade.Bench);

        // THEN: The event fired once.
        AssertInt(fired).IsEqual(1);

        // WHEN: The player undoes the purchase.
        EditorState.History.Undo();

        // THEN: The event fired again.
        AssertInt(fired).IsEqual(2);
    }

    /// <summary>
    /// Creates a stop on a finished two-stop bus line. The line is added to
    /// LevelState and removed after the test.
    /// </summary>
    private BusStop CreateStopOnLine()
    {
        var busStopScene = GD.Load<PackedScene>(Path.BusStopScene);
        var stop = AutoFree(busStopScene.Instantiate<BusStop>());
        stop.GlobalPosition = new Vector2(0, 0);
        var otherStop = AutoFree(busStopScene.Instantiate<BusStop>());
        otherStop.GlobalPosition = new Vector2(100, 0);

        _line = AutoFree(new BusLine { Path = [stop, otherStop] });
        LevelState.AllBusLines.Add(_line);
        return stop;
    }
}
