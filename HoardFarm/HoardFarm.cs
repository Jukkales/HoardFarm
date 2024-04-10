using System.Globalization;
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

public sealed class HoardFarm : IDalamudPlugin
{
    private readonly HoardFarmService hoardFarmService;
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    public WindowSystem WindowSystem = new("HoardFarm");

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

        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);

        hoardFarmService = new HoardFarmService();
        HoardService = hoardFarmService;

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ShowMainWindow;
        PluginInterface.UiBuilder.OpenConfigUi += ShowConfigWindow;
        Framework.Update += FrameworkUpdate;
        
        PluginService.TaskManager = new TaskManager();
        
        
        EzCmd.Add("/hoardfarm", (_, _) => ShowMainWindow() ,"Open the Hoard Farm window.");

        CultureInfo.DefaultThreadCurrentUICulture = ClientState.ClientLanguage switch
        {
            ClientLanguage.French => CultureInfo.GetCultureInfo("fr"),
            ClientLanguage.German => CultureInfo.GetCultureInfo("de"),
            ClientLanguage.Japanese => CultureInfo.GetCultureInfo("ja"),
            _ => CultureInfo.GetCultureInfo("en")
        };
    }

    private void FrameworkUpdate(IFramework framework)
    {
        YesAlreadyManager.Tick();
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        configWindow.Dispose();
        hoardFarmService.Dispose();
        
        Framework.Update -= FrameworkUpdate;
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void ShowMainWindow()
    {
        mainWindow.IsOpen = true;
    }
    
    public void ShowConfigWindow()
    {
        configWindow.IsOpen = true;
    }
}
