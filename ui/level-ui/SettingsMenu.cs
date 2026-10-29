using Godot;
using System;


public partial class SettingsMenu : PanelContainer
{
    private Button _closeMenuButton, _exitGameButton;
    private Slider _audioAdjust;


    public override void _Ready()
    {
        Visible = false;


        _audioAdjust = GetNode<Slider>("SettingsVBox/VolumeContainer/VolumeSlider");
        _exitGameButton = GetNode<Button>("SettingsVBox/ExitGameButton");
        _closeMenuButton = GetNode<Button>("SettingsVBox/CloseMenuButton");
       
        _audioAdjust.ValueChanged += _onAudioLevelChanged;
        _exitGameButton.Pressed += _onExitGameButtonPressed;
        _closeMenuButton.Pressed += _onCloseMenuButtonPressed;
    }


    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if(keyEvent.Keycode == Key.Escape)
            {
                if (Visible) hideMenu();
                else showMenu();
               
            }
        }
    }


    private void _onAudioLevelChanged(double audioLevel)
    {
        GD.Print(audioLevel);
    }


    private void _onExitGameButtonPressed()
    {
        GetTree().Quit();
    }


    private void _onCloseMenuButtonPressed()
    {
        hideMenu();
    }


    private void showMenu()
    {
        Show();
        GetTree().Paused = true;
    }


    private void hideMenu()
    {
        Hide();
        GetTree().Paused = false;
    }

}