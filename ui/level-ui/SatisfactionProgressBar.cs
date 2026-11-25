using Godot;
using System;

public partial class SatisfactionProgressBar : ProgressBar
{

    public override void _Ready()
    {
        MinValue = 0;
        MaxValue = 100;
        Value = 0;  // Initial city satisfaction starts at 0, assuming no bus lines have been placed
    }

    public override void _Process(double delta)
    {
        AdjustProgressBar();
    }

    /// <summary>
    /// Sets progress bar with new value according to average resident satisfaction
    /// </summary>
    public void SetProgressBar(float newVal)
    {
        Value = Mathf.Clamp(newVal, MinValue, MaxValue);
    }

    /// <summary>
    /// Uses average of resident satisfaction to adjust progress bar
    /// </summary>
    public void AdjustProgressBar()
    {
        SetProgressBar(CalculateAverage() * 100);
    }

    /// <summary>
    /// Calculates average satisfaction for all houses
    /// </summary>
    public float CalculateAverage()
    {
        float _totalSatisfaction = 0;
        int _houseCount = LevelState.AllHouses.Count;

        foreach (House house in LevelState.AllHouses)
        {
            house.UpdateCheckStatus();
            _totalSatisfaction += house._residentSatisfaction;
        }

        if (_houseCount == 0) return 0;

        return _totalSatisfaction / _houseCount;
    }


}
