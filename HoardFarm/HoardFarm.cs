using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AutoRetainerAPI;
using Dalamud;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.Reflection;
using HoardFarm.IPC;
using HoardFarm.Model;
using HoardFarm.Service;
using HoardFarm.Windows;

namespace HoardFarm;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class HoardFarm : IDalamudPlugin
{
    private readonly AchievementService achievementService;
    private readonly AutoRetainerApi autoRetainerApi;
    private readonly ConfigWindow configWindow;
    private readonly DeepDungeonMenuOverlay deepDungeonMenuOverlay;

    private readonly HoardFarmService hoardFarmService;
    private readonly MainWindow mainWindow;
    private readonly RetainerService retainerService;
    public readonly WindowSystem WindowSystem = new("HoardFarm");

    public HoardFarm(DalamudPluginInterface? pluginInterface)
    {
        pluginInterface?.Create<PluginService>();
        P = this;

        ECommonsMain.Init(pluginInterface, this, Module.DalamudReflector);
        DalamudReflector.RegisterOnInstalledPluginsChangedEvents(() =>
        {
            if (PluginInstalled(NavmeshIPC.Name))
                NavmeshIPC.Init();
        });

        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        mainWindow = new MainWindow();
        configWindow = new ConfigWindow();
        deepDungeonMenuOverlay = new DeepDungeonMenuOverlay();

        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(deepDungeonMenuOverlay);

        hoardFarmService = new HoardFarmService();
        HoardService = hoardFarmService;

        achievementService = new AchievementService();
        Achievements = achievementService;

        autoRetainerApi = new AutoRetainerApi();
        RetainerApi = autoRetainerApi;

        retainerService = new RetainerService();
        RetainerScv = retainerService;

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += () => OnCommand();
        PluginInterface.UiBuilder.OpenConfigUi += ShowConfigWindow;
        Framework.Update += FrameworkUpdate;

        PluginService.TaskManager = new TaskManager();


        EzCmd.Add("/hoardfarm", (_, args) => OnCommand(args),
                  "Opens the Hoard Farm window.\n" +
                  "/hoardfarm config | c → Open the config window.\n" +
                  "/hoardfarm enable | e → Enable farming mode.\n" +
                  "/hoardfarm disable | d → Disable farming mode.\n" +
                  "/hoardfarm toggle | t → Toggle farming mode.\n"
        );

        CultureInfo.DefaultThreadCurrentUICulture = ClientState.ClientLanguage switch
        {
            ClientLanguage.French => CultureInfo.GetCultureInfo("fr"),
            ClientLanguage.German => CultureInfo.GetCultureInfo("de"),
            ClientLanguage.Japanese => CultureInfo.GetCultureInfo("ja"),
            _ => CultureInfo.GetCultureInfo("en")
        };
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        hoardFarmService.Dispose();

        autoRetainerApi.Dispose();
        retainerService.Dispose();

        Framework.Update -= FrameworkUpdate;
        ECommonsMain.Dispose();
    }


    private void FrameworkUpdate(IFramework framework)
    {
        Tick();
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void OnCommand(string? args = null)
    {
        args = args?.Trim().ToLower() ?? "";

        switch (args)
        {
            case "c":
            case "config":
                ShowConfigWindow();
                return;
            case "e":
            case "enable":
                HoardService.HoardMode = true;
                return;
            case "d":
            case "disable":
                if (HoardService.HoardMode) HoardService.FinishRun = true;
                return;
            case "t":
            case "toggle":
                HoardService.HoardMode = !HoardService.HoardMode;
                return;
            default:
                ShowMainWindow();
                break;
        }
    }

    public void ShowConfigWindow()
    {
        configWindow.IsOpen = true;
    }

    public void ShowMainWindow()
    {
        if (!mainWindow.IsOpen)
        {
            Achievements.UpdateProgress();
            mainWindow.IsOpen = true;
        }
    }
}
