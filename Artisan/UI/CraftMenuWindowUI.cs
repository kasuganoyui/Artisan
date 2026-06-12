using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.IPC;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using static Artisan.CraftingLogic.Solvers.ExpertSolverProfiles;
using static Artisan.CraftingLogic.Solvers.ExpertSolverSettings;

namespace Artisan.UI
{
    internal class CraftMenuWindowUI : Window
    {
        public bool EnableMacroOptions { get; set; }
        public ExpertSolverSettingsUI ExpertSettingsUI = new();

        public CraftMenuWindowUI(string windowName, ImGuiWindowFlags flags) : base(windowName, flags)
        {
            IsOpen = false;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            DisableWindowSounds = true;
            PositionCondition = ImGuiCond.Appearing;

            TitleBarButtons.Add(new()
            {
                Icon = FontAwesomeIcon.Cog,
                ShowTooltip = () => ImGui.SetTooltip("打开设置"),
                Click = (x) => P.PluginUi.IsOpen = true,
            });
        }

        public override bool DrawConditions()
        {
            return IsOpen;
        }

        public override void PreDraw()
        {
            if (P.Config.DisableTheme)
            {
                return;
            }

            P.Style.Push();
            P.StylePushed = true;
        }

        public override void PostDraw()
        {
            if (!P.StylePushed)
            {
                return;
            }

            P.Style.Pop();
            P.StylePushed = false;
        }

        public bool SolverIs(RecipeConfig config, string type)
        {
            // if no solver is loaded, check the default so things can render correctly
            bool solverLoaded = config.CurrentSolverType != "";
            switch (type)
            {
                case "standard":
                    return solverLoaded ? config.SolverIsStandard : !LuminaSheets.RecipeSheet[Endurance.RecipeID].IsExpert;
                case "expert":
                    return solverLoaded ? config.SolverIsExpert : LuminaSheets.RecipeSheet[Endurance.RecipeID].IsExpert;
                case "raph":
                case "raphael":
                    return solverLoaded ? config.SolverIsRaph : false;
                default: return false;
            }
        }

        public override void Draw()
        {
            try
            {
                if (!IsOpen)
                {
                    return;
                }
                var changed = false;
                var foundRecipe = P.Config.RecipeConfigs.GetValueOrDefault(Endurance.RecipeID);
                var config = foundRecipe ?? new();
                var autoMode = P.Config.AutoMode;
                var expertRecipe = LuminaSheets.RecipeSheet[Endurance.RecipeID].IsExpert;

                // save a new config entry for expert recipes so per-recipe settings work as expected
                if (foundRecipe == null && expertRecipe)
                    changed = true;

                if (ImGui.Checkbox("自动操作执行模式", ref autoMode))
                {
                    P.Config.AutoMode = autoMode;
                    P.Config.Save();
                }

                var enable = Endurance.Enable;
                var recipe = LuminaSheets.RecipeSheet!.First(x => x.Key == Endurance.RecipeID).Value;

                if (!CraftingListFunctions.HasItemsForRecipe(Endurance.RecipeID) && !Endurance.Enable)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.Checkbox("耐力模式开关", ref enable))
                {
                    Endurance.ToggleEndurance(enable);
                }

                if (!CraftingListFunctions.HasItemsForRecipe(Endurance.RecipeID) && !Endurance.Enable)
                {
                    ImGui.EndDisabled();
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, $"缺少材料：\r\n- {string.Join("\r\n- ", PreCrafting.MissingIngredients(recipe))}");
                }

                ExpertProfile profile = CraftingProcessor.GetExpertProfileForRecipe(config);
                ExpertSolverSettings expCfg = profile.ID == 0 ? P.Config.ExpertSolverConfig : profile.Settings;
                if (Crafting.MaterialMiracleCharges() > 0 && (SolverIs(config, "standard") || SolverIs(config, "expert")))
                {
                    int maxMiracles = SolverIs(config, "expert") ? expCfg.OverrideCosmicRecipeSettings ? expCfg.MaxMaterialMiracleUses : (int)config.ExpertMaxMaterialMiracleUses : P.Config.StandardMMUses;
                    int delayMatMiracle = SolverIs(config, "expert") ? expCfg.OverrideCosmicRecipeSettings ? expCfg.MinimumStepsBeforeMiracle : (int)config.ExpertMinimumStepsBeforeMiracle : P.Config.StandardMMSteps;
                    MMSet useMMWhen = SolverIs(config, "expert") ? expCfg.OverrideCosmicRecipeSettings ? expCfg.UseMMWhen : config.expertUseMMWhen : MMSet.Steps;

                    if (expCfg.OverrideCosmicRecipeSettings && SolverIs(config, "expert"))
                    {
                        ImGui.TextWrapped("这些设置已被您当前的专家配置文件覆盖。\r\n禁用该选项以便为每个配方单独设置。");
                        ImGui.BeginDisabled();
                    }

                    ImGui.PushItemWidth(100);
                    if (ExpertSettingsUI.SliderIntWithIcons("MaxMaterialMiracles", ref maxMiracles, 0, 3, $"{(SolverIs(config, "expert") ? "[ex] " : "")}最大 [s!MaterialMiracle] 使用次数"))
                    {
                        if (SolverIs(config, "expert"))
                        {
                            if (expCfg.OverrideCosmicRecipeSettings)
                                expCfg.MaxMaterialMiracleUses = maxMiracles;
                            else
                                config.expertMaxMaterialMiracleUses = (uint)maxMiracles;
                        }
                        else
                            P.Config.MaxMaterialMiracles = maxMiracles;
                        changed = true;
                    }

                    if (expCfg.OverrideCosmicRecipeSettings && SolverIs(config, "expert")) ImGui.EndDisabled();
                    var mmNote = "要更改 Raphael 求解器的使用方式，请前往设置 > Raphael 求解器设置。";
                    if (SolverIs(config, "expert"))
                        ImGuiComponents.HelpMarker($"此设置仅适用于专家求解器。\r\n{mmNote}");
                    if (SolverIs(config, "standard"))
                        ImGuiComponents.HelpMarker($"此选项将在增益效果持续期间将标准配方求解器切换为专家求解器。\r\n{mmNote}");

                    if (expCfg.OverrideCosmicRecipeSettings && SolverIs(config, "expert")) ImGui.BeginDisabled();

                    if (maxMiracles > 0)
                    {
                        if (SolverIs(config, "expert"))
                        {
                            ImGui.PushItemWidth(250);
                            if (ImGui.BeginCombo("##mmSet", expCfg.GetMMSet(useMMWhen)))
                            {
                                foreach (MMSet x in Enum.GetValues<MMSet>())
                                {
                                    if (ImGui.Selectable(expCfg.GetMMSet(x)))
                                    {
                                        if (expCfg.OverrideCosmicRecipeSettings)
                                            expCfg.UseMMWhen = x;
                                        else
                                            config.expertUseMMWhen = x;
                                        changed = true;
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            ImGui.SameLine(0.0f, 4.0f);
                            ExpertSettingsUI.DrawIconText("[ex] 何时开始");
                        }
                        else
                            ImGui.Text("在此步数后使用：");

                        if ((SolverIs(config, "expert") && useMMWhen == MMSet.Steps) || SolverIs(config, "standard"))
                        {
                            ImGui.PushItemWidth(250);
                            if (ImGui.SliderInt($"###MaterialMiracleSlider", ref delayMatMiracle, 0, 20))
                            {
                                if (SolverIs(config, "expert"))
                                {
                                    if (expCfg.OverrideCosmicRecipeSettings)
                                        expCfg.MinimumStepsBeforeMiracle = delayMatMiracle;
                                    else
                                        config.expertMinimumStepsBeforeMiracle = (uint)delayMatMiracle;
                                }
                                else
                                    P.Config.MinimumStepsBeforeMiracle = delayMatMiracle;
                                changed = true;
                            }
                            if (SolverIs(config, "expert"))
                            {
                                ImGui.SameLine(0.0f, 4.0f);
                                ExpertSettingsUI.DrawIconText("[ex] 步数");
                            }
                        }
                    }
                    if (expCfg.OverrideCosmicRecipeSettings && SolverIs(config, "expert")) ImGui.EndDisabled();
                }

                // todo: should this set the raph setting, not just tell users where to set it?
                if (Crafting.SteadyHandCharges() > 0)
                {
                    if (expertRecipe && SolverIs(config, "expert"))
                    {
                        int maxSteady = expCfg.OverrideCosmicRecipeSettings ? expCfg.MaxSteadyUses : (int)config.ExpertMaxSteadyUses;

                        ImGui.PushItemWidth(100);
                        if (expCfg.OverrideCosmicRecipeSettings && SolverIs(config, "expert"))
                        {
                            ImGui.TextWrapped("此设置已被您当前的专家配置文件覆盖。\r\n禁用该选项以便为每个配方单独设置。");
                            ImGui.BeginDisabled();
                        }
                        if (ExpertSettingsUI.SliderIntWithIcons("MaxSteadyUses", ref maxSteady, 0, 2, "[ex] 最大 [s!SteadyHand] 使用次数"))
                        {
                            if (expCfg.OverrideCosmicRecipeSettings)
                                expCfg.MaxSteadyUses = maxSteady;
                            else
                                config.expertMaxSteadyUses = (uint)maxSteady;
                            changed = true;
                        }
                        if (expCfg.OverrideCosmicRecipeSettings) ImGui.EndDisabled();
                        ImGuiComponents.HelpMarker($"此设置仅适用于专家求解器。\r\n要更改 Raphael 求解器的使用方式，请前往设置 > Raphael 求解器设置。");
                    }
                    else if (config.SolverIsRaph || config.SolverIsStandard)
                    {
                        ImGui.TextWrapped($"此任务支持 {Skills.SteadyHand.NameOfAction()}。要更改 Raphael 求解器的使用方式，请前往设置 > Raphael 求解器设置。");
                    }
                }

                if (EnableMacroOptions)
                {
                    ImGui.Spacing();

                    if (SimpleTweaks.IsFocusTweakEnabled())
                    {
                        ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, $@"警告：您已启用 ""Auto Focus Recipe Search"" SimpleTweak。此功能与 Artisan 高度不兼容，建议将其禁用。");
                    }

                    if (Endurance.RecipeID == 0)
                    {
                        return;
                    }

                    changed |= config.Draw(Endurance.RecipeID);

                    if (changed)
                    {
                        P.Config.RecipeConfigs[Endurance.RecipeID] = config;
                        P.Config.Save();
                    }
                }
            }
            catch { }
        }
    }
}
