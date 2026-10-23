using Godot;
using System;
using System.Linq;
public partial class InformationPanel : Control
{
	private Label _titleLabel;
	private Label _infoText;
    private Label TitleLabel => _titleLabel ??= GetNode<Label>("VBoxContainer/TitleLabel");
    private Label InfoText => _infoText ??= GetNode<Label>("VBoxContainer/InfoText");

	public override void _Ready()
	{
		LevelState.InformationPanel = this;
		_titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
		_infoText = GetNode<Label>("VBoxContainer/InfoText");
		this.Visible=false;
	}

	public void ShowBusLine(BusLine line)
	{ 
		this.Visible= true; //makes the bus line information visible when the user clicks the line
		TitleLabel.Text = line.ColorName +" " +"busline"; //changes the color of the busline name
		TitleLabel.AddThemeColorOverride("font_color", line.Color);
		int stopCount = line.Path.OfType<BusStop>().Count();
	
		int totalResidents = 0;
		foreach (BusStop stop in line.Path.OfType<BusStop>())
		{
			totalResidents += stop.GetHouseCount();
			}
		InfoText.Text =
		$"Number of stops: {stopCount}\n\n" +
		$"Residents served: {totalResidents}\n\n" +
		$"Time to complete: {line.TimeToComplete:F2} minutes";
	}

	public void ShowHouse(House house, String displayInfo)
	{
		this.Visible = true;
		TitleLabel.Text = house.BuildingColor + " House";
		InfoText.Text = displayInfo;
	}

	public void ShowBusStop(BusStop stop, int count)
	{
		this.Visible = true;
		TitleLabel.Text = "Bus Stop";
		InfoText.Text = count + " residents have access to this stop";
	}
	
}
