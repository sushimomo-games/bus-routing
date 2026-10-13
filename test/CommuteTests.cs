using GdUnit4;
using Godot;
using System.Collections.Generic;
using static GdUnit4.Assertions;
using static GlobalConstants;

[TestSuite]
public class CommuteTests
{
    private const float Tolerance = 0.0001f;

    // SCENARIO: A resident walks to a bus stop, rides one bus line, then walks
    // from the last stop to their destination.
    [TestCase]
    public void GivenSingleRideCommute_WhenMilesWalkedCalculated_ThenBothWalksAreCounted()
    {
        // GIVEN: A house 30 px from the boarding stop, a 200 px bus ride, and
        // a destination 70 px from the exit stop.
        var house = CreateNode(new Vector2(0, 0));
        var boardingStop = CreateBusStop(new Vector2(30, 0));
        var exitStop = CreateBusStop(new Vector2(230, 0));
        var destination = CreateNode(new Vector2(230, 70));
        var line = CreateBusLine([boardingStop, exitStop]);

        var commute = new Commute(
        [
            new WalkSegment(house, boardingStop),
            new RideSegment(line, boardingStop, exitStop),
            new WalkSegment(exitStop, destination)
        ]);

        // WHEN: The total walking distance of the commute is calculated.
        float milesWalked = commute.MilesWalked;

        // THEN: Both walks are added together and converted to miles.
        float expectedMiles = (30f + 70f) / PixelsPerMile;
        AssertFloat(milesWalked).IsEqualApprox(expectedMiles, Tolerance);
    }

    // SCENARIO: A resident has to walk between two bus stops to transfer from
    // one bus line to another.
    [TestCase]
    public void GivenWalkingTransfer_WhenMilesWalkedCalculated_ThenTransferWalkIsCounted()
    {
        // GIVEN: A commute with a 40 px walk to the first stop, a 60 px walk
        // to transfer between lines, and a 100 px walk to the destination.
        var house = CreateNode(new Vector2(0, 0));
        var firstBoardingStop = CreateBusStop(new Vector2(40, 0));
        var firstExitStop = CreateBusStop(new Vector2(240, 0));
        var secondBoardingStop = CreateBusStop(new Vector2(240, 60));
        var secondExitStop = CreateBusStop(new Vector2(240, 360));
        var destination = CreateNode(new Vector2(340, 360));
        var firstLine = CreateBusLine([firstBoardingStop, firstExitStop]);
        var secondLine = CreateBusLine([secondBoardingStop, secondExitStop]);

        var commute = new Commute(
        [
            new WalkSegment(house, firstBoardingStop),
            new RideSegment(firstLine, firstBoardingStop, firstExitStop),
            new WalkSegment(firstExitStop, secondBoardingStop),
            new RideSegment(secondLine, secondBoardingStop, secondExitStop),
            new WalkSegment(secondExitStop, destination)
        ]);

        // WHEN: The total walking distance of the commute is calculated.
        float milesWalked = commute.MilesWalked;

        // THEN: The transfer walk is counted along with the first and last walks.
        float expectedMiles = (40f + 60f + 100f) / PixelsPerMile;
        AssertFloat(milesWalked).IsEqualApprox(expectedMiles, Tolerance);
    }

    // SCENARIO: Two residents walk the same distance, but one rides the bus
    // much farther than the other.
    [TestCase]
    public void GivenLongerBusRide_WhenWalkingIsUnchanged_ThenMilesWalkedIsUnchanged()
    {
        // GIVEN: Two commutes with identical 50 px walks on each end, but bus
        // rides of 100 px and 1000 px.
        var house = CreateNode(new Vector2(0, 0));
        var boardingStop = CreateBusStop(new Vector2(50, 0));

        var nearExitStop = CreateBusStop(new Vector2(150, 0));
        var nearDestination = CreateNode(new Vector2(150, 50));
        var shortLine = CreateBusLine([boardingStop, nearExitStop]);

        var farExitStop = CreateBusStop(new Vector2(1050, 0));
        var farDestination = CreateNode(new Vector2(1050, 50));
        var longLine = CreateBusLine([boardingStop, farExitStop]);

        var shortRideCommute = new Commute(
        [
            new WalkSegment(house, boardingStop),
            new RideSegment(shortLine, boardingStop, nearExitStop),
            new WalkSegment(nearExitStop, nearDestination)
        ]);
        var longRideCommute = new Commute(
        [
            new WalkSegment(house, boardingStop),
            new RideSegment(longLine, boardingStop, farExitStop),
            new WalkSegment(farExitStop, farDestination)
        ]);

        // WHEN: The total walking distance of each commute is calculated.
        float shortRideMilesWalked = shortRideCommute.MilesWalked;
        float longRideMilesWalked = longRideCommute.MilesWalked;

        // THEN: Bus rides add nothing to the walking distance, so both
        // commutes report the same miles walked.
        float expectedMiles = (50f + 50f) / PixelsPerMile;
        AssertFloat(shortRideMilesWalked).IsEqualApprox(expectedMiles, Tolerance);
        AssertFloat(longRideMilesWalked).IsEqualApprox(expectedMiles, Tolerance);
    }

    // SCENARIO: Two residents take the same bus ride, but one lives farther
    // from the boarding stop than the other.
    [TestCase]
    public void GivenLongerWalkToStop_WhenMilesWalkedCalculated_ThenMilesWalkedIsHigher()
    {
        // GIVEN: Two houses that are 20 px and 100 px from the same stop.
        var nearHouse = CreateNode(new Vector2(30, 0));
        var farHouse = CreateNode(new Vector2(-50, 0));
        var boardingStop = CreateBusStop(new Vector2(50, 0));
        var exitStop = CreateBusStop(new Vector2(250, 0));
        var destination = CreateNode(new Vector2(250, 10));
        var line = CreateBusLine([boardingStop, exitStop]);

        var nearCommute = new Commute(
        [
            new WalkSegment(nearHouse, boardingStop),
            new RideSegment(line, boardingStop, exitStop),
            new WalkSegment(exitStop, destination)
        ]);
        var farCommute = new Commute(
        [
            new WalkSegment(farHouse, boardingStop),
            new RideSegment(line, boardingStop, exitStop),
            new WalkSegment(exitStop, destination)
        ]);

        // WHEN: The total walking distance of each commute is calculated.
        float nearMilesWalked = nearCommute.MilesWalked;
        float farMilesWalked = farCommute.MilesWalked;

        // THEN: The resident who lives farther away walks more, by exactly
        // the extra distance to the stop.
        AssertFloat(farMilesWalked).IsGreater(nearMilesWalked);
        AssertFloat(farMilesWalked - nearMilesWalked).IsEqualApprox(80f / PixelsPerMile, Tolerance);
    }

    /// <summary>
    /// Creates a plain node at the given position to stand in for a house or
    /// destination building.
    /// </summary>
    private static Node2D CreateNode(Vector2 position)
    {
        var node = AutoFree(new Node2D());
        node.GlobalPosition = position;
        return node;
    }

    /// <summary>
    /// Creates a bus stop at the given position.
    /// </summary>
    private static BusStop CreateBusStop(Vector2 position)
    {
        var busStop = AutoFree(GD.Load<PackedScene>(Path.BusStopScene).Instantiate<BusStop>());
        busStop.GlobalPosition = position;
        return busStop;
    }

    /// <summary>
    /// Creates a bus line that travels through the given stops in order.
    /// </summary>
    private static BusLine CreateBusLine(List<RoadNode> path)
    {
        return AutoFree(new BusLine { Path = path });
    }
}
