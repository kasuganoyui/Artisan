using Artisan.CraftingLogic.CraftData;
using Artisan.CraftingLogic.Solvers;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using PunishLib.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using static Artisan.CraftingLogic.Solvers.ExpertSolverSettings;
using static Artisan.RawInformation.AddonExtensions;

namespace Artisan.UI;

internal class ExpertSolverSettingsUI
{
    public IDalamudTextureWrap? expertIcon;

    public enum SkillIconID
    {
        BasicSynthesis = 1501,
        CarefulSynthesis = 1986,
        RapidSynthesis = 1988,
        Groundwork = 1518,
        IntensiveSynthesis = 1514,
        PrudentSynthesis = 1520,
        MuscleMemory = 1994,

        BasicTouch = 1502,
        StandardTouch = 1516,
        AdvancedTouch = 1519,
        HastyTouch = 1989,
        PreparatoryTouch = 1507,
        PreciseTouch = 1524,
        PrudentTouch = 1535,
        TrainedFinesse = 1997,
        Reflect = 1982,
        RefinedTouch = 1522,
        DaringTouch = 1998,

        ByregotsBlessing = 1975,
        TrainedEye = 1981,
        DelicateSynthesis = 1503,

        Veneration = 1995,
        Innovation = 1987,
        GreatStrides = 1955,
        TricksOfTrade = 1990,
        MastersMend = 1952,
        Manipulation = 1985,
        WasteNot = 1992,
        WasteNot2 = 1993,
        Observe = 1954,
        CarefulObservation = 1984,
        FinalAppraisal = 1983,
        HeartAndSoul = 1996,
        QuickInnovation = 1999,
        ImmaculateMend = 1950,
        TrainedPerfection = 1926,

        MaterialMiracle = 61277,
        SteadyHand = 1953,
    }

    public Dictionary<string, Vector4> ConditionColors = new()
    {
        { "Normal",    new(1.000f, 1.000f, 1.000f, 1f) },
        { "Centered",  new(0.949f, 0.863f, 0.137f, 1f) },
        { "Sturdy",    new(0.153f, 0.718f, 0.871f, 1f) },
        { "Pliant",    new(0.043f, 0.831f, 0.043f, 1f) },
        { "Malleable", new(0.200f, 0.400f, 1.000f, 1f) },
        { "Primed",    new(0.769f, 0.220f, 0.984f, 1f) },
        { "Good",      new(1.000f, 0.353f, 0.408f, 1f) },
        { "Good Omen", new(1.000f, 0.843f, 0.722f, 1f) },
        { "Robust",    new(0.373f, 0.773f, 1.000f, 1f) },
        { "Excellent", new(0.992f, 0.820f, 1.000f, 1f) },
    };

    public ExpertSolverSettingsUI()
    {
        var tex = Svc.PluginInterface.UiBuilder.LoadUld("ui/uld/RecipeNoteBook.uld");
        expertIcon = tex?.LoadTexturePart("ui/uld/RecipeNoteBook_hr1.tex", 14);
    }

    public bool DrawGeneralSettings(ExpertSolverSettings s)
    {
        bool changed = false;

        ImGui.PushItemWidth(250);
        changed |= SliderIntWithIcons("ImmacMissingDura", ref s.ImmacMissingDura, 30, 80, "当缺少 {0} 达到此数值时优先使用 [s!ImmaculateMend]", [DurabilityString.ToLower()]);

        ImGui.PushItemWidth(250);
        changed |= SliderIntWithIcons("ManipClipTurns", ref s.ManipClipTurns, 0, 10, "在还剩以下回合数时重新施加 [s!Manipulation]");

        ImGui.Dummy(new Vector2(0, 5f));
        DrawIconText("使用 [s!TrainedPerfection] 的时机：");
        HelpMarkerWithIcons(["\"(Late)\" 选项会尝试在 [s!Innovation] 和 [s!GreatStrides] 下使用 [s!PreparatoryTouch]。", "\"Either action\" 在普通 {0} 下默认使用 [s!Groundwork]。"], [ConditionString.ToLower()]);
        ImGui.PushItemWidth(400);
        if (ImGui.BeginCombo("##midUseTPSetting", s.GetMidUseTPSettingName(s.MidUseTP)))
        {
            foreach (MidUseTPSetting x in Enum.GetValues<MidUseTPSetting>())
            {
                if (ImGui.Selectable(s.GetMidUseTPSettingName(x)))
                {
                    s.MidUseTP = x;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }
        if (s.MidUseTP != MidUseTPSetting.MidUseTPDuringQuality)
        {
            ImGui.PushItemWidth(150);
            changed |= SliderIntWithIcons("MidMaxBaitStepsForTP", ref s.MidMaxBaitStepsForTP, 0, 5, "在 [s!TrainedPerfection] 期间使用 [s!Observe] 此次数以寻找更好的 {0}（0 为禁用）", [ConditionString.ToLower()]);
            HelpMarkerWithIcons(["寻找 [c!Malleable] 以使用 [s!Groundwork]。", "寻找 [c!Good] 或 [c!Pliant] 以使用 [s!PreparatoryTouch]。"]);
        }

        ImGui.Dummy(new Vector2(0, 5f));
        ImGui.TextWrapped($"伊修加德复兴");
        ImGui.Indent();
        changed |= ImGui.Checkbox("最大化伊修加德复兴配方品质，而非仅达到最大阈值", ref s.MaxIshgardRecipes);
        ImGuiComponents.HelpMarker("这将尝试最大化品质以获得更多天穹点数。");
        ImGui.Unindent();

        ImGui.TextWrapped($"宇宙探索");
        ImGui.Indent();
        changed |= ImGui.Checkbox("最大化宇宙探索配方品质，而非仅达到第三阈值", ref s.MaxCosmicRecipes);
        changed |= ImGui.Checkbox("覆盖各配方的宇宙探索设置###overrideCosmic", ref s.OverrideCosmicRecipeSettings);
        ImGuiComponents.HelpMarker("默认情况下，宇宙探索设置为每个配方单独保存。启用此选项将改用以下全局设置。");
        ImGui.Indent();
        if (!s.OverrideCosmicRecipeSettings) ImGui.BeginDisabled();
        ImGui.Dummy(new Vector2(0, 2f));
        DrawIconText("[s!MaterialMiracle] 使用：");
        ImGui.Indent();
        ImGui.PushItemWidth(250);
        changed |= SliderIntWithIcons("MaxMaterialMiracleUses", ref s.MaxMaterialMiracleUses, 0, 3, "每次制作最大使用次数");
        ImGuiComponents.HelpMarker("如果使用次数大于一次，增益效果结束后会立即重新施加。");
        if (s.MaxMaterialMiracleUses > 0)
        {
            ImGui.PushItemWidth(250);
            if (ImGui.BeginCombo("开始时机##mmSet", s.GetMMSet(s.UseMMWhen)))
            {
                foreach (MMSet x in Enum.GetValues<MMSet>())
                {
                    if (ImGui.Selectable(s.GetMMSet(x)))
                    {
                        s.UseMMWhen = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            if (s.UseMMWhen == MMSet.Steps)
            {
                ImGui.PushItemWidth(250);
                changed |= SliderIntWithIcons("MinimumStepsBeforeMiracle", ref s.MinimumStepsBeforeMiracle, 0, 20, "步骤数");
            }
        }
        ImGui.Unindent();

        ImGui.Dummy(new Vector2(0, 5f));
        ImGui.PushItemWidth(250);
        changed |= SliderIntWithIcons("MaxSteadyUses", ref s.MaxSteadyUses, 0, 2, "每次制作最大 [s!SteadyHand] 使用次数");
        HelpMarkerWithIcons(["[s!SteadyHand] 会尽快使用以确保 [s!RapidSynthesis] 成功。", "设为 0 以禁用。"]);
        if (!s.OverrideCosmicRecipeSettings) ImGui.EndDisabled();
        ImGui.Unindent();
        ImGui.Unindent();

        return changed;
    }

    public bool DrawOpenerSettings(ExpertSolverSettings s)
    {
        bool changed = false;
        try
        {
            ImGui.TextWrapped($"起手动作：");
            ImGui.PushItemWidth(400);
            if (ImGui.BeginCombo("##OpenerAction", s.GetOpenerSet(s.OpenerAction)))
            {
                foreach (OpenerSet x in Enum.GetValues<OpenerSet>())
                {
                    if (ImGui.Selectable(s.GetOpenerSet(x)))
                    {
                        s.OpenerAction = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            if (s.OpenerAction != OpenerSet.MuMe)
            {
                ImGui.Dummy(new Vector2(0, 5f));
                changed |= CheckboxWithIcons("ReflectQuickInno", ref s.ReflectQuickInno, "在 [s!Reflect] 之前使用 [s!QuickInnovation]");
            }

            if (s.OpenerAction != OpenerSet.Reflect)
            {
                ImGui.Dummy(new Vector2(0, 5f));
                DrawIconText("当 [s!MuscleMemory] 增益效果激活时：");
                ImGui.Indent();
                changed |= CheckboxWithIcons("MuMeIntensiveGood", ref s.MuMeIntensiveGood, "当 [c!Good] 时，优先使用 [s!IntensiveSynthesis]（400%）而非 [s!RapidSynthesis]（500%）");
                changed |= CheckboxWithIcons("MuMeIntensiveMalleable", ref s.MuMeIntensiveMalleable, "当 [c!Malleable] 时，使用 [s!HeartAndSoul] → [s!IntensiveSynthesis]（如可用）");
                changed |= CheckboxWithIcons("MuMePrimedManip", ref s.MuMePrimedManip, "当 [c!Primed] 且 [s!Veneration] 已激活时，使用 [s!Manipulation]");
                HelpMarkerWithIcons("如禁用此选项，[s!Manipulation] 将在 [s!MuscleMemory] 激活期间仅在 [c!Pliant] 时使用。");
                changed |= CheckboxWithIcons("MuMeAllowObserve", ref s.MuMeAllowObserve, "当 [c!Normal] 或其他无关 {0} 时，使用 [s!Observe] 替代 [s!RapidSynthesis]", [ConditionString.ToLower()]);
                HelpMarkerWithIcons("这会以消耗 [s!MuscleMemory] 步骤为代价节省 {0}。", [DurabilityString.ToLower()]);
                changed |= CheckboxWithIcons("MuMeIntensiveLastResort", ref s.MuMeIntensiveLastResort, "当 [s!MuscleMemory] 剩余 1 步且非 [c!Centered] 时，使用 [s!IntensiveSynthesis]（必要时通过 [s!HeartAndSoul]）");
                HelpMarkerWithIcons("如果最后一步是 [c!Centered]，仍会使用 [s!RapidSynthesis]。");

                ImGui.Dummy(new Vector2(0, 5f));
                DrawIconText("仅当 [s!MuscleMemory] 剩余步骤数超过以下值时使用这些技能：");
                ImGuiComponents.HelpMarker($"求解器仍只会在合适的 {ConditionString.ToLower()} 下使用这些技能。");
                // these have a minimum of 1 to avoid using a buff on the final turn of MuMe
                ImGui.PushItemWidth(250);
                SliderIntWithIcons("MuMeMinStepsForManip", ref s.MuMeMinStepsForManip, 1, 5, "[s!Manipulation]");
                ImGui.PushItemWidth(250);
                SliderIntWithIcons("MuMeMinStepsForVene", ref s.MuMeMinStepsForVene, 1, 5, "[s!Veneration]");
                ImGui.Unindent();
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        return changed;
    }

    public bool DrawPreQualitySettings(ExpertSolverSettings s)
    {
        bool changed = false;
        try
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"这些设置适用于起手之后、达到最大 {Buffs.InnerQuiet.NameOfBuff()} 层数之前。");

            // Pre-quality dura/CP settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"常规");
            ImGui.Indent();

            changed |= CheckboxWithIcons("MidBaitPliantWithObservePreQuality", ref s.MidBaitPliantWithObservePreQuality, "当 {0} 告急时，使用 [s!Observe] 尝试触发有利的 {1} 以使用 [s!Manipulation]", [DurabilityString.ToLower(), ConditionString.ToLower()]);
            HelpMarkerWithIcons(["等待 [c!Pliant]（以及 [c!Primed]，如已启用相应选项）。", "如禁用，[s!Manipulation] 将立即使用，无论 {0} 如何。"], [ConditionString.ToLower()]);

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("在 [c!Primed] 时使用 [s!Manipulation]：");
            ImGui.PushItemWidth(400);
            if (ImGui.BeginCombo("##PQPrimedManip", s.GetPQPrimedManipSet(s.PQPrimedManip)))
            {
                foreach (PQPrimedManipSet x in Enum.GetValues<PQPrimedManipSet>())
                {
                    if (ImGui.Selectable(s.GetPQPrimedManipSet(x)))
                    {
                        s.PQPrimedManip = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("使用 [s!WasteNot]：");
            ImGui.PushItemWidth(300);
            if (ImGui.BeginCombo("##PQWasteNot", s.GetPQWasteNotSet(s.PQWasteNot)))
            {
                foreach (PQWasteNotSet x in Enum.GetValues<PQWasteNotSet>())
                {
                    if (ImGui.Selectable(s.GetPQWasteNotSet(x)))
                    {
                        s.PQWasteNot = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            if (s.PQWasteNot != PQWasteNotSet.Never)
            {
                ImGui.Indent();
                ImGui.PushItemWidth(250);
                changed |= SliderIntWithIcons("PQWasteNotMaxIQ", ref s.PQWasteNotMaxIQ, 0, 9, "仅在 <= X 层 {0} 时", [Buffs.InnerQuiet.NameOfBuff()]);
                ImGui.Unindent();
            }

            ImGui.Unindent();

            // Pre-quality progress settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"{ProgressString}");
            ImGui.Indent();
            DrawIconText("确保完成 {0}：", [ProgressString.ToLower()]);
            HelpMarkerWithIcons(["通常情况下，求解器会根据情况利用每个 {0} 推进 {1} 或 {2}。", "然而，在最大 {3} 层且使用 [s!Innovation] 时，{1} 的机会较少。", "使用此设置在需要时确保 {1} 完成。"], [ConditionString.ToLower(), ProgressString.ToLower(), QualityString.ToLower(), Buffs.InnerQuiet.NameOfBuff()]);
            ImGui.PushItemWidth(400);
            if (ImGui.BeginCombo("##whenToForceProgressSetting", s.GetWhenToForceProgressSettingName(s.WhenToForceProgress)))
            {
                foreach (WhenToForceProgressSetting x in Enum.GetValues<WhenToForceProgressSetting>())
                {
                    if (ImGui.Selectable(s.GetWhenToForceProgressSettingName(x)))
                    {
                        s.WhenToForceProgress = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            if (s.WhenToForceProgress != WhenToForceProgressSetting.WhenToForceProgressNever)
            {
                ImGui.Dummy(new Vector2(0, 3f));
                ImGui.Indent();
                changed |= CheckboxWithIcons("ForceProgressVene", ref s.ForceProgressVene, "在需要大幅补足 {0} 时使用 [s!Veneration]", [ProgressString.ToLower()]);
                DrawIconText("当需要完成 {1} 时，使用 [s!Observe] 此次数以优化 [s!RapidSynthesis] 的 {0}：", [ConditionString.ToLower(), ProgressString.ToLower()]);
                HelpMarkerWithIcons("寻找 [c!Centered]、[c!Sturdy]/[c!Robust] 或 [c!Malleable]。");
                ImGui.PushItemWidth(250);
                changed |= ImGui.SliderInt("(-1 为无限制, 0 为禁用)##ForceProgressMaxBait", ref s.ForceProgressMaxBait, -1, 10);
                ImGui.Unindent();
            }

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("当 {0} 开始不足且需要使用 [s!RapidSynthesis] 时：", [DurabilityString.ToLower()]);
            ImGui.PushItemWidth(400);
            if (ImGui.BeginCombo("##midKeepHighDuraSetting", s.GetMidKeepHighDuraSettingName(s.MidKeepHighDura)))
            {
                foreach (MidKeepHighDuraSetting x in Enum.GetValues<MidKeepHighDuraSetting>())
                {
                    if (ImGui.Selectable(s.GetMidKeepHighDuraSettingName(x)))
                    {
                        s.MidKeepHighDura = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("当 [c!Good] 且仍在处理 {0} 时：", [ProgressString.ToLower()]);
            HelpMarkerWithIcons("如禁用，即使仍有 {0} 剩余，[c!Good] 也会用于 [s!PreciseTouch] 或 [s!TricksOfTrade]（如其他设置允许）。", [ProgressString.ToLower()]);
            if (ImGui.BeginCombo("##midAllowIntensiveSetting", s.GetMidAllowIntensiveSettingName(s.MidAllowIntensive)))
            {
                foreach (MidAllowIntensiveSetting x in Enum.GetValues<MidAllowIntensiveSetting>())
                {
                    if (ImGui.Selectable(s.GetMidAllowIntensiveSettingName(x)))
                    {
                        s.MidAllowIntensive = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Dummy(new Vector2(0, 5f));
            changed |= CheckboxWithIcons("MidAllowVenerationGoodOmen", ref s.MidAllowVenerationGoodOmen, "当 [c!GoodOmen] 且 {0} 大幅不足时使用 [s!Veneration]", [ProgressString.ToLower()]);
            HelpMarkerWithIcons("具体而言，当即将到来的 [c!Good] 步骤的 [s!IntensiveSynthesis] 在没有 [s!Veneration] 的情况下无法最大化 {0} 时。", [ProgressString.ToLower()]);
            ImGui.Unindent();

            // Pre-quality Inner Quiet settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"{Buffs.InnerQuiet.NameOfBuff()}");
            ImGui.Indent();
            DrawIconText("当 [c!Good] 时，使用 [s!PreciseTouch]：");
            ImGui.PushItemWidth(300);
            if (ImGui.BeginCombo("##PQGoodPrecise", s.GetPQGoodPreciseSet(s.PQGoodPrecise)))
            {
                foreach (PQGoodPreciseSet x in Enum.GetValues<PQGoodPreciseSet>())
                {
                    if (ImGui.Selectable(s.GetPQGoodPreciseSet(x)))
                    {
                        s.PQGoodPrecise = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("使用 [s!HeartAndSoul] 强制 [s!PreciseTouch]：");
            ImGui.Indent();
            changed |= CheckboxWithIcons("MidAllowSturdyPreсise", ref s.MidAllowSturdyPreсise, "当 [c!Sturdy]/[c!Robust] 时");
            ImGui.PushItemWidth(250);
            changed |= SliderIntWithIcons("MidMinIQForHSPrecise", ref s.MidMinIQForHSPrecise, 0, 10, "当 {0} 达到此层数时（10 为禁用）", [Buffs.InnerQuiet.NameOfBuff()]);
            ImGui.Unindent();

            ImGui.Dummy(new Vector2(0, 5f));
            changed |= CheckboxWithIcons("PQAdvancedCombo", ref s.PQAdvancedCombo, "优先使用 [s!Observe] → [s!AdvancedTouch] 而非 [s!PrudentTouch]");

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("使用 [s!HastyTouch] 和 [s!DaringTouch]：");
            ImGui.Indent();
            changed |= CheckboxWithIcons("MidAllowCenteredHasty", ref s.MidAllowCenteredHasty, "当 [c!Centered] 时（85% 成功率, 10 {0}）", [DurabilityString.ToLower()]);
            changed |= CheckboxWithIcons("MidAllowSturdyHasty", ref s.MidAllowSturdyHasty, "当 [c!Sturdy]/[c!Robust] 时（60% 成功率, 5 {0}）", [DurabilityString.ToLower()]);
            ImGui.Unindent();

            ImGui.Unindent();

            // Pre-quality quality settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"{QualityString}");
            ImGui.Indent();

            DrawIconText("当拥有 >= X 层 {0} 时使用 [s!Innovation]（设为 10 以禁用）：", [Buffs.InnerQuiet.NameOfBuff()]);
            ImGui.Indent();
            ImGui.PushItemWidth(250);
            changed |= SliderIntWithIcons("PQPrimedInnoIQ", ref s.PQPrimedInnoIQ, 0, 10, "在 [c!Primed] 时");
            ImGui.PushItemWidth(250);
            changed |= SliderIntWithIcons("PQOtherInnoIQ", ref s.PQOtherInnoIQ, 0, 10, "在任意其他 {0} 时", [ConditionString.ToLower()]);
            ImGui.Unindent();

            ImGui.Unindent();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        return changed;
    }

    public bool DrawQualitySettings(ExpertSolverSettings s)
    {
        bool changed = false;
        try
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"此设置在达到最大 {Buffs.InnerQuiet.NameOfBuff()} 层数后生效。");

            // Mid-quality dura/CP settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"常规");
            ImGui.Indent();
            changed |= CheckboxWithIcons("MidBaitPliantWithObserveAfterIQ", ref s.MidBaitPliantWithObserveAfterIQ, "当 {0} 非常低时，使用 [s!Observe] 触发有利的 {1} 以恢复 {0}", [DurabilityString.ToLower(), ConditionString.ToLower()]);
            HelpMarkerWithIcons(["刷取 [c!Pliant]（也可能刷出 [c!Primed]）。", "如果禁用，将立即使用恢复或需要 0 {0} 的行动，无论 {1} 如何。"], [DurabilityString.ToLower(), ConditionString.ToLower()]);
            changed |= CheckboxWithIcons("MidPrimedManipAfterIQ", ref s.MidPrimedManipAfterIQ, "在 [c!Primed] 时使用 [s!Manipulation]，前提是剩余 CP 足够有效利用恢复的 {0}", [DurabilityString.ToLower()]);
            changed |= CheckboxWithIcons("MidObserveGoodOmenForTricks", ref s.MidObserveGoodOmenForTricks, "在 [c!GoodOmen] 时，无增益状态下优先使用 [s!Observe] → [s!TricksOfTrade]");
            HelpMarkerWithIcons(["如果禁用，求解器会优先使用增益技能，将 [c!Good] 回合用于 {0} 或 {1}。", "启用此选项通常效率更高。"], [ProgressString.ToLower(), QualityString.ToLower()]);
            ImGui.Unindent();

            // Mid-quality progress settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"{ProgressString}");
            ImGui.Indent();
            changed |= CheckboxWithIcons("MidAllowVenerationAfterIQ", ref s.MidAllowVenerationAfterIQ, "在 {0} 大幅不足时使用 [s!Veneration]", [ProgressString.ToLower()]);
            HelpMarkerWithIcons(["特指当即使到了制作后期，不使用 [s!Veneration] 时单次 [s!IntensiveSynthesis] 也无法完成制作的情况。", "如果启用了「优先 {0}」设置，则以此为准。"], [ProgressString.ToLower()]);
            ImGui.Unindent();

            // Mid-quality action settings
            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.TextWrapped($"{QualityString}");
            ImGui.Indent();

            DrawIconText("使用 [s!PreparatoryTouch]：");
            ImGui.Indent();
            changed |= CheckboxWithIcons("MidAllowGoodPrep", ref s.MidAllowGoodPrep, "在 [c!Good] + [s!Innovation] + [s!GreatStrides] 下");
            HelpMarkerWithIcons("即使品质提升较大，效率仍低于 [s!PreciseTouch]。");
            changed |= CheckboxWithIcons("MidAllowSturdyPrep", ref s.MidAllowSturdyPrep, "在 [c!Sturdy]/[c!Robust] + [s!Innovation] 下");
            ImGui.Unindent();

            ImGui.Dummy(new Vector2(0, 5f));
            changed |= CheckboxWithIcons("MidGSBeforeInno", ref s.MidGSBeforeInno, "在非收尾的 {0} 连招前使用 [s!GreatStrides]", [QualityString.ToLower()]);
            HelpMarkerWithIcons(["例如：[s!Innovation] → [s!Observe] → [s!AdvancedTouch]。", "启用此选项会消耗更多 CP 但节省 {0}，可能有助于避免使用一次昂贵的 {0} 相关技能。"], [DurabilityString.ToLower()]);

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("当 [c!Good] 且只有 [s!GreatStrides] 生效时：");
            HelpMarkerWithIcons(["「免费」[s!PreparatoryTouch] 指 [s!TrainedPerfection]，可在 Pre-{0} 设置中启用。", "保留 [s!QuickInnovation] 用于收尾阶段紧急使用 [s!ByregotsBlessing] 效率最高，但可能并非必需。"], [QualityString]);
            ImGui.PushItemWidth(350);
            if (ImGui.BeginCombo("##midAllowQuickInnoGoodSetting", s.GetQQuickInnoGoodSet(s.QQuickInnoGood)))
            {
                foreach (QQuickInnoGoodSet x in Enum.GetValues<QQuickInnoGoodSet>())
                {
                    if (ImGui.Selectable(s.GetQQuickInnoGoodSet(x)))
                    {
                        s.QQuickInnoGood = x;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.Unindent();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        return changed;
    }

    public bool DrawFinisherSettings(ExpertSolverSettings s)
    {
        bool changed = false;
        try
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"此设置在接近最高 {QualityString.ToLower()} 或其他选项用尽时生效。");

            ImGui.Dummy(new Vector2(0, 5f));
            DrawIconText("使用 [s!CarefulObservation] 尝试触发 [c!Good]：");
            ImGui.Indent();
            changed |= CheckboxWithIcons("FinisherBaitGoodByregot", ref s.FinisherBaitGoodByregot, "为 [s!ByregotsBlessing] 充当临时的 [s!GreatStrides]");
            HelpMarkerWithIcons("当 [s!GreatStrides] + [s!ByregotsBlessing] 足以完成，但没有足够 CP 使用 [s!GreatStrides] 或常规 [s!Observe] 时触发。");
            changed |= CheckboxWithIcons("EmergencyCPBaitGood", ref s.EmergencyCPBaitGood, "用于 CP 极低时的 [s!TricksOfTrade]");
            HelpMarkerWithIcons("当完全没有其他选项，且即使 [s!ByregotsBlessing] 也不足以达到 {0} 时触发。", [QualityString.ToLower()]);
            ImGui.Unindent();

            ImGui.Dummy(new Vector2(0, 5f));
            changed |= CheckboxWithIcons("FinisherUseQuickInno", ref s.FinisherUseQuickInno, "在 CP 不足时使用 [s!QuickInnovation] 完成制作");
            HelpMarkerWithIcons("当没有足够 CP 使用 [s!Innovation] 和/或 [s!GreatStrides]，但 [s!QuickInnovation] 足以达到 {0} 目标时。", [QualityString.ToLower()]);
            changed |= CheckboxWithIcons("RapidSynthYoloAllowed", ref s.RapidSynthYoloAllowed, "允许在无其他选项时使用 [s!RapidSynthesis] 完成制作");
            ImGuiComponents.HelpMarker($"如果禁用，求解器将什么都不做，可能会中断挂机专家制作。通常可以安全启用，因为只有在没有 CP 或 {DurabilityString.ToLower()} 剩余时才会触发。");
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        return changed;
    }

    public bool DrawAllSettings(ExpertSolverSettings s, bool startOpen)
    {
        bool changed = false;

        ImGuiTreeNodeFlags flags = startOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        if (ImGui.CollapsingHeader("常规", flags))
        {
            ImGui.Dummy(new Vector2(0, 5f));
            changed |= DrawGeneralSettings(s);
            ImGui.Dummy(new Vector2(0, 5f));
        }
        if (ImGui.CollapsingHeader("起手", flags))
        {
            ImGui.Dummy(new Vector2(0, 5f));
            changed |= DrawOpenerSettings(s);
            ImGui.Dummy(new Vector2(0, 5f));
        }

        if (ImGui.CollapsingHeader($"主要循环 - Pre-{QualityString} 阶段", flags))
        {
            ImGui.Dummy(new Vector2(0, 5f));
            changed |= DrawPreQualitySettings(s);
            ImGui.Dummy(new Vector2(0, 5f));
        }

        if (ImGui.CollapsingHeader($"主要循环 - {QualityString} 阶段", flags))
        {
            ImGui.Dummy(new Vector2(0, 5f));
            changed |= DrawQualitySettings(s);
            ImGui.Dummy(new Vector2(0, 5f));
        }

        if (ImGui.CollapsingHeader($"收尾", flags))
        {
            ImGui.Dummy(new Vector2(0, 5f));
            changed |= DrawFinisherSettings(s);
        }

#if DEBUG
        ImGui.Dummy(new Vector2(0, 5f));
        changed |= ImGui.Checkbox("调试：仅观察###debugObserve", ref s.DebugObserveOnly);
        HelpMarkerWithIcons("注意：只会连续使用 [s!Observe] 和 [s!TricksOfTrade] 以收集条件数据。");
        changed |= ImGui.Checkbox("调试：仅改革###debugInnovate", ref s.DebugInnovateOnly);
        HelpMarkerWithIcons("注意：只会连续使用 [s!Innovation] 以收集条件数据。比 [s!Observe] 更快。");
#endif

        return changed;
    }

    public bool DrawGlobalSettings(ExpertSolverSettings s)
    {
        bool changed = false;
        try
        {
            ImGui.TextWrapped($"专家配方求解器并非标准求解器的替代品。此求解器仅用于专家配方。");
            if (expertIcon != null)
            {
                ImGui.TextWrapped($"此求解器仅适用于制作笔记中带有");
                ImGui.SameLine();
                ImGui.Image(expertIcon.Handle, expertIcon.Size, new Vector2(0, 0), new Vector2(1, 1), new Vector4(0.94f, 0.57f, 0f, 1f));
                ImGui.SameLine();
                ImGui.TextWrapped($"图标的配方。");
            }

            ImGui.Dummy(new Vector2(0, 5f));
            ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"重要提示：这些设置已为最佳性能选定。更改它们可能导致求解器性能显著下降。请自行承担风险。");
            if (ImGui.Checkbox("我了解，让我进入", ref P.Config.AcknowledgeExpertSettings))
            {
                P.Config.Save();
            }

            ImGui.Dummy(new Vector2(0, 5f));

            if (P.Config.AcknowledgeExpertSettings)
            {
                ImGui.Indent();
                changed |= DrawAllSettings(s, false);
                ImGui.Unindent();

                ImGui.Indent();
                ImGui.TextWrapped($"专家配置文件");
                ImGui.Indent();
                changed |= ImGui.Checkbox("使用专家求解器配置文件", ref s.EnableExpertProfiles);
                ImGuiComponents.HelpMarker("配置文件让你可以为不同配方设置不同的专家求解器设置。此功能面向高级用户——专家求解器在大多数情况下应能「开箱即用」。");

                if (s.EnableExpertProfiles)
                {
                    if (IconButtons.IconTextButton(FontAwesomeIcon.ExternalLinkAlt, "创建/编辑专家求解器配置文件"))
                    {
                        P.PluginUi.OpenWindow = OpenWindow.ExpertProfiles;
                    }
                }
                ImGui.Unindent();

                ImGui.Dummy(new Vector2(0, 5f));
                if (ImGuiEx.ButtonCtrl("将专家求解器设置重置为默认值"))
                {
                    P.Config.ExpertSolverConfig = new();
                    changed |= true;
                }

                ImGui.Dummy(new Vector2(0, 10f));
                ImGui.Unindent();
            }

            return changed;
        }
        catch { }
        return changed;
    }

    /// <summary>
    /// Custom HelpMarker that supports skill icons and colorful condition dots.
    /// </summary>
    /// <param name="str">The helpText string with custom markup.</param>
    /// <param name="args">Substitution strings for the helpText string.</param>
    public void HelpMarkerWithIcons(string str, object[]? args = null) => HelpMarkerWithIcons([str], args);

    /// <summary>
    /// Custom HelpMarker that supports skill icons and colorful condition dots.
    /// </summary>
    /// <param name="lines">An Array of helpText strings with custom markup.</param>
    /// <param name="args">Substitution strings for each helpText string.</param>
    public void HelpMarkerWithIcons(string[] lines, object[]? args = null)
    {
        if (args == null)
            args = [];

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
            ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                foreach (string str in lines)
                    DrawIconText(str, args);
            }
        }
    }

    /// <summary>
    /// Custom ImGui.Checkbox that supports skill icons and colorful condition dots in its label.
    /// </summary>
    /// <param name="ID">A unique ID for the checkbox.</param>
    /// <param name="val">The boolean setting to attach to the checkbox.</param>
    /// <param name="str">The string with custom markup for the checkbox's label.</param>
    /// <param name="args">Substitution strings for the checkbox's label.</param>
    public bool CheckboxWithIcons(string ID, ref bool val, string str, object[]? args = null)
    {
        if (args == null)
            args = [];

        bool changed = false;

        ImGui.PushID(ID);
        changed |= ImGui.Checkbox($"##{ID}", ref val);
        ImGui.SameLine(0.0f, 4.0f);

        DrawIconText(str, args);

        ImGui.PopID();
        return changed;
    }

    /// <summary>
    /// Custom ImGui.SliderInt that supports skill icons and colorful condition dots in its label.
    /// </summary>
    /// <param name="ID">A unique ID for the slider.</param>
    /// <param name="val">The int setting to attach to the slider.</param>
    /// <param name="min">Minimum value for the slider.</param>
    /// <param name="max">Maximum value for the slider.</param>
    /// <param name="str">The string with custom markup for the slider's label.</param>
    /// <param name="args">Substitution strings for the slider's label.</param>
    public bool SliderIntWithIcons(string ID, ref int val, int min, int max, string str, object[]? args = null)
    {
        if (args == null)
            args = [];

        bool changed = false;

        ImGui.PushID(ID);
        changed |= ImGui.SliderInt($"##{ID}", ref val, min, max);
        ImGui.SameLine(0.0f, 4.0f);

        DrawIconText(str, args);

        ImGui.PopID();
        return changed;
    }

    /// <summary>
    /// Draws Text, colorized Text, and Image elements from a string with custom markup.
    /// </summary>
    /// <param name="str">The string with custom markup to be rendered.</param>
    /// <param name="args">Substitution strings for the primary string.</param>
    /// <param name="color">The color to be used for standard strings.</param>
    public void DrawIconText(string str, object[]? args = null, Vector4? color = null)
    {
        if (args == null)
            args = [];
        if (color == null)
            color = ImGuiColors.DalamudWhite;

        SkillIconID skillIcon;
        Condition condition;
        Skills skill;
        string formatStr = String.Format(str, args);
        string[] parts = Regex.Split(formatStr, @"(\[.+?\])");
        for (int i = 0; i < parts.Length; i++)
        {
            float spacing = 2.0f;
            string part = parts[i];
            if (part.StartsWith("[c!"))
            {
                // Render a condition dot (●) with the appropriate color and the localized condition name
                Vector4 condColor;
                string c = part[3..^1];
                if (ConditionColors.TryGetValue(c, out condColor))
                {
                    ImGuiEx.Text(condColor, "● ");
                    ImGui.SameLine(0.0f, 0.0f);
                }
                if (Enum.TryParse(c, out condition))
                    ImGuiEx.Text(color, condition.ToLocalizedString());
                spacing = 0.0f;
            }
            else if (part.StartsWith("[s!"))
            {
                // Render a skill icon and the localized skill name
                string s = part[3..^1];
                if (Enum.TryParse(s, out skillIcon))
                {
                    uint iconID = (uint)skillIcon;
                    ImGui.Image(P.Icons.TryLoadIconAsync(iconID).Result.Handle, new Vector2(20f, 20f));
                    ImGui.SameLine(0.0f, 4.0f);
                }
                if (Enum.TryParse(s, out skill))
                {
                    ImGuiEx.Text(color, skill.NameOfAction());
                    spacing = 0.0f;
                }
            }
            else if (part == "[ex]")
            {
                // Render the expert crafting icon
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4.0f);
                ImGui.Image(expertIcon.Handle, new Vector2(36f, 18f), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0.94f, 0.57f, 0f, 1f));
                spacing = 4.0f;
            }
            else
            {
                // Plain text
                ImGuiEx.Text(color, part);
            }

            if (i < parts.Length - 1)
                ImGui.SameLine(0.0f, spacing);
        }
    }
}
