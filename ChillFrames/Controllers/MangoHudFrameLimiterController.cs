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

public class MangoHudFrameLimiterController : IFrameLimiterController, IDisposable
{


    private static LimiterSettings Settings => System.Config.Limiter;

    private int storedValue = 0;


    public MangoHudFrameLimiterController()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Dispose()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework)
    {
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
        if (value == storedValue)
            return;

        UpdateValueAsync(value).WaitSafely();
        storedValue = value;
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