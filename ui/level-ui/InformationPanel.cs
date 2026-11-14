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

    //References for automatic refresh purposes
    private BusLine _currentLine;
    private House _currentHouse;
    private BusStop _currentStop;
    private String _currentHouseDisplay;
    private int _currentCount;

    public enum InfoPanelType
    {
        None,
        BusLine,
        BusStop,
        House
    }
    private InfoPanelType _currentType;

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

        _currentType = InfoPanelType.None;
    }

    public override void _Process(double delta)
    {
        switch (_currentType)
        {
            case InfoPanelType.None:
                this.Visible = false;
                break;
            case InfoPanelType.BusLine:
                if (_currentLine != SelectedBusLine && SelectedBusLine != null && CurrentBusLineCreationStep == BusLineCreationStep.ContinuingEdit)
                {
                    ShowBusLine(SelectedBusLine);
                }
                break;
            case InfoPanelType.House:
                ShowHouse(_currentHouse);
                break;
            case InfoPanelType.BusStop:
                ShowBusStop(_currentStop);
                break;
        }
    }

    private void UnsubscribeBusLineEvents()
    {
        if (_currentLine != null)
        {
            _currentLine.OnPathChanged -= OnBusLinePathChanged;
            _currentLine.OnDeleted += OnBusLineDeleted;
        }
        RefreshBusLineDisplay();
    }

    private void OnBusLinePathChanged()
    {
        if (_currentType == InfoPanelType.BusLine)
        {
            RefreshBusLineDisplay();
        }
    }

    private void OnBusLineDeleted()
    {
        UnsubscribeBusLineEvents();
        _currentType = InfoPanelType.None;
        _currentLine = null;
        this.Visible = false;
    }

    public void ShowBusLine(BusLine line)
    {
        UnsubscribeBusLineEvents();

        _currentType = InfoPanelType.BusLine;
        _currentLine = line;

        if (_currentLine != null)
        {
            _currentLine.OnPathChanged += OnBusLinePathChanged;
            _currentLine.OnDeleted += OnBusLineDeleted;
        }

        RefreshBusLineDisplay();
    }

    private void RefreshBusLineDisplay()
    {
        if (_currentLine == null) return;

        this.Visible = true;
        _editLineBtn.Visible = true;
        _deleteLineBtn.Visible = true;

        TitleLabel.Text = _currentLine.ColorName + " " + "busline"; //changes the color of the busline name
        TitleLabel.AddThemeColorOverride("font_color", _currentLine.Color);
        int stopCount = _currentLine.Path.OfType<BusStop>().Count();

        int totalResidents = 0;
        foreach (BusStop stop in _currentLine.Path.OfType<BusStop>())
        {
            totalResidents += stop.GetHouseCount();
        }
        InfoText.Text =
        $"Number of stops: {stopCount}\n\n" +
        $"Residents served: {totalResidents}\n\n" +
        $"Time to complete: {_currentLine.TimeToComplete} minutes";
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

    public void ShowHouse(House house, String displayInfo = null)
    {
        _currentType = InfoPanelType.House;
        _currentHouse = house;
        _currentHouseDisplay = displayInfo;

        this.Visible = true;
        _hideButtons();

        TitleLabel.Text = house.BuildingColor + " House";
        string liveDisplayInfo = house.GetDisplayInfo();

        InfoText.Text = liveDisplayInfo;
    }

    public void ShowBusStop(BusStop stop, int count = 0)
    {
        _currentType = InfoPanelType.BusStop;
        _currentStop = stop;

        //check, if count turns to 0, then maybe wait a few seconds before cutting off display?
        this.Visible = true;
        _hideButtons();

        int liveCount = stop.GetHouseCount();

        TitleLabel.Text = "Bus Stop";
        InfoText.Text = liveCount + " residents have access to this stop";
    }

    private void _hideButtons()
    {
        _editLineBtn.Visible = false;
        _deleteLineBtn.Visible = false;
    }

}
