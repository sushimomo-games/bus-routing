using Godot;
using System;
using System.Linq;
public partial class InformationPanel : Control
{
	private Label _titleLabel;
	private Label _infoText;

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
		_titleLabel.Text = line.ColorName +" " +"busline"; //changes the color of the busline name
		_titleLabel.AddThemeColorOverride("font_color", line.Color);
		int stopCount = line.Path.OfType<BusStop>().Count();
		_infoText.Text =
		$"Number of stops: {stopCount}\n\n" +
		$"Time to complete: {line.TimeToComplete:F2} minutes";
	}

	public void ShowHouse(House house, String displayInfo)
	{
		this.Visible = true;
		_titleLabel.Text = house.BuildingColor + " House";
		_infoText.Text = displayInfo;
	}

	public void ShowBusStop(BusStop stop, int count)
	{
		this.Visible = true;
		_titleLabel.Text = "Bus Stop";
		_infoText.Text = count + " residents have access to this stop";
	}
	
}
