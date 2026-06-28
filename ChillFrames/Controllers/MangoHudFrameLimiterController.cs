using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ChillFrames.Classes;
using ChillFrames.Utilities;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

namespace ChillFrames.Controllers;

public class MangoHudFrameLimiterController : IDisposable
{


    private static LimiterSettings Settings => System.Config.Limiter;

    public int Value = 0;


    public MangoHudFrameLimiterController()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Dispose()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework)
    {

        if (!System.Config.PluginEnable)
        {
            SetValue(0);
            return;
        }


        var targetState = FrameLimiterCondition.GetTargetState();

        switch (targetState)
        {
            case LimiterStateTarget.UpperLimit:
                SetValue(Settings.UpperFramerateTarget);
                break;

            case LimiterStateTarget.LowerLimit:
                SetValue(Settings.BaseFramerateTarget);
                break;

            case LimiterStateTarget.BaseLimit:
                SetValue(Settings.BaseFramerateTarget);
                break;
        }
    }

    private void SetValue(int value)
    {
        if (value == Value)
            return;

        UpdateValueAsync(value).WaitSafely();
        Value = value;
    }

    private static async Task UpdateValueAsync(int value)
    {
        try
        {
            var sourceFile = System.Config.MangoHudSourceFile;
            var destinationFile = System.Config.MangoHudUsedFile;

            if (!File.Exists(sourceFile))
                return;

            List<string> lines = [.. await File.ReadAllLinesAsync(sourceFile)];
            string fpsLimitLinePrefix = "fps_limit=";

            int lineIndex = lines.FindIndex(x => x.StartsWith(fpsLimitLinePrefix, StringComparison.OrdinalIgnoreCase));
            if (lineIndex == -1)
                lines.Add($"fps_limit={value}");
            else
                lines[lineIndex] = $"fps_limit={value}";

            // Write everything in a single, atomic operation (overwrites automatically)
            await File.WriteAllLinesAsync(destinationFile, lines);

        }
        catch (Exception ex)
        {
            Log.Error("Failed to update MangoHud FPS limit.", ex);
        }
    }

}