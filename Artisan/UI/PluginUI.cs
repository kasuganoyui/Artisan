using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.FCWorkshops;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Artisan.UI.Tables;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.WindowsFormsReflector;
using Lumina.Excel.Sheets;
using PunishLib.ImGuiMethods;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace Artisan.UI
{
    unsafe internal class PluginUI : Window
    {
        public event EventHandler<bool>? CraftingWindowStateChanged;
        public ExpertSolverSettingsUI ExpertSettingsUI = new();
        public RaphaelCacheUI RaphaelCacheUI = new();

        private bool visible = false;
        public OpenWindow OpenWindow { get; set; }

        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private bool craftingVisible = false;
        public bool CraftingVisible
        {
            get { return this.craftingVisible; }
            set { if (this.craftingVisible != value) CraftingWindowStateChanged?.Invoke(this, value); this.craftingVisible = value; }
        }

        public PluginUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###Artisan")
        {
            this.RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new(250, 100),
                MaximumSize = new(9999, 9999)
            };
            this.TitleBarButtons.Add(new()
            {
                Icon = FontAwesomeIcon.Cog,
                ShowTooltip = () => ImGuiEx.SetTooltip("打开设置"),
                Click = (x) => P.PluginUi.IsOpen = true,
            });
            P.ws.AddWindow(this);
        }

        public override void PreDraw()
        {
            if (!P.Config.DisableTheme)
            {
                P.Style.Push();
                P.StylePushed = true;
            }

        }

        public override void PostDraw()
        {
            if (P.StylePushed)
            {
                P.Style.Pop();
                P.StylePushed = false;
            }
        }

        public void Dispose()
        {

        }

        public override void Draw()
        {
            try
            {
                if (DalamudInfo.IsOnStaging())
                {
                    var scale = ImGui.GetIO().FontGlobalScale;
                    ImGui.GetIO().FontGlobalScale = scale * 1.5f;
                    using (var f = ImRaii.PushFont(ImGui.GetFont()))
                    {
                        ImGuiEx.TextWrapped($"请注意，你正在使用 Dalamud staging 版本，你遇到的任何问题都可能是 Dalamud 测试版特有的，与 Artisan 无关。本插件并非为 staging 环境开发，除非该问题在 Dalamud 正式版中也存在，否则不提供修复。");
                        ImGui.Separator();

                        ImGui.Spacing();
                        ImGui.GetIO().FontGlobalScale = scale;
                    }

                }
                var region = ImGui.GetContentRegionAvail();
                var itemSpacing = ImGui.GetStyle().ItemSpacing;

                var topLeftSideHeight = region.Y;

                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f.Scale(), 0));
                try
                {
                    ShowEnduranceMessage();

                    using (var table = ImRaii.Table($"ArtisanTableContainer", 2, ImGuiTableFlags.Resizable))
                    {
                        if (!table)
                            return;

                        ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);

                        ImGui.TableNextColumn();

                        var regionSize = ImGui.GetContentRegionAvail();

                        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                        using (var leftChild = ImRaii.Child($"###ArtisanLeftSide", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                        {
                            var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan-icon.png");

                            if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                            {
                                ImGuiEx.LineCentered("###ArtisanLogo", () =>
                                {
                                    ImGui.Image(logo.Handle, new(125f.Scale(), 125f.Scale()));
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text($"真棒！你是第69个发现这个秘密的人。");
                                        ImGui.EndTooltip();
                                    }
                                });

                            }
                            ImGui.Spacing();
                            ImGui.Separator();

                            if (ImGui.Selectable("概述", OpenWindow == OpenWindow.Overview))
                            {
                                OpenWindow = OpenWindow.Overview;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("设置", OpenWindow == OpenWindow.Main))
                            {
                                OpenWindow = OpenWindow.Main;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("耐力模式", OpenWindow == OpenWindow.Endurance))
                            {
                                OpenWindow = OpenWindow.Endurance;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("宏", OpenWindow == OpenWindow.Macro))
                            {
                                OpenWindow = OpenWindow.Macro;
                            }
                            if (P.Config.ExpertSolverConfig.EnableExpertProfiles)
                            {
                                ImGui.Spacing();
                                if (ImGui.Selectable("专家配置文件", OpenWindow == OpenWindow.ExpertProfiles))
                                {
                                    OpenWindow = OpenWindow.ExpertProfiles;
                                }
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("Raphael 缓存", OpenWindow == OpenWindow.RaphaelCache))
                            {
                                OpenWindow = OpenWindow.RaphaelCache;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("配方分配器", OpenWindow == OpenWindow.Assigner))
                            {
                                OpenWindow = OpenWindow.Assigner;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("制作列表", OpenWindow == OpenWindow.Lists))
                            {
                                OpenWindow = OpenWindow.Lists;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("列表构建器", OpenWindow == OpenWindow.SpecialList))
                            {
                                OpenWindow = OpenWindow.SpecialList;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("部队工坊", OpenWindow == OpenWindow.FCWorkshop))
                            {
                                OpenWindow = OpenWindow.FCWorkshop;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("模拟器", OpenWindow == OpenWindow.Simulator))
                            {
                                OpenWindow = OpenWindow.Simulator;
                            }
                            ImGui.Spacing();
                            if (ImGui.Selectable("关于", OpenWindow == OpenWindow.About))
                            {
                                OpenWindow = OpenWindow.About;
                            }


#if DEBUG
                            drawDebugTab();
#else
                        if(GenericHelpers.IsKeyPressed(Keys.LControlKey) && GenericHelpers.IsKeyPressed(Keys.LShiftKey)) drawDebugTab();
#endif
                            void drawDebugTab()
                            {
                                ImGui.Spacing();
                                if (ImGui.Selectable("DEBUG", OpenWindow == OpenWindow.Debug))
                                {
                                    OpenWindow = OpenWindow.Debug;
                                }
                                ImGui.Spacing();
                            }


                        }

                        ImGui.PopStyleVar();
                        ImGui.TableNextColumn();
                        using (var rightChild = ImRaii.Child($"###ArtisanRightSide", Vector2.Zero, false))
                        {
                            switch (OpenWindow)
                            {
                                case OpenWindow.Main:
                                    DrawMainWindow();
                                    break;
                                case OpenWindow.Endurance:
                                    Endurance.Draw();
                                    break;
                                case OpenWindow.Lists:
                                    CraftingListUI.Draw();
                                    break;
                                case OpenWindow.About:
                                    AboutTab.Draw("Artisan");
                                    break;
                                case OpenWindow.Debug:
                                    DebugTab.Draw();
                                    break;
                                case OpenWindow.Macro:
                                    MacroUI.Draw();
                                    break;
                                case OpenWindow.ExpertProfiles:
                                    ExpertProfilesUI.Draw();
                                    break;
                                case OpenWindow.RaphaelCache:
                                    RaphaelCacheUI.Draw();
                                    break;
                                case OpenWindow.Assigner:
                                    AssignerUI.Draw();
                                    break;
                                case OpenWindow.FCWorkshop:
                                    FCWorkshopUI.Draw();
                                    break;
                                case OpenWindow.SpecialList:
                                    SpecialLists.Draw();
                                    break;
                                case OpenWindow.Overview:
                                    DrawOverview();
                                    break;
                                case OpenWindow.Simulator:
                                    SimulatorUI.Draw();
                                    break;
                                case OpenWindow.None:
                                    break;
                                default:
                                    break;
                            }
                            ;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                ImGui.PopStyleVar();
            }
            catch { }
        }

        private void DrawOverview()
        {
            var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
            {
                ImGuiEx.LineCentered("###ArtisanTextLogo", () =>
                {
                    ImGui.Image(logo.Handle, new Vector2(logo.Width, 100f.Scale()));
                });
            }

            ImGuiEx.LineCentered("###ArtisanOverview", () =>
            {
                ImGuiEx.TextUnderlined("Artisan - 概述");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"首先感谢你下载我的这个小小制作插件。自 2022 年 6 月以来，我一直在持续开发 Artisan，它是我插件的巅峰之作。");
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"在开始使用 Artisan 之前，我们先了解一下插件的工作原理。掌握几个关键要点后，Artisan 就很容易上手了。");

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanModes", () =>
            {
                ImGuiEx.TextUnderlined("制作模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan 提供了一种\"自动执行模式\"，它会直接获取推荐的动作并替你执行。" +
                                " 默认情况下，它会以游戏允许的最快速度执行，比普通宏更快。" +
                                " 这样做并没有绕过任何游戏限制，不过你也可以根据需要设置延迟。" +
                                " 启用此功能与 Artisan 默认使用的建议生成过程无关。");

            var automode = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/AutoMode.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(automode, out var example))
            {
                ImGuiEx.LineCentered("###AutoModeExample", () =>
                {
                    ImGui.Image(example.Handle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"如果你没有启用自动模式，你还可以使用另外两种模式：\"半手动模式\"和\"全手动模式\"。" +
                                $" \"半手动模式\"会在你开始制作时弹出一个小窗口。");

            var craftWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/ThemeCraftingWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(craftWindowExample, out example))
            {
                ImGuiEx.LineCentered("###CraftWindowExample", () =>
                {
                    ImGui.Image(example.Handle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"点击\"执行推荐动作\"按钮，就是让插件执行它推荐的动作。" +
                $" 这被称为半手动模式，因为你仍需点击每个动作，但不用在热键栏上找到它们。" +
                $" \"全手动模式\"则像平常一样按热键栏上的按钮。" +
                $" 默认情况下，Artisan 会高亮显示热键栏上对应的动作来帮助你。（可在设置中禁用）");

            var outlineExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/OutlineExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(outlineExample, out example))
            {
                ImGuiEx.LineCentered("###OutlineExample", () =>
                {
                    ImGui.Image(example.Handle, new Vector2(example.Width, example.Height));
                });
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanSuggestions", () =>
            {
                ImGuiEx.TextUnderlined("求解器/宏");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan 默认会为你的下一个制作步骤提供建议。不过这个求解器并不完美，绝对不是合适装备的替代品。" +
                $"你不需要做任何事来启用此行为——只要启用 Artisan 即可。" +
                $"\r\n\r\n" +
                $"如果你要制作一个默认求解器无法完成的高难配方，Artisan 允许你编写宏，用宏来替代默认求解器的建议。" +
                $" Artisan 的宏不受长度限制，可以以游戏允许的最快速度执行，还支持一些可随时调整的附加选项。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"点击此处前往宏菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Macro;
            }
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"创建宏后，你需要将其分配给一个配方。使用配方窗口的下拉菜单可以轻松完成。默认情况下，该菜单位于游戏内制作笔记窗口的右上角，但可以在设置中取消附着。");


            var recipeWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/RecipeWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(recipeWindowExample, out example))
            {
                ImGuiEx.LineCentered("###RecipeWindowExample", () =>
                {
                    ImGui.Image(example.Handle, new Vector2(example.Width, example.Height));
                });
            }


            ImGuiEx.TextWrapped($"从下拉框中选择已创建的宏。当你去制作这个物品时，技能建议将被你的宏的内容所取代。");

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Endurance", () =>
            {
                ImGuiEx.TextUnderlined("耐力模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan 有一种名为\"耐力模式\"的模式，本质上就是更高级的\"自动重复模式\"，会持续尝试为你制作同一件物品。" +
                $"耐力模式通过从游戏内的制作笔记中选择配方并启用该功能来工作。" +
                $"你的角色随后会尝试持续制作该物品，次数取决于你拥有的材料数量。" +
                $"\r\n\r\n" +
                $"其他功能应该不言自明，因为耐力模式还可以管理你在制作之间对食物、药水、手册、修理和魔晶石提取的使用。" +
                $"修理功能仅支持使用暗物质进行修理，不支持修理 NPC。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"点击此处进入耐力菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Endurance;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Lists", () =>
            {
                ImGuiEx.TextUnderlined("制作清单");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan 还能够创建物品清单，并让它自动逐一制作清单中的每件物品。" +
                $"制作清单拥有许多强大的工具，可以简化从材料到成品的整个过程。" +
                $"它还支持导入和导出到 Teamcraft。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"点击此处进入制作清单菜单。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Lists;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Questions", () =>
            {
                ImGuiEx.TextUnderlined("有问题？");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"如果你对这里未列出的内容有疑问，可以在我们的");
            ImGui.SameLine(ImGui.GetCursorPosX(), 1.5f);
            ImGuiEx.TextUnderlined($"Discord 服务器中提问。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://discord.gg/Zzrcc8kmvy");
                }
            }

            ImGuiEx.TextWrapped($"你也可以在我们的");
            ImGui.SameLine(ImGui.GetCursorPosX(), 2f);
            ImGuiEx.TextUnderlined($"GitHub 页面上提交问题。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://github.com/PunishXIV/Artisan");
                }
            }

        }

        public static void DrawMainWindow()
        {
            ImGui.TextWrapped($"你可以在这里更改 Artisan 使用的一些设置。其中部分设置也可以在制作过程中切换。");
            ImGui.TextWrapped($"为了使用 Artisan 的手动推荐高亮功能，请将你已解锁的每个制作技能放置在可见的热键栏上。");
            bool autoEnabled = P.Config.AutoMode;
            int maxQuality = P.Config.MaxPercentage;
            bool useSpecialist = P.Config.UseSpecialist;
            //bool showEHQ = P.Config.ShowEHQ;
            //bool useSimulated = P.Config.UseSimulatedStartingQuality;

            bool changed = false;

            ImGui.Dummy(new Vector2(0, 5f));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5f));

            if (ImGui.CollapsingHeader("通用设置"))
            {
                ImGui.Indent();

                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"自动动作模式");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                if (ImGui.Checkbox("自动动作执行模式", ref autoEnabled))
                {
                    P.Config.AutoMode = autoEnabled;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"自动使用每个推荐的动作，而不是仅仅高亮显示它们。");

                if (autoEnabled)
                {
                    ImGui.Indent();
                    changed |= ImGui.Checkbox($"模拟宏延迟", ref P.Config.ReplicateMacroDelay);
                    ImGuiComponents.HelpMarker("此设置会延迟每个自动动作，就像你在使用带 <wait.2> 或 <wait.3> 的游戏内宏一样。禁用后，你可以手动设置 Artisan 在每个动作后等待的时间。");

                    if (!P.Config.ReplicateMacroDelay)
                    {
                        var delay = P.Config.AutoDelay;
                        ImGui.PushItemWidth(250);
                        if (ImGui.SliderInt("执行延迟（毫秒）###ActionDelay", ref delay, 0, 1000))
                        {
                            if (delay < 0) delay = 0;
                            if (delay > 1000) delay = 1000;

                            P.Config.AutoDelay = delay;
                            P.Config.Save();
                        }
                    }
                    ImGui.Unindent();
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"消耗品");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("强制使用消耗品", ref P.Config.AbortIfNoFoodPot);
                ImGuiComponents.HelpMarker("Artisan 将要求配置好的食物、手册和药品，如果找不到则拒绝制作。");

                changed |= ImGui.Checkbox("对试验制作使用消耗品", ref P.Config.UseConsumablesTrial);
                changed |= ImGui.Checkbox("对简易制作使用消耗品", ref P.Config.UseConsumablesQuickSynth);

                ImGui.SetNextItemWidth(32f.Scale());
                if (ImGui.InputInt("当与制作的等级差大于以下数值时不使用消耗品", ref P.Config.ConsumableLevelGapDifference))
                {
                    if (P.Config.ConsumableLevelGapDifference < 0)
                        P.Config.ConsumableLevelGapDifference = 0;

                    P.Config.Save();
                }

                StringBuilder helper = new("对于以下每个职业，你不会在低于此等级的制作中使用消耗品：\n\n");
                for (uint i = (uint)Job.CRP; i <= (uint)Job.ALC; i++)
                {
                    var j = Svc.Data.GetExcelSheet<ClassJob>().GetRow(i).Abbreviation.ToString().ToUpper();
                    var l = CharacterInfo.JobLevel((Job)i);
                    var d = Math.Max(1, l - P.Config.ConsumableLevelGapDifference);
                    helper.Append($"{j} - {d}\n");
                }
                var maxLevel = Svc.Data.GetExcelSheet<RecipeLevelTable>().Max(x => x.ClassJobLevel);
                ImGuiComponents.HelpMarker($"将此设置为 {maxLevel} 以禁用。\r\n{helper}");

                if (ImGui.CollapsingHeader("默认消耗品"))
                {
                    ImGui.Indent();
                    changed |= P.Config.DefaultConsumables.DrawFood();
                    changed |= P.Config.DefaultConsumables.DrawPotion();
                    changed |= P.Config.DefaultConsumables.DrawManual();
                    changed |= P.Config.DefaultConsumables.DrawSquadronManual();
                    ImGui.Unindent();
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"修理");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox($"优先使用 NPC 修理而非自助修理", ref P.Config.PrioritizeRepairNPC);
                ImGuiComponents.HelpMarker("修理时，如果附近有修理 NPC，将尝试使用 NPC 修理而非自助修理。如果找不到 NPC 且你拥有所需的修理等级，仍会尝试使用自助修理。");

                changed |= ImGui.Checkbox($"无法修理时禁用耐力模式", ref P.Config.DisableEnduranceNoRepair);
                ImGuiComponents.HelpMarker($"耐力模式是指在完成一次制作后继续制作同一物品的功能，即\"制作 X\"。");

                changed |= ImGui.Checkbox($"无法修理时暂停清单", ref P.Config.DisableListsNoRepair);

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"声音");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("耐力模式完成后播放声音", ref P.Config.PlaySoundFinishEndurance);
                changed |= ImGui.Checkbox("清单完成后播放声音", ref P.Config.PlaySoundFinishList);
                changed |= ImGui.Checkbox("出错时播放声音", ref P.Config.PlaySoundError);

                if (P.Config.PlaySoundFinishEndurance || P.Config.PlaySoundFinishList || P.Config.PlaySoundError)
                {
                    ImGui.PushItemWidth(250);
                    changed |= ImGui.SliderFloat("音量", ref P.Config.SoundVolume, 0f, 1f, "%.2f");
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"杂项");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("任务搜索器就绪时禁用耐力模式并暂停清单", ref P.Config.RequestToStopDuty);

                if (P.Config.RequestToStopDuty)
                {
                    changed |= ImGui.Checkbox("完成时恢复耐力模式并取消暂停清单", ref P.Config.RequestToResumeDuty);

                    if (P.Config.RequestToResumeDuty)
                    {
                        ImGui.PushItemWidth(250);
                        changed |= ImGui.SliderInt("恢复前延迟（秒）", ref P.Config.RequestToResumeDelay, 5, 60);
                    }
                }

                changed |= ImGui.Checkbox("禁用特殊配方的自动装备所需物品", ref P.Config.DontEquipItems);
                ImGuiComponents.HelpMarker("例如伊克萨尔族任务和晓月生产采集工具配方。");

                ImGui.Dummy(new Vector2(0, 5f));
                if (ImGuiEx.ButtonCtrl("重置所有宇宙探索配方配置"))
                {
                    var copy = P.Config.RecipeConfigs;
                    foreach (var c in copy)
                    {
                        if (Svc.Data.GetExcelSheet<Recipe>().GetRow(c.Key).Number == 0)
                            P.Config.RecipeConfigs.Remove(c.Key);
                    }
                    P.Config.Save();
                }

                ImGui.Unindent();

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 10f));
            }

            if (ImGui.CollapsingHeader("宏设置"))
            {
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("若无法使用动作则跳过宏步骤", ref P.Config.SkipMacroStepIfUnable);
                changed |= ImGui.Checkbox($"防止 Artisan 在宏之后自动求解", ref P.Config.DisableMacroArtisanRecommendation);
                ImGuiComponents.HelpMarker($"仅在宏未完成配方时适用。Artisan 将根据配方使用标准或专家求解器。");

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 10f));
            }

            if (ImGui.CollapsingHeader("标准配方求解器设置"))
            {
                string ProgressString = LuminaSheets.AddonSheet[213].Text.ToString();
                string QualityString = LuminaSheets.AddonSheet[216].Text.ToString();
                string ConditionString = LuminaSheets.AddonSheet[215].Text.ToString();

                ImGui.Indent();

                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"动作使用");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                P.PluginUi.ExpertSettingsUI.DrawIconText("当 {0} 为以下情况时强制使用 [s!TricksOfTrade]：", [ConditionString.ToLower()]);
                P.PluginUi.ExpertSettingsUI.HelpMarkerWithIcons(["这些选项使求解器在合适的 {0} 上优先使用 [s!TricksOfTrade]。", "仅在会使用 [s!PreciseTouch] 或 [s!IntensiveSynthesis] 时才会触发。", "当 [s!TricksOfTrade] 为最优选择时，无论何种情况都会使用它。"], [ConditionString.ToLower()]);
                ImGui.Indent();
                changed |= P.PluginUi.ExpertSettingsUI.CheckboxWithIcons("useTricksGood", ref P.Config.UseTricksGood, "[c!Good]");
                changed |= P.PluginUi.ExpertSettingsUI.CheckboxWithIcons("useTricksExcellent", ref P.Config.UseTricksExcellent, "[c!Excellent]");
                ImGui.Unindent();

                changed |= ImGui.Checkbox("使用专家技能", ref P.Config.UseSpecialist);
                P.PluginUi.ExpertSettingsUI.HelpMarkerWithIcons(["若当前职业有专家认证，这将消耗能工巧匠图纸。", "当求解器会使用 [s!Observe] 时，将改用 [s!CarefulObservation]。", "[s!HeartAndSoul] 将用于提前使用 [s!PreciseTouch]。"]);

                changed |= P.PluginUi.ExpertSettingsUI.CheckboxWithIcons("useQualityStarter", ref P.Config.UseQualityStarter, "以 [s!Reflect] 开始制作");
                P.PluginUi.ExpertSettingsUI.HelpMarkerWithIcons(["这通常对低耐久的配方更有利。", "如果禁用，每次制作将以 [s!MuscleMemory] 开始。"]);

                //if (ImGui.Checkbox("低属性模式", ref P.Config.LowStatsMode))
                //    P.Config.Save();
                //ImGuiComponents.HelpMarker("这会替换部分低属性时不合适的制作技能。");

                ImGui.Dummy(new Vector2(0, 2f));
                P.PluginUi.ExpertSettingsUI.DrawIconText("在 {0} 层数达到或低于以下数值时使用 [s!PreparatoryTouch]：", [Buffs.InnerQuiet.NameOfBuff()]);
                ImGuiComponents.HelpMarker($"降低此数值有助于节省 CP。");
                ImGui.PushItemWidth(250);
                changed |= ImGui.SliderInt($"###MaxIQStacksPrepTouch", ref P.Config.MaxIQPrepTouch, 0, 10);

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"{QualityString}");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                ImGui.TextWrapped($"非收藏品配方的最大 {QualityString.ToLower()}：");
                ImGuiComponents.HelpMarker($"一旦品质达到此百分比，标准求解器将专注于进度。");
                ImGui.PushItemWidth(250);
                changed |= ImGui.SliderInt("###SliderMaxQuality", ref P.Config.MaxPercentage, 0, 100, $"%d%%");

                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"收藏品的 {QualityString} 阈值：");
                ImGuiComponents.HelpMarker($"一旦收藏品达到此阈值，求解器将不再追求 {QualityString.ToLower()}。具体阈值因配方而异。");

                if (ImGui.RadioButton($"1st", P.Config.SolverCollectibleMode == 1))
                {
                    P.Config.SolverCollectibleMode = 1;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"2nd", P.Config.SolverCollectibleMode == 2))
                {
                    P.Config.SolverCollectibleMode = 2;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"3rd", P.Config.SolverCollectibleMode == 3))
                {
                    P.Config.SolverCollectibleMode = 3;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"Max", P.Config.SolverCollectibleMode == 4))
                {
                    P.Config.SolverCollectibleMode = 4;
                    P.Config.Save();
                }

                ImGui.Dummy(new Vector2(0, 2f));
                var thresholdImg = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/CollectableThresholds.png");
                if (ThreadLoadImageHandler.TryGetTextureWrap(thresholdImg, out var img))
                {
                    ImGui.Image(img.Handle, new Vector2(img.Width, img.Height));
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"宇宙探索");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                ImGui.PushItemWidth(250);
                changed |= P.PluginUi.ExpertSettingsUI.SliderIntWithIcons("MaxMaterialMiracles", ref P.Config.MaxMaterialMiracles, 0, 3, "每次制作最多使用 [s!MaterialMiracle] 次数");
                ImGuiComponents.HelpMarker($"这将在增益效果持续期间将标准求解器切换为专家求解器。[s!MaterialMiracle] 是限时增益，而非带有层数的永久增益；使用标准求解器模拟配方时，它将根据每个技能的动画时长估算增益的持续时长。");
                if (P.Config.MaxMaterialMiracles > 0)
                {
                    ImGui.PushItemWidth(250);
                    changed |= ImGui.SliderInt($"在以下步数后使用###MinimumStepsBeforeMiracle", ref P.Config.MinimumStepsBeforeMiracle, 0, 20);
                }

                ImGui.Unindent();

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 10f));
            }

            bool openExpert = false;
            if (ImGui.CollapsingHeader("专家配方求解器设置"))
            {
                openExpert = true;
                if (P.PluginUi.ExpertSettingsUI.expertIcon is not null)
                {
                    ImGui.SameLine();
                    ImGui.Image(P.PluginUi.ExpertSettingsUI.expertIcon.Handle, new(P.PluginUi.ExpertSettingsUI.expertIcon.Width * ImGuiHelpers.GlobalScale, ImGui.GetItemRectSize().Y), new(0, 0), new Vector2(1, 1), new(0.94f, 0.57f, 0f, 1f));
                }
                if (P.PluginUi.ExpertSettingsUI.DrawGlobalSettings(P.Config.ExpertSolverConfig))
                    P.Config.Save();
            }
            if (!openExpert)
            {
                if (P.PluginUi.ExpertSettingsUI.expertIcon is not null)
                {
                    ImGui.SameLine();
                    ImGui.Image(P.PluginUi.ExpertSettingsUI.expertIcon.Handle, new(P.PluginUi.ExpertSettingsUI.expertIcon.Width * ImGuiHelpers.GlobalScale, ImGui.GetItemRectSize().Y), new(0, 0), new Vector2(1, 1), new(0.94f, 0.57f, 0f, 1f));
                }
            }

            if (ImGui.CollapsingHeader("Raphael 求解器设置"))
            {
                if (P.Config.RaphaelSolverConfig.Draw())
                    P.Config.Save();
            }

            using (ImRaii.Disabled())
            {
                if (ImGui.CollapsingHeader("脚本求解器设置（当前已禁用）"))
                {
                    if (P.Config.ScriptSolverConfig.Draw())
                        P.Config.Save();
                }
            }
            if (ImGui.CollapsingHeader("UI 设置"))
            {
                ImGui.Indent();

                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"General");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("禁用高亮框", ref P.Config.DisableHighlightedAction);
                ImGuiComponents.HelpMarker("这是手动操作时在热键栏上高亮显示动作的方框。");

                changed |= ImGui.Checkbox($"禁用推荐提示", ref P.Config.DisableToasts);
                ImGuiComponents.HelpMarker("这些是推荐新动作时弹出的通知。");

                changed |= ImGui.Checkbox("使用“执行接下来 X 个动作”按钮替代“执行推荐行动”按钮", ref P.Config.UseDoNextX);
                ImGuiComponents.HelpMarker("未使用自动行动模式时，将按钮替换为一次排队执行指定数量推荐动作，类似宏的连续执行。");

                if (P.Config.UseDoNextX)
                {
                    ImGui.Indent();
                    ImGui.PushItemWidth(50);
                    changed |= ImGui.InputInt("要排队执行的动作数量", ref P.Config.DoNextXAmount, 0, 0);
                    ImGui.Unindent();
                }

                changed |= ImGui.Checkbox("禁用自定义主题", ref P.Config.DisableTheme);
                ImGui.SameLine();
                if (IconButtons.IconTextButton(FontAwesomeIcon.Clipboard, "复制主题"))
                {
                    ImGui.SetClipboardText("DS1H4sIAAAAAAAACq1YS3PbNhD+Kx2ePR6AeJG+xXYbH+KOJ3bHbW60REusaFGlKOXhyX/v4rEACEqumlY+ECD32/cuFn7NquyCnpOz7Cm7eM1+zy5yvfnDPL+fZTP4at7MHVntyMi5MGTwBLJn+HqWLZB46Ygbx64C5kQv/nRo8xXQ3AhZZRdCv2jdhxdHxUeqrJO3Ftslb5l5u/Fa2rfEvP0LWBkBPQiSerF1Cg7wApBn2c5wOMv2juNn9/zieH09aP63g+Kqyr1mI91mHdj5mj3UX4bEG+b5yT0fzRPoNeF1s62e2np+EuCxWc+7z5cLr1SuuCBlkTvdqBCEKmaQxCHJeZmXnFKlgMHVsmnnEZ5IyXMiFUfjwt6yCHvDSitx1212m4gHV0QURY4saMEYl6Q4rsRl18/rPuCZQ+rFJxeARwyAJb5fVmD4NBaJEK3eL331UscuAgflOcY0J5zLUioHpHmhCC0lCuSBwU23r3sfF/0N0wKdoxcGFqHezYZmHypJIkgiSCJIalc8NEM7Utb6ErWlwngt9aUoFRWSB3wilRUl5SRwISUFvhJt9lvDrMgLIjgLzK66tq0228j0H+R3W693l1UfmUd9kqA79MKn9/2sB9lPI8hbofb073vdh1BbQYRgqKzfGbTfTWVqHmnMOcXUpI6BXhzGJjEQCNULmy4x9GpZz1a3Vb8KqaIDz4RPVGZin6dlZPKDSS29baAyRqYfzVGnr0ekaaowTbEw9MLjLnfD0GGT1unHSSlKr2lRyqLA2qU5ESovi6m+lkvqYiZ1/ygxyqrgjDKF8Yr2lp1pd4R7dokhvOBUQk37TCVKQbX4TMVtyuymruKWJCURVEofClYWbNpWCQfFifDwsWnYyXXS8ZxDOI+H0uLToPzrhKg3VV8N3amt1dP/t5goW/E85pg2pB8N8sd623yr3/dNOPYVstELg9cLA8zFCJKapQpEYkPVi9CMA/L/Uv8hrk1hmg9WKKMQXyIxnGFrm6i06MkhBHlIiQ8rI0xx4k/rsLWBsWpbTmmhqFIypcvUHTRgQ859V/bbKaPf1s/dbBcfD0R6NnCWwg/dS3lB4MfQMSrnCY9EK8qEw9uUl4YdHjRQRVFTuu5mq2a9uOvrfVOH0SDHqtXxMjDfi1RA/fyyGb7G5y5KdJg8EnTXdsOHZl1vQyJJQrlCQTDsEBi80HdhO+VwrEP48hwdTRp202yHbgGzhRfu03/UCA4gjglDd44mUT2D2i4UH9coSy8mfjEYN54NfbcOOIZnn15M7YqAH5rFEmdl3eJ8r0N5E9zH0fz71nQQyN+1/zSP6yR2A/l93dazoY6n5DdyiumWc91Xi+u+2zxU/aI+Jipq2QD5tdrfgO3t2P5jcqz9gLEXAEjgFHzcMJUgr5uXyDQsNSxZtCvX81s3r1qLOw0EztC3ORiEs4vssu9W9fqn2263HqpmncFF016PqklGjh1kjQ2NUyUJH08mcIk9gSrqn+jg0XFoqeqTrmDPwQv+PDEr6wl3oljaxcRSRTCyMc/lJJ/lAcnNhMr3WWZ+ES3exrXE+HJ2yNOrowkb97A2cExdXcrYjaFToVDfGSMqnCaDa0pi/vzNMyLG/wQEyzmzfhx7KAwJUn93Fz6v5shD8B+DRAG4Oh+QHYapovAd3/OEQzuiDSdE4c8wjJHh7iiBFFozvP3+NxT8RWGlEQAA");
                    Notify.Success("主题已复制到剪贴板");
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"Artisan 窗口");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("将迷你菜单位置停靠到制作笔记", ref P.Config.LockMiniMenuR);

                ImGui.Indent();
                if (!P.Config.LockMiniMenuR)
                {
                    changed |= ImGui.Checkbox($"锁定当前迷你菜单位置", ref P.Config.PinMiniMenu);
                }
                if (ImGui.Button("重置迷你菜单位置"))
                {
                    AtkResNodeFunctions.ResetPosition = true;
                }
                ImGui.Unindent();

                changed |= ImGui.Checkbox("隐藏迷你菜单模拟器结果", ref P.Config.HideRecipeWindowSimulator);

                changed |= ImGui.Checkbox($"隐藏任务助手", ref P.Config.HideQuestHelper);
                ImGuiComponents.HelpMarker("如果未禁用，任务助手是一个小窗口，可以为需要特定配方的任务打开制作笔记到指定配方、/say 特定短语或执行特定情感动作。它只会在进行这些任务时出现。");

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"原生 UI 替换");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox($"扩展制作笔记搜索栏", ref P.Config.ReplaceSearch);
                ImGuiComponents.HelpMarker($"扩展制作笔记中的搜索栏，提供即时结果。点击任意结果即可在制作笔记中打开。");

                changed |= ImGui.Checkbox("在配方记录中使用原生制作-X 按钮", ref P.Config.UseNativeButtons);
                ImGuiComponents.HelpMarker("这将把制作-X 按钮界面更改为使用原生游戏素材的版本。");

                changed |= ImGui.Checkbox("在制作笔记中显示升级分类完成度", ref P.Config.ShowLevelingRecipeProgress);
                ImGuiComponents.HelpMarker("显示每个升级分类中已完成的总配方数，若全部完成则显示勾选标记。");
                changed |= ImGui.Checkbox("在制作笔记中显示其他分类的完成度", ref P.Config.ShowOtherRecipeProgress);
                ImGuiComponents.HelpMarker("显示房屋分类和装饰品已完成的总配方数（若完成则显示勾选标记）。这些分类在原生 UI 中有完成度勾选标记，但不直接与成就挂钩。");

                changed |= ImGui.Checkbox("禁用右键菜单选项", ref P.Config.HideContextMenus);
                ImGuiComponents.HelpMarker("这些是你在配方或物品上右键单击或按方块键时 Artisan 添加的选项。");

                if (!P.Config.HideContextMenus)
                {
                    ImGui.Indent();
                    ImGui.PushItemWidth(50);
                    if (ImGui.InputInt("从右键菜单向清单添加新物品的次数", ref P.Config.ContextMenuLoops))
                    {
                        if (P.Config.ContextMenuLoops <= 0)
                            P.Config.ContextMenuLoops = 1;

                        P.Config.Save();
                    }
                    ImGui.Unindent();
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"其他插件");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("禁用 Allagan Tools 与制作清单的集成", ref P.Config.DisableAllaganTools);

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"模拟器界面");
                ImGuiComponents.HelpMarker("这些设置适用于 Artisan 的\"模拟器\"标签页。");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                ImGui.PushItemWidth(250);
                changed |= ImGui.SliderFloat("模拟器动作图标大小", ref P.Config.SimulatorActionSize, 5f, 70f);
                ImGuiComponents.HelpMarker("设置模拟器中动作图标的缩放比例。");

                changed |= ImGui.Checkbox("在手动模式中启用悬停预览", ref P.Config.SimulatorHoverMode);
                changed |= ImGui.Checkbox($"在手动模式中隐藏动作提示", ref P.Config.DisableSimulatorActionTooltips);

                ImGui.Unindent();

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 10f));
            }

            if (ImGui.CollapsingHeader("清单设置"))
            {
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"这些设置会在创建制作清单时自动应用。");

                ImGui.Indent();

                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"通用");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("跳过已经拥有足够数量的物品", ref P.Config.DefaultListSkip);
                changed |= ImGui.Checkbox("将新加入清单的物品设为简易制作", ref P.Config.DefaultListQuickSynth);
                changed |= ImGui.Checkbox("更改数量后自动调整所有子制作", ref P.Config.DefaultAdjustQuantities);
                changed |= ImGui.Checkbox("加入清单后重置\"添加次数\"", ref P.Config.ResetTimesToAdd);

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.TextWrapped($"自动化");
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Indent();

                changed |= ImGui.Checkbox("自动从精炼度已满的装备中提取魔晶石", ref P.Config.DefaultListMateria);

                changed |= ImGui.Checkbox("自动修理装备", ref P.Config.DefaultListRepair);

                if (P.Config.DefaultListRepair)
                {
                    ImGui.TextWrapped($"修理阈值：");
                    ImGui.SameLine();
                    ImGui.PushItemWidth(250);
                    changed |= ImGui.SliderInt("durability###SliderRepairDefault", ref P.Config.DefaultListRepairPercent, 0, 100, $"%d%%");
                }

                ImGui.PushItemWidth(250);
                if (ImGui.SliderFloat("清单制作间隔（秒）", ref P.Config.ListCraftThrottle2, 0f, 2f, "%.1f"))
                {
                    if (P.Config.ListCraftThrottle2 < 0f)
                        P.Config.ListCraftThrottle2 = 0f;

                    if (P.Config.ListCraftThrottle2 > 2f)
                        P.Config.ListCraftThrottle2 = 2f;

                    P.Config.Save();
                }

                ImGui.Unindent();

                ImGui.Dummy(new Vector2(0, 5f));
                if (ImGui.CollapsingHeader("材料表设置"))
                {
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, "列设置仅在你已经查看过某个清单的材料表后才会生效。");

                    changed |= ImGui.Checkbox($"默认：隐藏\"库存\"列", ref P.Config.DefaultHideInventoryColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"雇员\"列", ref P.Config.DefaultHideRetainerColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"仍需数量\"列", ref P.Config.DefaultHideRemainingColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"来源\"列", ref P.Config.DefaultHideCraftableColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"可制作数量\"列", ref P.Config.DefaultHideCraftableCountColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"用于制作\"列", ref P.Config.DefaultHideCraftItemsColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"分类\"列", ref P.Config.DefaultHideCategoryColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"采集区域\"列", ref P.Config.DefaultHideGatherLocationColumn);
                    changed |= ImGui.Checkbox($"默认：隐藏\"ID\"列", ref P.Config.DefaultHideIdColumn);
                    changed |= ImGui.Checkbox($"默认：启用\"仅显示 HQ 制作\"", ref P.Config.DefaultHQCrafts);
                    changed |= ImGui.Checkbox($"默认：启用\"颜色校验\"", ref P.Config.DefaultColourValidation);

                    ImGui.Dummy(new Vector2(0, 5f));
                    changed |= ImGui.Checkbox($"从 Universalis 获取价格", ref P.Config.UseUniversalis);
                    if (P.Config.UseUniversalis)
                    {
                        changed |= ImGui.Checkbox($"仅查询当前大区的 Universalis 数据", ref P.Config.LimitUnversalisToDC);
                        changed |= ImGui.Checkbox($"仅在请求时获取价格", ref P.Config.UniversalisOnDemand);
                        ImGuiComponents.HelpMarker("启用后，你需要为每个物品点击按钮来获取价格。");
                    }
                }

                ImGui.Unindent();
                ImGui.Dummy(new Vector2(0, 10f));
            }

            if (changed)
            {
                P.Config.Save();
            }
        }

        private void ShowEnduranceMessage()
        {
            if (!P.Config.ViewedEnduranceMessage)
            {
                P.Config.ViewedEnduranceMessage = true;
                P.Config.Save();

                ImGui.OpenPopup("EndurancePopup");

                var windowSize = new Vector2(512 * ImGuiHelpers.GlobalScale,
                    ImGui.GetTextLineHeightWithSpacing() * 13 + 2 * ImGui.GetFrameHeightWithSpacing() * 2f);
                ImGui.SetNextWindowSize(windowSize);
                ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - windowSize) / 2);

                using var popup = ImRaii.Popup("EndurancePopup",
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Modal);
                if (!popup)
                    return;

                ImGui.TextWrapped($@"我收到了不少关于耐力模式“出 bug”、不再自动设置材料的反馈。从上一次更新开始，耐力模式的旧功能已经移到了一个新的设置中。");
                ImGui.Dummy(new Vector2(0));

                var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/EnduranceNewSetting.png");

                if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var img))
                {
                    ImGuiEx.LineCentered("###EnduranceNewSetting", () =>
                    {
                        ImGui.Image(img.Handle, new Vector2(img.Width, img.Height));
                    });
                }

                ImGui.Spacing();

                ImGui.TextWrapped($"这个改动是为了恢复耐力模式最初的行为。如果你不关心材料配比，请务必启用最大数量模式。");

                ImGui.SetCursorPosY(windowSize.Y - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y);
                if (ImGui.Button("关闭", -Vector2.UnitX))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    public enum OpenWindow
    {
        None = 0,
        Main = 1,
        Endurance = 2,
        Macro = 3,
        Lists = 4,
        About = 5,
        Debug = 6,
        FCWorkshop = 7,
        SpecialList = 8,
        Overview = 9,
        Simulator = 10,
        RaphaelCache = 11,
        Assigner = 12,
        ExpertProfiles = 13,
    }
}
