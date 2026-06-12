using Artisan.CraftingLogic.Solvers;
using Artisan.UI.Tables;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using OtterGui;
using OtterGui.Filesystem;
using PunishLib.ImGuiMethods;
using System;
using System.Linq;
using System.Numerics;
using static Artisan.CraftingLogic.Solvers.ExpertSolverProfiles;

namespace Artisan.UI
{
    internal class ExpertProfilesUI
    {
        internal static ExpertProfile selectedProfile = new();
        private static readonly ExpertProfileList EPL = new();

        internal static void Draw()
        {
            try
            {
                ImGui.TextWrapped($"专家求解器配置文件是特定专家求解器设置的一个快照或\"配置方案\"。和宏一样，不同的配置文件可以分配给特定的专家配方。");

                ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"重要提示：这些不是高级设置或\"专家用户\"配置文件。它们仅用于专家配方求解器。");
                var expertIcon = P.PluginUi.ExpertSettingsUI.expertIcon;
                if (expertIcon != null)
                {
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"专家配方在制作笔记中显示为此图标：");
                    ImGui.SameLine();
                    ImGui.Image(expertIcon.Handle, expertIcon.Size, new Vector2(0, 0), new Vector2(1, 1), new Vector4(0.94f, 0.57f, 0f, 1f));
                }

                ImGui.Dummy(new Vector2(0, 5f));
                if (IconButtons.IconTextButton(Dalamud.Interface.FontAwesomeIcon.ExternalLinkAlt, "编辑全局专家求解器设置"))
                {
                    P.PluginUi.OpenWindow = OpenWindow.Main;
                }

                ImGui.Dummy(new Vector2(0, 10f));
                ImGui.TextWrapped("左键单击配置文件进行编辑。右键单击配置文件以选中它而不编辑。");

                ImGui.Dummy(new Vector2(0, 5f));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 5f));

                ImGui.BeginChild("ProfileSelector", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 200f));
                EPL.Draw(ImGui.GetContentRegionAvail().X);
                ImGui.EndChild();

                ImGui.Spacing();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }
    }

    internal class ExpertProfileList : ItemSelector<ExpertProfile>
    {
        public ExpertProfileList()
            : base(P.Config.ExpertSolverProfiles.ExpertProfiles, Flags.Add | Flags.Delete | Flags.Move | Flags.Filter | Flags.Duplicate)
        {
            CurrentIdx = -1;
        }

        protected override string AddButtonTooltip()
        {
            return "添加新配置文件";
        }

        protected override string DeleteButtonTooltip()
        {
            return "永久删除选中的配置文件\r\n（按住 Ctrl 确认）";
        }

        protected override bool Filtered(int idx)
        {
            return Filter.Length != 0 && !Items[idx].Name.Contains(
                       Filter,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        protected override bool OnAdd(string name)
        {
            Svc.Log.Information($"OnAdd");
            try
            {
                var profile = new ExpertProfile { Name = name, Settings = new ExpertSolverSettings() };
                P.Config.ExpertSolverProfiles.AddNewExpertProfile(profile);
                P.Config.Save();

                return true;
            }
            catch (Exception ex)
            {
                ex.Log();
                return false;
            }
        }

        protected override bool OnDelete(int idx)
        {
            if (P.ws.Windows.TryGetFirst(
                    x => x.WindowName.Contains(ExpertProfilesUI.selectedProfile.ID.ToString()) && x.GetType() == typeof(ExpertProfileEditor),
                    out var window))
            {
                P.ws.RemoveWindow(window);
            }

            P.Config.ExpertSolverProfiles.ExpertProfiles.RemoveAt(idx);
            P.Config.Save();

            ExpertProfilesUI.selectedProfile = new ExpertProfile();
            return true;
        }

        protected override bool OnDraw(int idx, out bool changes)
        {
            changes = false;
            if (ExpertProfilesUI.selectedProfile.ID == P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID)
                ImGui.BeginDisabled();

            using var id = ImRaii.PushId(idx);
            var selected = ImGui.Selectable($"{P.Config.ExpertSolverProfiles.ExpertProfiles[idx].Name} (ID: {P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID})", idx == CurrentIdx);
            if (selected)
            {
                if (!P.ws.Windows.Any(x => x.WindowName.Contains(P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID.ToString())))
                {
                    Interface.SetupValues();
                    ExpertProfileEditor editor = new(P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID);
                }
                else
                {
                    P.ws.Windows.TryGetFirst(
                        x => x.WindowName.Contains(P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID.ToString()),
                        out var window);
                    window.BringToFront();
                }

                ExpertProfilesUI.selectedProfile = P.Config.ExpertSolverProfiles.ExpertProfiles[idx];
            }


            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                if (CurrentIdx == idx)
                {
                    CurrentIdx = -1;
                    ExpertProfilesUI.selectedProfile = new ExpertProfile();
                }
                else
                {
                    CurrentIdx = idx;
                    ExpertProfilesUI.selectedProfile = P.Config.ExpertSolverProfiles.ExpertProfiles[idx];
                }
            }


            if (ExpertProfilesUI.selectedProfile.ID == P.Config.ExpertSolverProfiles.ExpertProfiles[idx].ID)
                ImGui.EndDisabled();

            return selected;
        }

        protected override bool OnDuplicate(string name, int idx)
        {
            var baseProfile = P.Config.ExpertSolverProfiles.ExpertProfiles[idx];
            ExpertProfile newProfile = new ExpertProfile();
            newProfile = baseProfile.JSONClone();
            newProfile.Name = name;
            P.Config.ExpertSolverProfiles.AddNewExpertProfile(newProfile);
            P.Config.Save();
            return true;
        }

        protected override bool OnMove(int idx1, int idx2)
        {
            P.Config.ExpertSolverProfiles.ExpertProfiles.Move(idx1, idx2);
            return true;
        }
    }
}
