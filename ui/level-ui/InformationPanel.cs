using Godot;
using System;
using System.Linq;
using static EditorState;
public partial class InformationPanel : Control
{
    private Label _titleLabel;
    private Label _infoText;
    private Label TitleLabel => _titleLabel ??= GetNode<Label>("VBoxContainer/TitleLabel");
    private Label InfoText => _infoText ??= GetNode<Label>("VBoxContainer/InfoText");
    private Button _deleteLineBtn, _editLineBtn;
    private Button _endBusLineButton;
    private Label _lineEditorStatusLabel;
    private BusLine _currentLine;
    private BusStop _currentStop;
    private Label _upgradesLabel;
    private Control _upgradesContainer;
    private Label UpgradesLabel => _upgradesLabel ??= GetNode<Label>("VBoxContainer/UpgradesLabel");
    private Control UpgradesContainer => _upgradesContainer ??= GetNode<Control>("VBoxContainer/UpgradesContainer");


    public override void _Ready()
    {
        LevelState.InformationPanel = this;
        this.Visible = false;

        _titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
        _infoText = GetNode<Label>("VBoxContainer/InfoText");
        _editLineBtn = GetNode<Button>("VBoxContainer/EditBtn");
        _deleteLineBtn = GetNode<Button>("VBoxContainer/DeleteBtn");
        _endBusLineButton = GetTree().CurrentScene.GetNode<Button>(Path.EndBusLineButtonNode);
        _lineEditorStatusLabel = GetTree().CurrentScene.GetNode<Label>(Path.LineEditorStatusLabelNode);

        _editLineBtn.Pressed += _onEditLineBtnPressed;
        _deleteLineBtn.Pressed += _onDeleteLineBtnPressed;

    }

    public override void _Process(double delta)
    {
        // Hide the panel if the shown stop was deleted.
        if (_currentStop != null && !IsInstanceValid(_currentStop))
        {
            _currentStop = null;
            this.Visible = false;
        }
    }

    public void ShowBusLine(BusLine line)
    {
        _currentLine = line;
        _hideUpgrades();

        this.Visible = true; //makes the bus line information visible when the user clicks the line
        _editLineBtn.Visible = true;
        _deleteLineBtn.Visible = true;

        TitleLabel.Text = line.ColorName + " " + "busline"; //changes the color of the busline name
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

    /// <summary>
    /// Allows user to edit bus line from the information panel. Indicates to user
    /// which bus line is currently being edited.
    /// </summary>
    public void _onEditLineBtnPressed()
    {
        SelectedBusLine = _currentLine;
        CurrentBusLineCreationStep = BusLineCreationStep.BeginningEdit;
        _lineEditorStatusLabel.Text = $"● Editing bus line: {_currentLine.ColorName}";

        _endBusLineButton.Visible = true;
        _lineEditorStatusLabel.Visible = true;
        this.Visible = false;
    }

    /// <summary>
    /// Removes bus line from list of existing bus lines using Delete() of
    /// BusLine class.
    /// </summary>
    public void _onDeleteLineBtnPressed()
    {
        this.Visible = false;
        _currentLine.Delete();
    }

    public void ShowHouse(House house, String displayInfo)
    {
        this.Visible = true;
        _hideButtons();
        _hideUpgrades();

        TitleLabel.Text = house.BuildingColor + " House";
        InfoText.Text = displayInfo;
    }

    public void ShowBusStop(BusStop stop, int count)
    {
        this.Visible = true;
        _hideButtons();

        TitleLabel.Text = "Bus Stop";
        InfoText.Text = count + " residents have access to this stop";

        SetCurrentStop(stop);
        UpgradesLabel.Visible = true;
        UpgradesContainer.Visible = true;
        RefreshUpgradeTiles();
    }

    /// <summary>
    /// Hides the upgrades section and stops listening to the current stop.
    /// </summary>
    private void _hideUpgrades()
    {
        SetCurrentStop(null);
        UpgradesLabel.Visible = false;
        UpgradesContainer.Visible = false;
    }

    /// <summary>
    /// Moves the UpgradesChanged subscription to the given stop, so the tiles
    /// refresh when an upgrade is bought or undone while the panel is open.
    /// </summary>
    private void SetCurrentStop(BusStop stop)
    {
        if (_currentStop != null && IsInstanceValid(_currentStop))
        {
            _currentStop.UpgradesChanged -= RefreshUpgradeTiles;
        }
        _currentStop = stop;
        if (_currentStop != null)
        {
            _currentStop.UpgradesChanged += RefreshUpgradeTiles;
        }
    }

    private void RefreshUpgradeTiles()
    {
        foreach (var tile in UpgradesContainer.GetChildren().OfType<UpgradeTile>())
        {
            tile.ShowStop(_currentStop);
        }
    }

    private void _hideButtons()
    {
        if (_editLineBtn != null) _editLineBtn.Visible = false;
        if (_deleteLineBtn != null) _deleteLineBtn.Visible = false;
    }

}
