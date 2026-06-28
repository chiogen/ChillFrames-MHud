using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using ChillFrames.Classes;
using ChillFrames.Controllers;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace ChillFrames.Windows;

public class SettingsWindow : Window {
	private static Configuration Config => System.Config;

	public SettingsWindow() : base("ChillFrames Settings") {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(500.0f, 505.0f),
			MaximumSize = new Vector2(500.0f, 505.0f),
		};

		Flags |= ImGuiWindowFlags.NoScrollbar;
		Flags |= ImGuiWindowFlags.NoScrollWithMouse;
		Flags |= ImGuiWindowFlags.NoResize;
	}

	public override void Draw() {
		using var uiLockout = ImRaii.Disabled(Services.Condition.Any(ConditionFlag.InCombat));
		DrawLimiterStatus();

		using var tabBar = ImRaii.TabBar("ChillFramesSettingsTabBar");
		if (!tabBar) return;

		using (var settingsTab = ImRaii.TabItem("Limiter Settings")) {
			if (settingsTab) {
				DrawSettings();
			}
		}

		using (var mhudSettingsTab = ImRaii.TabItem("MangoHud"))
		{
			if (mhudSettingsTab)
				DrawMangoHudSettings();
		}

	}

	private void DrawLimiterStatus() {
		using var statusTable = ImRaii.Table("status_table", 2);
		if (!statusTable) return;

		ImGui.TableNextColumn();

		if (Config.PluginEnable) {
			var targetFps = FrameLimiterCondition.GetTargetState() switch {
				LimiterStateTarget.LowerLimit => Config.Limiter.LowerFramerateTarget,
				LimiterStateTarget.BaseLimit => Config.Limiter.BaseFramerateTarget,
				LimiterStateTarget.UpperLimit => Config.Limiter.UpperFramerateTarget,
				_ => Config.Limiter.BaseFramerateTarget,
			};

			ImGui.Text($"Target Framerate");
			ImGui.TableNextColumn();
			ImGui.Text($"{targetFps} fps");
		}
		else {
			ImGui.TextColored(KnownColor.Red.Vector(), "Limiter Inactive");
			ImGui.TableNextColumn();
		}
	}

	private void DrawSettings() {
		ImGuiHelpers.ScaledDummy(5.0f);
		DrawFpsLimitOptions();

		ImGuiHelpers.ScaledDummy(5.0f);
		DrawLimiterOptions();
	}


	private static void DrawFpsLimitOptions() {
		using var fpsInputTable = ImRaii.Table("fps_input_settings", 3);
		if (!fpsInputTable) return;

		ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		ImGui.Text("Lower Limit");
		ImGui.SameLine();
		ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.75f);
		var lowerLimit = System.Config.Limiter.LowerFramerateTarget;
		ImGui.InputInt("##LowerLimit", ref lowerLimit);
		if (ImGui.IsItemDeactivatedAfterEdit()) {
			System.Config.Limiter.LowerFramerateTarget = Math.Clamp(lowerLimit, 1, System.Config.Limiter.BaseFramerateTarget);
			System.Config.Save();
		}

		ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		ImGui.Text("Base Limit");
		ImGui.SameLine();
		ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.75f);
		var baseLimit = System.Config.Limiter.BaseFramerateTarget;
		ImGui.InputInt("##BaseLimit", ref baseLimit);
		if (ImGui.IsItemDeactivatedAfterEdit()) {
			System.Config.Limiter.BaseFramerateTarget = Math.Clamp(baseLimit, System.Config.Limiter.LowerFramerateTarget, System.Config.Limiter.UpperFramerateTarget);
			System.Config.Save();
		}

		ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		ImGui.Text("Upper Limit");
		ImGui.SameLine();
		ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.75f);
		var upperLimit = System.Config.Limiter.UpperFramerateTarget;
		ImGui.InputInt("##UpperLimit", ref upperLimit);
		if (ImGui.IsItemDeactivatedAfterEdit()) {
			System.Config.Limiter.UpperFramerateTarget = Math.Clamp(upperLimit, System.Config.Limiter.BaseFramerateTarget, 1000);
			System.Config.Save();
		}
	}

	private void DrawLimiterOptions() {
		using var table = ImRaii.Table("limiter_options_table", 3);
		if (!table) return;

		ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthFixed, 150.0f * ImGuiHelpers.GlobalScale);
		ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 75.0f * ImGuiHelpers.GlobalScale);
		ImGui.TableSetupColumn("When Condition Active", ImGuiTableColumnFlags.WidthStretch);

		ImGui.TableHeadersRow();

		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.Spacing();

		ImGui.TableNextRow();

		foreach (var option in System.LimiterOptions)
			DrawOption(option);
	}

	private void DrawOption(IFrameLimiterOption option) {
		ImGui.TableNextColumn();
		var startX = ImGui.GetCursorPosX();

		ImGuiHelpers.ScaledDummy(26.0f);
		ImGui.SameLine(0.0f);
		ImGui.SetCursorPosX(startX + 4.0f * ImGuiHelpers.GlobalScale);

		ImGui.AlignTextToFramePadding();
		ImGui.Text(option.Label);

		ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		if (option.Active) {
			ImGui.TextColored(KnownColor.Green.Vector(), "Active");
		}
		else {
			ImGui.TextColored(KnownColor.OrangeRed.Vector(), "Inactive");
		}

		ImGui.TableNextColumn();
		ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 4.0f * ImGuiHelpers.GlobalScale);

		DrawOptionCombo(option);
	}

	private string LowerLimitString => $"Use Lower Limit\t( {System.Config.Limiter.LowerFramerateTarget} fps )";
	private string BaseLimitString => $"Use Base Limit\t   ( {System.Config.Limiter.BaseFramerateTarget} fps )";
	private string UpperLimitString => $"Use Upper Limit\t( {System.Config.Limiter.UpperFramerateTarget} fps )";

	private string TargetString(LimiterStateTarget target) => target switch {
		LimiterStateTarget.LowerLimit => LowerLimitString,
		LimiterStateTarget.BaseLimit => BaseLimitString,
		LimiterStateTarget.UpperLimit => UpperLimitString,
		_ => BaseLimitString,
	};

	private void DrawOptionCombo(IFrameLimiterOption option) {
		using var combo = ImRaii.Combo($"##OptionCombo_{option.Label}", TargetString(option.Target));
		if (!combo) return;

		if (ImGui.Selectable(UpperLimitString, option.Target == LimiterStateTarget.UpperLimit)) {
			option.Target = LimiterStateTarget.UpperLimit;
			System.Config.Save();
		}

		if (ImGui.Selectable(BaseLimitString, option.Target == LimiterStateTarget.BaseLimit)) {
			option.Target = LimiterStateTarget.BaseLimit;
			System.Config.Save();
		}

		if (ImGui.Selectable(LowerLimitString, option.Target == LimiterStateTarget.LowerLimit)) {
			option.Target = LimiterStateTarget.LowerLimit;
			System.Config.Save();
		}
	}

	private void DrawMangoHudSettings()
	{
		DrawHeader("MangoHud file paths");

		string sourceFile = System.Config.MangoHudSourceFile;
		if (ImGui.InputText("Source File", ref sourceFile))
		{
			if (File.Exists(sourceFile))
			{
				System.Config.MangoHudSourceFile = sourceFile;
				System.Config.Save();
			}
		}

		string destinationFile = System.Config.MangoHudUsedFile;
		if (ImGui.InputText("Used file", ref destinationFile))
		{
			System.Config.MangoHudUsedFile = destinationFile;
			System.Config.Save();
		}
	}
	
	private static void DrawHeader(string text)
	{
		ImGuiHelpers.ScaledDummy(10.0f);
		ImGui.Text(text);
		ImGui.Separator();
		ImGuiHelpers.ScaledDummy(5.0f);
	}

}