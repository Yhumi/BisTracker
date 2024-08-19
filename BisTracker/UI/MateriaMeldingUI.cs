using BisTracker.BiS;
using BisTracker.BiS.Models;
using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.UI
{
    internal class MateriaMeldingUI : Window
    {
        private static string SelectedJobBisName = string.Empty;
        private static string SelectedJobBisSearch = string.Empty;

        private static JobBis? SavedJobBis = null;

        private static Vector2 BisSelectorWindowSize = Vector2.Zero;

        private static List<BisItem> BisItems = new();
        private static uint BisItemsSavedJob = 0;

        public MateriaMeldingUI() : base($"###MeldingWindow", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing)
        {
            this.Size = new Vector2(0, 0);
            this.Position = new Vector2(0, 0);
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            DisableWindowSounds = true;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MaximumSize = new Vector2(0, 0),
            };
        }

        public override void Draw()
        {
            if (!P.Config.ShowMateriaMeldingWindows) return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return;

            if (P.Config.SavedBis != null && P.Config.SavedBis.Any(x => x.Job == CharacterInfo.JobIDUint))
                DrawOptions();

            if (SavedJobBis != null)
            {
                if (CharacterInfo.JobIDUint != BisItemsSavedJob) ResetBis();
                else DrawMateriaHelper();
            }
                
        }

        public unsafe static void DrawMateriaHelper()
        {
            var mateiaMeldWindow = Svc.GameGui.GetAddonByName("MateriaAttach");
            if (mateiaMeldWindow == IntPtr.Zero)
                return;

            var addonPtr = (AtkUnitBase*)mateiaMeldWindow;
            if (addonPtr == null)
                return;

            var baseX = addonPtr->X;
            var baseY = addonPtr->Y;

            if (addonPtr->UldManager.NodeListCount > 1)
            {
                if (addonPtr->UldManager.NodeList[1]->IsVisible())
                {
                    var componentNode = addonPtr->UldManager.NodeList[1];

                    if (!componentNode->IsVisible())
                        return;

                    //if (!IsEquippedTab(addonPtr->UldManager.NodeList[26]->GetAsAtkComponentDropdownList()))
                    //    return;

                    var inventoryItemListComponentNode = addonPtr->UldManager.NodeList[17]->GetAsAtkComponentList();
                    AtkComponentBase* selectedItemPointer = GetSelectedItem(inventoryItemListComponentNode);

                    if (selectedItemPointer != null)
                    {
                        var selectedItemName = GetStringFromTextNode(selectedItemPointer->UldManager.NodeList[10]->GetAsAtkTextNode());

                        JobBis_Item? bisItem = SavedJobBis.BisItems.Where(x => 
                            x.ItemName != null && 
                            (x.ItemName.ToLower().StartsWith(selectedItemName.ToLower().TrimEnd('.')) ||
                            (P.Config.ShowAugmentedMeldsForUnaugmentedPieces && x.ItemName.ToLower().StartsWith($"Augmented {selectedItemName}".ToLower().TrimEnd('.'))))).FirstOrDefault();

                        
                        if (bisItem != null)
                        {
                            DrawBisPieceWindow(componentNode, bisItem);

                            if (IsEquippedTab(addonPtr->UldManager.NodeList[26]->GetAsAtkComponentDropdownList()) && bisItem.Materia.Count > 0 && P.Config.HighlightBisMateriaInMateriaMelder)
                            {
                                var item = CharacterInfo.GetEquippedItem(bisItem.Id);
                                if (item == null) return;

                                var melds = item->Materia;
                                var slottedMelds = melds.ToArray().Where(x => x != 0);
                                if (slottedMelds.Count() == bisItem.Materia.Count) return; //The item is fully melded.

                                var nextMateriaToMeld = bisItem.Materia[slottedMelds.Count()];
                                if (nextMateriaToMeld == null) return;

                                var materiaItemListComponentNode = addonPtr->UldManager.NodeList[7]->GetAsAtkComponentList();
                                UpdateBisMateriaNameColor(materiaItemListComponentNode, new List<string>() { nextMateriaToMeld.ItemName ?? string.Empty });
                            }
                        }
                    }
                }
            }
        }

        public unsafe static void DrawBisPieceWindow(AtkResNode* componentNode, JobBis_Item bisItem)
        {
            var position = AtkResNodeFunctions.GetNodePosition(componentNode);
            var scale = AtkResNodeFunctions.GetNodeScale(componentNode);
            var size = new Vector2(componentNode->Width, componentNode->Height) * scale;

            var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);

            ImGuiHelpers.ForceNextWindowMainViewport();

            if ((AtkResNodeFunctions.ResetPosition && position.X != 0) || P.Config.LockMiniMenuR)
            {
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + BisSelectorWindowSize.Y + 14), ImGuiCond.Always);
                AtkResNodeFunctions.ResetPosition = false;
            }
            else
            {
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + BisSelectorWindowSize.Y + 14), ImGuiCond.FirstUseEver);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));

            var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
            if (P.Config.PinMiniMenu)
                flags |= ImGuiWindowFlags.NoMove;

            ImGui.Begin($"###BisPiece{componentNode->NodeId}", flags);
            ImGui.Text($"{bisItem.ItemName}");
            ImGui.Separator();
            if (bisItem.Materia.Count > 0)
            {
                foreach (var bisItemMateria in bisItem.Materia)
                {
                    ImGui.Text(bisItemMateria.GetMateriaLabel());
                }
            }
            else { ImGui.Text("No melds.");  }
            

            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        public unsafe static void UpdateBisMateriaNameColor(AtkComponentList* materiaItemListComponentNode, List<string> bisMateriaNames)
        {
            try
            {
                var listItemCount = materiaItemListComponentNode->UldManager.NodeListCount;
                for (var i = 3; i < listItemCount - 2; i++)
                {
                    var listItem = materiaItemListComponentNode->UldManager.NodeList[i]->GetAsAtkComponentNode();
                    var materiaText = listItem->GetComponent()->UldManager.NodeList[5]->GetAsAtkTextNode()->NodeText.ExtractText();
                    if (bisMateriaNames.Contains(materiaText))
                    {
                        listItem->GetComponent()->UldManager.NodeList[5]->GetAsAtkTextNode()->SetText($"\u0002H\u0002D\u0003\u0002I\u0002E\u0003{materiaText}\u0002I\u0002\u0001\u0003\u0002H\u0002\u0001\u0003");
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public unsafe static AtkComponentBase* GetSelectedItem(AtkComponentList* inventoryItemListComponentNode)
        {
            try
            {
                var listItemCount = inventoryItemListComponentNode->UldManager.NodeListCount;
                for (var i = 3; i < listItemCount - 2; i++)
                {
                    var listItem = inventoryItemListComponentNode->UldManager.NodeList[i]->GetAsAtkComponentNode();
                    if (listItem->GetComponent()->UldManager.NodeList[1]->IsVisible())
                    {
                        return listItem->GetComponent();
                    }
                }
                return null;
            } 
            catch(Exception e)
            {
                return null;
            }
        }

        public unsafe static bool IsEquippedTab(AtkComponentDropDownList* materiaMeldingDropdown)
        {
            var checkBoxNode = materiaMeldingDropdown->UldManager.NodeList[1]->GetAsAtkComponentCheckBox();
            var selectedItem = checkBoxNode->UldManager.NodeList[2]->GetAsAtkTextNode();
            return selectedItem->NodeText.ExtractText().Trim().ToLower() == "equipped";
        }

        public unsafe static string GetStringFromTextNode(AtkTextNode* textNode)
        {
            return textNode->NodeText.ExtractText().Replace("", "").TrimEnd();
        }

        public unsafe static void DrawOptions()
        {
            var mateiaMeldWindow = Svc.GameGui.GetAddonByName("MateriaAttach");
            if (mateiaMeldWindow == IntPtr.Zero)
                return;

            var addonPtr = (AtkUnitBase*)mateiaMeldWindow;
            if (addonPtr == null)
                return;

            var baseX = addonPtr->X;
            var baseY = addonPtr->Y;

            if (addonPtr->UldManager.NodeListCount > 1)
            {
                if (addonPtr->UldManager.NodeList[1]->IsVisible())
                {
                    var node = addonPtr->UldManager.NodeList[1];

                    if (!node->IsVisible())
                        return;

                    if (P.Config.LockMiniMenuR)
                    {
                        var position = AtkResNodeFunctions.GetNodePosition(node);
                        var scale = AtkResNodeFunctions.GetNodeScale(node);
                        var size = new Vector2(node->Width, node->Height) * scale;
        
                        var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                        //position += ImGuiHelpers.MainViewport.Pos;

                        ImGuiHelpers.ForceNextWindowMainViewport();

                        if ((AtkResNodeFunctions.ResetPosition && position.X != 0) || P.Config.LockMiniMenuR)
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);
                            AtkResNodeFunctions.ResetPosition = false;
                        }
                        else
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                        }
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                    var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding;
                    if (P.Config.PinMiniMenu)
                        flags |= ImGuiWindowFlags.NoMove;

                    ImGui.Begin($"###Options{node->NodeId}", flags);

                    ImGui.Text($"{CharacterInfo.JobID} BiS:");
                    if (ImGui.BeginCombo("###BisSelection", SelectedJobBisName))
                    {
                        ImGui.Text("Search");
                        ImGui.SameLine();
                        ImGui.InputText("###BisXivGearAppSetSearch", ref SelectedJobBisSearch, 100);

                        if (ImGui.Selectable("", SelectedJobBisName == string.Empty))
                        {
                            ResetBis();
                        }

                        foreach (var bisSet in P.Config.SavedBis.Where(x => x.Job == CharacterInfo.JobIDUint))
                        {
                            bool selected = ImGui.Selectable($"{bisSet.Name}", bisSet.Name == SelectedJobBisName);
                            
                            if (selected)
                            {
                                ResetBis();
                                SelectedJobBisName = bisSet.Name;
                                BisItemsSavedJob = CharacterInfo.JobIDUint;
                                SavedJobBis = bisSet;
                                //LoadBisIntoList();
                            }
                        }
                    }

                    BisSelectorWindowSize = ImGui.GetWindowSize();

                    ImGui.End();
                    ImGui.PopStyleVar(2);
                }
            }
        }

        public static void ResetBis()
        {
            BisItems.Clear();
            BisItemsSavedJob = 0;
            SelectedJobBisName = string.Empty;
        }

        public static void LoadBisIntoList()
        {
            if (SavedJobBis == null || SavedJobBis.BisItems == null) { return; }

            foreach (var bisItem in SavedJobBis.BisItems)
            {
                BisItems.Add(new(bisItem));
            }

            Svc.Log.Debug($"Added {BisItems.Count} items to in-memory bis sheet.");
        }

        public override void PreDraw()
        {
        }

        public override void PostDraw()
        {
        }
        public void Dispose()
        {
        }  
    }
}
