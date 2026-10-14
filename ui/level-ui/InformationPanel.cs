using Godot;

public partial class InformationPanel : Control
{
    private Label _titleLabel;
    private Label _infoText;

    public override void _Ready()
    {
        LevelState.InformationPanel = this;
        _titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
        _infoText = GetNode<Label>("VBoxContainer/InfoText");
    }

    public void ShowBusLine(BusLine line)
    {
        _titleLabel.Text = line.ColorName;

        _infoText.Text =
            $"Time to complete: {line.TimeToComplete:F2} minutes";
    }
}