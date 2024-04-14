using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using ECommons.ImGuiMethods;
using ImGuiNET;

namespace HoardFarm.ImGuiEx;

// https://github.com/UnknownX7/Hypostasis/blob/ccaf819cb755d022790d8955d4283c95a0c10fec/ImGui/Header.cs
public static partial class ImGuiEx
{
    [LibraryImport("cimgui")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint igGetCurrentWindow();
    public static unsafe ImGuiWindow* GetCurrentWindow() => (ImGuiWindow*)igGetCurrentWindow();
    public static unsafe ImGuiWindowFlags GetCurrentWindowFlags() => GetCurrentWindow()->Flags;
    public static unsafe bool CurrentWindowHasCloseButton() => GetCurrentWindow()->HasCloseButton != 0;
    public record HeaderIconOptions
    {
        public Vector2 Offset { get; init; } = Vector2.Zero;
        public ImGuiMouseButton MouseButton { get; init; } = ImGuiMouseButton.Left;
        public string Tooltip { get; init; } = string.Empty;
        public uint Color { get; init; } = 0xFFFFFFFF;
        public bool ToastTooltipOnClick { get; init; } = false;
        public ImGuiMouseButton ToastTooltipOnClickButton { get; init; } = ImGuiMouseButton.Left;
    }

    private static uint headerLastWindowID = 0;
    private static ulong headerLastFrame = 0;
    private static uint headerCurrentPos = 0;
    private static float headerImGuiButtonWidth = 0;

    public static bool AddHeaderIcon(string id, FontAwesomeIcon icon, HeaderIconOptions options = null)
    {
        if (ImGui.IsWindowCollapsed()) return false;

        var scale = ImGuiHelpers.GlobalScale;
        var currentID = ImGui.GetID(0);
        if (currentID != headerLastWindowID || headerLastFrame != PluginInterface.UiBuilder.FrameCount)
        {
            headerLastWindowID = currentID;
            headerLastFrame = PluginInterface.UiBuilder.FrameCount;
            headerCurrentPos = 0;
            headerImGuiButtonWidth = 0f;
            if (CurrentWindowHasCloseButton())
                headerImGuiButtonWidth += 17 * scale;
            if (!GetCurrentWindowFlags().HasFlag(ImGuiWindowFlags.NoCollapse))
                headerImGuiButtonWidth += 17 * scale;
        }

        options ??= new();
        var prevCursorPos = ImGui.GetCursorPos();
        var buttonSize = new Vector2(20 * scale);
        var buttonPos = new Vector2(ImGui.GetWindowWidth() - buttonSize.X - headerImGuiButtonWidth - 20 * headerCurrentPos++ * scale - ImGui.GetStyle().FramePadding.X * 2, ImGui.GetScrollY() + 1);
        ImGui.SetCursorPos(buttonPos);
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRectFullScreen();

        var pressed = false;
        ImGui.InvisibleButton(id, buttonSize);
        var itemMin = ImGui.GetItemRectMin();
        var itemMax = ImGui.GetItemRectMax();
        var halfSize = ImGui.GetItemRectSize() / 2;
        var center = itemMin + halfSize;
        if (ImGui.IsWindowHovered() && ImGui.IsMouseHoveringRect(itemMin, itemMax, false))
        {
            if (!string.IsNullOrEmpty(options.Tooltip))
                ImGui.SetTooltip(options.Tooltip);
            ImGui.GetWindowDrawList().AddCircleFilled(center, halfSize.X, ImGui.GetColorU32(ImGui.IsMouseDown(ImGuiMouseButton.Left) ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered));
            if (ImGui.IsMouseReleased(options.MouseButton))
                pressed = true;
            if (options.ToastTooltipOnClick && ImGui.IsMouseReleased(options.ToastTooltipOnClickButton))
                PluginInterface.UiBuilder.AddNotification(options.Tooltip!, null, NotificationType.Info);
        }

        ImGui.SetCursorPos(buttonPos);
        ImGui.PushFont(UiBuilder.IconFont);
        var iconString = icon.ToIconString();
        drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), itemMin + halfSize - ImGui.CalcTextSize(iconString) / 2 + options.Offset, options.Color, iconString);
        ImGui.PopFont();

        ImGui.PopClipRect();
        ImGui.SetCursorPos(prevCursorPos);

        return pressed;
    }

}
