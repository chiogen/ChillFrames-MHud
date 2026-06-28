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
            bool modified = false; // When false, the fps_limit line has to be added

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("fps_limit=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"fps_limit={value}";
                    modified = true;
                    break; // Stop looking once we've found and updated the value
                }
            }

            // Write everything in a single, atomic operation (overwrites automatically)
            await File.WriteAllLinesAsync(destinationFile, lines);

            if (!modified)
                File.AppendAllText(destinationFile, $"\nfps_limit={value}");

        }
        catch (Exception ex)
        {
            Log.Error("Failed to update MangoHud FPS limit.", ex);
        }
    }

}