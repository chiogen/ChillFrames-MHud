using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

namespace ChillFrames.Controllers;

public class DtrController : IDisposable {
	private readonly IDtrBarEntry dtrEntry;
	private double smoothedFps;

	public DtrController() {
		dtrEntry = Services.DtrBar.Get("Chill Frames");

		dtrEntry.Tooltip = GetTooltip();
		dtrEntry.OnClick = DtrOnClick;

		dtrEntry.Shown = System.Config.General.EnableDtrBar;
	}

	public void Dispose()
		=> dtrEntry.Remove();

	private void DtrOnClick(DtrInteractionEvent dtrInteractionEvent) {
		switch (dtrInteractionEvent.ClickType) {
			case MouseClickType.Left:
				if (Services.Condition.Any(ConditionFlag.InCombat)) return;
				System.Config.PluginEnable = !System.Config.PluginEnable;
				System.Config.Save();
				break;

			case MouseClickType.Right:
				System.ConfigWindow.Toggle();
				break;
		}

		dtrEntry.Tooltip = GetTooltip();
	}

	public void Update() {
		var rawFps = 1000 / FrameLimiterController.LastFrametime.TotalMilliseconds;
		var alpha = Math.Min(FrameLimiterController.LastFrametime.TotalSeconds / 1.0, 1.0);
		smoothedFps = smoothedFps == 0.0 ? rawFps : smoothedFps * (1.0 - alpha) + rawFps * alpha;
		var displayFps = $"{smoothedFps:N0} FPS";

		if (System.Config.General.EnableDtrColor) {
			dtrEntry.Text = new SeStringBuilder()
				.PushColorRgba(System.Config.PluginEnable ? System.Config.General.ActiveColor : System.Config.General.InactiveColor)
				.Append(displayFps)
				.PopColor()
				.ToReadOnlySeString()
				.ToDalamudString();
		}
		else {
			dtrEntry.Text = displayFps;
		}
	}

	private SeString GetTooltip()
		=> $"{(System.Config.PluginEnable ? "Left Click to Disable Limiter" : "Left Click to Enable Limiter")}\n" +
		   $"Right click to open Config Window";

	public void UpdateEnabled()
		=> dtrEntry.Shown = System.Config.General.EnableDtrBar;
}