using Godot;
using System;

public partial class StartScreen : CanvasLayer
{
    public override void _Ready()
    {
        GD.Print("StartScreen ready");
        try
        {
            var startBtn = GetNode("VBox/StartButton");
            if (startBtn != null)
            {
                startBtn.Connect("pressed", new Callable(this, nameof(OnStartPressed)));
                GD.Print("Connected StartButton");
            }

            var optionsBtn = GetNode("VBox/OptionsButton");
            if (optionsBtn != null)
            {
                optionsBtn.Connect("pressed", new Callable(this, nameof(OnOptionsPressed)));
                GD.Print("Connected OptionsButton");
            }

            var quitBtn = GetNode("VBox/QuitButton");
            if (quitBtn != null)
            {
                quitBtn.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));
                GD.Print("Connected QuitButton");
            }

            // Options dialog nodes
            var volumeSlider = GetNode("OptionsDialog/Margin/VBoxContainer/HBox/VolumeSlider");
            if (volumeSlider != null)
            {
                // Connect the value_changed signal
                volumeSlider.Connect("value_changed", new Callable(this, nameof(OnVolumeChanged)));
                GD.Print("Connected VolumeSlider");
            }

            // Increase the visible handle (grabber) size for the HSlider by applying a simple StyleBox
            // Create a small StyleBoxFlat and assign it to the slider's custom_styles/handle to make the knob larger
            try
            {
                var hs = GetNode<HSlider>("OptionsDialog/Margin/VBoxContainer/HBox/VolumeSlider");
                if (hs != null)
                {
                    var style = new StyleBoxFlat();
                    style.BgColor = new Color(0.9f, 0.9f, 0.9f); // light handle color
                    style.ContentMarginLeft = 8;
                    style.ContentMarginRight = 8;
                    style.ContentMarginTop = 8;
                    style.ContentMarginBottom = 8;
                    // Assign as the custom style for the handle (grabber)
                    hs.Set("custom_styles/handle", style);
                    // Ensure slider has enough minimum size to allow full travel
                    // Use the generic property setter to avoid compile-time property mismatch across Godot C# bindings
                    hs.Set("rect_min_size", new Vector2(300, 24));
                    GD.Print("Applied custom StyleBoxFlat to VolumeSlider handle");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("Failed to apply custom style to VolumeSlider: ", ex.Message);
            }

            var closeBtn = GetNode("OptionsDialog/Margin/VBoxContainer/CloseButton");
            if (closeBtn != null)
            {
                closeBtn.Connect("pressed", new Callable(this, nameof(OnOptionsClosePressed)));
                GD.Print("Connected Options CloseButton");
            }

            // Load saved volume (if any) and apply
            LoadAndApplyVolume();
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error in StartScreen._Ready: ", ex.Message);
        }
    }

    public override void _Process(double delta)
    {
        // Enter (ui_accept) starts the game
        if (Input.IsActionJustPressed("ui_accept"))
        {
            OnStartPressed();
        }
    }

    private void OnStartPressed()
    {
        GD.Print("Start pressed -> change scene to Town.tscn");
        // Open the Town scene when Start is pressed. If the scene has errors,
        // Godot will report them in the editor/runtime.
        GetTree().ChangeSceneToFile("res://scenes/Town.tscn");
    }

    private void OnOptionsPressed()
    {
        GD.Print("Options pressed -> showing options dialog");
    var dlg = GetNode("OptionsDialog");
    if (dlg != null)
    {
        // Use property set to avoid calling methods that may not be exposed on Node wrapper
        dlg.Set("visible", true);
        GD.Print("OptionsDialog set visible = true");
    }
    }

    private void OnOptionsClosePressed()
    {
    var dlg = GetNode("OptionsDialog");
    if (dlg != null)
    {
        dlg.Set("visible", false);
        GD.Print("OptionsDialog set visible = false");
    }
        SaveVolume();
    }

    private void OnVolumeChanged(double value)
    {
        // Update the label and apply volume to audio bus
        var lbl = GetNode<Label>("OptionsDialog/Margin/VBoxContainer/HBox/VolumeValue");
        int percent = (int)Math.Round(value);
        lbl.Text = percent + "%";

        // value is percent (0..100) in the UI; ApplyVolume expects 0..1
        ApplyVolume(percent / 100.0);
    }

    private void LoadAndApplyVolume()
    {
        var cfg = new ConfigFile();
        var err = cfg.Load("user://settings.cfg");
        double volPercent = 100.0; // store percent 0..100
        if (err == Error.Ok)
        {
            var v = cfg.GetValue("audio", "master_volume_percent", 100.0);
            try
            {
                volPercent = Convert.ToDouble(v);
            }
            catch
            {
                volPercent = 100.0;
            }
        }

        // Set slider and label (UI uses 0..100)
        var volumeSlider = GetNode<HSlider>("OptionsDialog/Margin/VBoxContainer/HBox/VolumeSlider");
        volumeSlider.Value = volPercent;
        var lbl = GetNode<Label>("OptionsDialog/Margin/VBoxContainer/HBox/VolumeValue");
        lbl.Text = ((int)Math.Round(volPercent)) + "%";

        ApplyVolume(volPercent / 100.0);
    }

    private void SaveVolume()
    {
        var volumeSlider = GetNode<HSlider>("OptionsDialog/Margin/VBoxContainer/HBox/VolumeSlider");
        double volPercent = volumeSlider.Value;
        var cfg = new ConfigFile();
        cfg.SetValue("audio", "master_volume_percent", volPercent);
        cfg.Save("user://settings.cfg");
    }

    private void ApplyVolume(double value)
    {
        // Convert linear 0..1 to dB. If value==0, set to -80 dB (effectively mute).
        float db;
        if (value <= 0.0)
            db = -80f;
        else
            db = (float)(20.0 * Math.Log10(value));

        int busIdx = AudioServer.GetBusIndex("Master");
        if (busIdx >= 0)
        {
            AudioServer.SetBusVolumeDb(busIdx, db);
        }
    }

    private void OnQuitPressed()
    {
        GD.Print("Quit pressed -> quitting");
        GetTree().Quit();
    }
}
