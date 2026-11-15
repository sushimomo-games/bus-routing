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
    private Button _deleteBtn, _editLineBtn;
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
        _deleteBtn = GetNode<Button>("VBoxContainer/DeleteBtn");
        _endBusLineButton = GetTree().CurrentScene.GetNode<Button>(Path.EndBusLineButtonNode);
        _lineEditorStatusLabel = GetTree().CurrentScene.GetNode<Label>(Path.LineEditorStatusLabelNode);

        _editLineBtn.Pressed += _onEditLineBtnPressed;
        _deleteBtn.Pressed += _onDeleteBtnPressed;

        _currentType = InfoPanelType.None;
    }

    /// <summary>
    /// Allows for information panel's data to update automatically; no longer
    /// requires user to click on specific elements to see changes in data.
    /// </summary>
    public override void _Process(double delta)
    {
        switch (_currentType)
        {
            case InfoPanelType.None:
                this.Visible = false;
                break;
            case InfoPanelType.BusLine:
                if (_currentLine != SelectedBusLine && SelectedBusLine != null)
                {
                    ShowBusLine(SelectedBusLine);
                }
                break;
            case InfoPanelType.House:
                if (_currentHouse != null)
                {
                    InfoText.Text = _currentHouse.GetDisplayInfo();
                }
                else
                {
                    _currentType = InfoPanelType.None;
                    this.Visible = false;
                }
                break;
            case InfoPanelType.BusStop:
                if (_currentStop != null && GodotObject.IsInstanceValid(_currentStop) && _currentStop.IsInsideTree())
                {
                    int liveCount = _currentStop.GetHouseCount();
                    InfoText.Text = liveCount + " residents have access to this stop";
                }
                else
                {
                    _currentType = InfoPanelType.None;
                    _currentStop = null;
                    this.Visible = false;
                }
                break;
        }
    }

    /// <summary>
    /// Unsubscribes from the current bus line's events and refreshes the display.
    /// </summary>
    private void UnsubscribeBusLineEvents()
    {
        if (_currentLine != null)
        {
            _currentLine.OnPathChanged -= OnBusLinePathChanged;
            _currentLine.OnDeleted -= OnBusLineDeleted;
        }
        RefreshBusLineDisplay();
    }

    /// <summary>
    /// If a bus line's path has been changed, will refresh display
    /// so that bus line data is appropriately updated.
    /// </summary>
    private void OnBusLinePathChanged()
    {
        if (_currentType == InfoPanelType.BusLine)
        {
            RefreshBusLineDisplay();
        }
    }

    /// <summary>
    /// If a bus line is deleted, no longer listens to its events, sets
    /// _currentLine to null, and causes information panel to disappear.
    /// </summary>
    private void OnBusLineDeleted()
    {
        UnsubscribeBusLineEvents();
        _currentType = InfoPanelType.None;
        _currentLine = null;
        this.Visible = false;
    }

    /// <summary>
    /// Specifies selected line, listens to its events (deletion, path changes),
    /// refreshes bus line display.
    /// </summary>
    public void ShowBusLine(BusLine line)
    {

        SelectedBusLine = line;
        _currentType = InfoPanelType.BusLine;
        _currentLine = line;

        if (_currentLine != null)
        {
            _currentLine.OnPathChanged += OnBusLinePathChanged;
            _currentLine.OnDeleted += OnBusLineDeleted;
        }

        RefreshBusLineDisplay();
    }

    /// <summary>
    /// Displays appropriate bus line information including name, stop count, residents,
    /// and time to complete.
    /// </summary>
    private void RefreshBusLineDisplay()
    {
        if (_currentLine == null) return;

        this.Visible = true;
        _editLineBtn.Visible = true;
        _deleteBtn.Visible = true;

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
    public void _onDeleteBtnPressed()
    {
        this.Visible = false;

        if (_currentType == InfoPanelType.BusLine && _currentLine != null)
        {
            _currentLine.Delete();
            _currentType = InfoPanelType.None;
        }else if (_currentType == InfoPanelType.BusStop && _currentStop != null)
        {
            //Bus Stop deletion will be handled here
            GD.Print("Bus stop - Delete button pressed");
            _currentType = InfoPanelType.None;
        }
    }

    public void ShowHouse(House house, String displayInfo = null)
    {
        UnsubscribeBusLineEvents();

        _currentType = InfoPanelType.House;
        _currentHouse = house;
        _currentHouseDisplay = displayInfo;

        _currentLine = null;

        this.Visible = true;
        _hideButtons();

        TitleLabel.Text = house.BuildingColor + " House";
        InfoText.Text = house.GetDisplayInfo();
    }

    public void ShowBusStop(BusStop stop, int count = 0)
    {
        UnsubscribeBusLineEvents();

        _currentType = InfoPanelType.BusStop;
        _currentStop = stop;

        this.Visible = true;

        //Delete button for bus stop currently non-functional; merely reads
        //input signal at this point
        _deleteBtn.Visible = true;
        _editLineBtn.Visible = false;

        int liveCount = stop.GetHouseCount();

        TitleLabel.Text = "Bus Stop";
        InfoText.Text = liveCount + " residents have access to this stop";
    }

    private void _hideButtons()
    {
        _editLineBtn.Visible = false;
        _deleteBtn.Visible = false;
    }

}
