using ChillFrames.Utilities;

namespace ChillFrames.Classes;

public class Configuration {
	public float DisableIncrementSetting = 0.025f;
	public float EnableIncrementSetting = 0.01f;

	public GeneralSettings General = new();
	public LimiterSettings Limiter = new();

	public bool PluginEnable = true;

	// MangoHud config
	public bool UseMangoHud = true;
	public string MangoHudSourceFile = "X:/.config/MangoHud/MangoHud.conf";
	public string MangoHudUsedFile = "X:/.config/MangoHud/ffxiv.conf";

	public static Configuration Load()
		=> Config.LoadConfig<Configuration>("System.config.json");

	public void Save()
		=> Config.SaveConfig(this, "System.config.json");
}