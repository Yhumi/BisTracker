using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using ECommons;
using ECommons.DalamudServices;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace BisTracker.BiS.Models
{
    public class JobBis
    {
        public uint? Job { get; set; }
        public string? Fight { get; set; }
        public string? Name { get; set; }
        public int? Food { get; set; }
        public int? Medicine { get; set; }

        //Better system
        public List<JobBis_Item>? BisItems { get; set; }

        //Old system for updating
        public string? SelectedXivGearAppSet { get; set; }
        public XivGearApp_SetItems? XivGearAppSetItems { get; set; }

        [NonSerialized]
        public List<JobBis_Parameter>? SetParameters;

        public JobBis() { }
        public JobBis(JobBis? jobBis)
        {
            if (jobBis == null) return;

            Job = jobBis.Job;
            Fight = jobBis.Fight;
            Name = jobBis.Name;
            Food = jobBis.Food;
            Medicine = jobBis.Medicine;

            BisItems = jobBis.BisItems;

            SelectedXivGearAppSet = jobBis.SelectedXivGearAppSet;
            XivGearAppSetItems = jobBis.XivGearAppSetItems;
        }

        public void PopulateBisItemsFromXIVGearApp(XivGearAppResponse xivGearAppResponse, string? selectedSetName)
        {
            if (xivGearAppResponse == null) return;
            Food = xivGearAppResponse.Food;
            

            if (xivGearAppResponse.Sets != null)
            {
                if (selectedSetName == null) return;
                Name = selectedSetName;
                var selectedSet = GetSetFromSelectedSetName(xivGearAppResponse.Sets, selectedSetName);
                Svc.Log.Debug($"Found: {selectedSet}");
                if (selectedSet == null) return;

                CreateBisItemsFromXivGearAppSetItems(selectedSet.Items, selectedSet.Food);
                return;
            }

            if (xivGearAppResponse.Items != null)
            {
                Name = xivGearAppResponse.Name;
                CreateBisItemsFromXivGearAppSetItems(xivGearAppResponse.Items, xivGearAppResponse.Food);
                return;
            }
        }

        private XivGearApp_Set? GetSetFromSelectedSetName(XivGearApp_Set[] xivGearAppSets, string selectedSetName)
        {
            Svc.Log.Debug($"Finding: {selectedSetName}");
            return xivGearAppSets.Where(x => x.Name.ToLower() == selectedSetName.ToLower()).FirstOrDefault() ?? null;
        }

        public void CreateBisItemsFromXivGearAppSetItems(XivGearApp_SetItems setItems, int? food)
        {
            if (BisItems == null) BisItems = new List<JobBis_Item>();

            BisItems.Add(new JobBis_Item(setItems.Weapon, CharacterEquippedGearSlotIndex.MainHand));
            BisItems.Add(new JobBis_Item(setItems.OffHand, CharacterEquippedGearSlotIndex.OffHand));

            BisItems.Add(new JobBis_Item(setItems.Head, CharacterEquippedGearSlotIndex.Head));
            BisItems.Add(new JobBis_Item(setItems.Body, CharacterEquippedGearSlotIndex.Body));
            BisItems.Add(new JobBis_Item(setItems.Hand, CharacterEquippedGearSlotIndex.Gloves));
            BisItems.Add(new JobBis_Item(setItems.Legs, CharacterEquippedGearSlotIndex.Legs));
            BisItems.Add(new JobBis_Item(setItems.Feet, CharacterEquippedGearSlotIndex.Feet));

            BisItems.Add(new JobBis_Item(setItems.Ears, CharacterEquippedGearSlotIndex.Ears));
            BisItems.Add(new JobBis_Item(setItems.Neck, CharacterEquippedGearSlotIndex.Neck));
            BisItems.Add(new JobBis_Item(setItems.Wrist, CharacterEquippedGearSlotIndex.Wrists));
            BisItems.Add(new JobBis_Item(setItems.RingRight, CharacterEquippedGearSlotIndex.RightRing));
            BisItems.Add(new JobBis_Item(setItems.RingLeft, CharacterEquippedGearSlotIndex.LeftRing));

            Food = food;

            CalculateSetParmeters();
            CalculateSetPrice();
        }

        public void PopulateBisItemsFromEtro(EtroResponse etroResponse)
        {
            if (etroResponse == null) return;
            if (BisItems == null) BisItems = new List<JobBis_Item>();
            if (etroResponse.SetItems == null) return;
            Job = (uint?)etroResponse.Job;
            Name = etroResponse.Name;

            foreach (var item in etroResponse.SetItems)
            {
                BisItems.Add(new(item));
            }

            if (etroResponse.Food != null)
            {
                var foodItem = LuminaSheets.GetItemFromItemFoodRowId(etroResponse.Food.GetValueOrDefault());
                if (foodItem != null)
                    Food = (int)foodItem.Value.RowId;
            }

            CalculateSetParmeters();
            CalculateSetPrice();
        }

        public void SetupItemStatistics()
        {
            Svc.Log.Debug("Setting up statistics.");
            if (BisItems == null) { return; }

            foreach (var bisItem in BisItems)
            {
                bisItem.SetupParams();
            }

            CalculateSetParmeters();
        }

        public void CalculateSetParmeters()
        {
            SetParameters = new List<JobBis_Parameter>();
            if (BisItems == null)
                BisItems = new();

            foreach (var item in BisItems)
            {
                var itemParams = item.GetParametersWithMateria();

                if (itemParams != null)
                {
                    foreach(var itemParam in itemParams)
                    {
                        int paramIndex = SetParameters.FindIndex(x => x.Param == itemParam.Param);

                        //Svc.Log.Debug($"{LuminaSheets.BaseParamSheet[itemParam.Param].Name} index: {paramIndex}");
                        if (paramIndex > -1)
                        {
                            //Svc.Log.Debug($"Old value: {SetParameters[paramIndex].Value}");
                            var setParam = SetParameters[paramIndex];
                            setParam.Value += itemParam.Value;

                            SetParameters[paramIndex] = setParam;
                            //Svc.Log.Debug($"New value: {SetParameters[paramIndex].Value}");
                            continue;
                        }

                        //Svc.Log.Debug($"Old value: 0");
                        var newParam = new JobBis_Parameter()
                        {
                            Param = itemParam.Param,
                            Value = itemParam.Value
                        };
                        SetParameters.Add(newParam);

                        //Svc.Log.Debug($"New value: {newParam.Value}");
                    }
                }
            } 

            //Add the base value from the level
            var levelData = ConstantData.LevelStats[100];
            var jobData = LuminaSheets.ClassJobSheet[Job.GetValueOrDefault()];
            string jobCategory = jobData.ClassJobCategory.Value.Name.ExtractText().ToLower();
            Svc.Log.Debug(jobCategory);

            if (jobCategory.Contains("magic") || jobCategory.Contains("war"))
            {
                foreach (var stat in SetParameters)
                {
                    bool isMainStat = ConstantData.MainStatIds.Values.Contains(stat.Param);
                    bool isFakeMainStat = ConstantData.FakeMainStatIds.Values.Contains(stat.Param);

                    short newVal = stat.Value;
                    if (isMainStat)
                    {
                        newVal = (short)(stat.Value + (levelData.BaseMainStat * GetMainStatModifierForJob(stat.Param) / 100));
                    }

                    if (isFakeMainStat)
                    {
                        newVal = (short)(stat.Value + levelData.BaseMainStat);
                    }


                    if (!isMainStat && !isFakeMainStat)
                    {
                        newVal = (short)(stat.Value + levelData.BaseSubStat);
                    }

                    stat.Value = newVal;
                }

                //Add the remaining substat base values

                foreach (var subStat in ConstantData.SubStatIds)
                {
                    if (subStat.Key == "Spell Speed" && jobCategory.Contains("war")) continue;
                    if (subStat.Key == "Skill Speed" && jobCategory.Contains("magic")) continue;
                    if (subStat.Key == "Tenacity" && !TankJob()) continue;

                    var subStatSetParameterIndex = SetParameters.Where(x => x.Param == subStat.Value).FirstOrDefault();
                    if (subStatSetParameterIndex == null)
                    {
                        var subStatSetParameter = new JobBis_Parameter() { Param = subStat.Value, Value = (short)levelData.BaseSubStat };
                        SetParameters.Add(subStatSetParameter);
                    }
                }
            }

            if (jobCategory.Contains("hand"))
            {
                var cpParam = SetParameters.Where(x => x.Param == ConstantData.DoHStatIds["CP"]).FirstOrDefault();
                if (cpParam == null)
                {
                    cpParam = new JobBis_Parameter()
                    {
                        Param = ConstantData.DoHStatIds["CP"],
                        Value = 0
                    };
                    SetParameters.Add(cpParam);
                }

                SetParameters.Where(x => x.Param == ConstantData.DoHStatIds["CP"]).First().Value = (short)(cpParam.Value + ConstantData.LevelStats[100].CP);
            }

            if (jobCategory.Contains("land"))
            {
                var gpParam = SetParameters.Where(x => x.Param == ConstantData.DoLStatIds["GP"]).FirstOrDefault();
                if (gpParam == null)
                {
                    gpParam = new JobBis_Parameter()
                    {
                        Param = ConstantData.DoLStatIds["GP"],
                        Value = 0
                    };
                    SetParameters.Add(gpParam);
                }

                SetParameters.Where(x => x.Param == ConstantData.DoLStatIds["GP"]).First().Value = (short)(gpParam.Value + ConstantData.LevelStats[100].GP);
            }
            
        }
    
        public void CalculateSetPrice()
        {
            if (BisItems == null)
                BisItems = new();
            foreach (var item in BisItems)
            {
                //Svc.Log.Debug($"SpecialShopCategory for {item.ItemName}: {LuminaSheets.GetSpecialShopContainingItem(item.Id)}");
            }
        }
    
        public ushort GetMainStatModifierForJob(uint stat)
        {
            var jobData = LuminaSheets.ClassJobSheet[Job.GetValueOrDefault()];
            var baseParam = LuminaSheets.BaseParamSheet[stat];

            switch (baseParam.Name.ExtractText())
            {
                case "Strength":
                    Svc.Log.Debug($"Strength modifier: {jobData.ModifierStrength}");
                    return jobData.ModifierStrength;
                case "Dexterity":
                    Svc.Log.Debug($"Dexterity modifier: {jobData.ModifierDexterity}");
                    return jobData.ModifierDexterity;
                case "Vitality":
                    Svc.Log.Debug($"Vitality modifier: {jobData.ModifierVitality}");
                    return jobData.ModifierVitality;
                case "Intelligence":
                    Svc.Log.Debug($"Intelligence modifier: {jobData.ModifierIntelligence}");
                    return jobData.ModifierIntelligence;
                case "Mind":
                    Svc.Log.Debug($"Mind modifier: {jobData.ModifierMind}");
                    return jobData.ModifierMind;
                default:
                    return default;
            }
        }

        private void SetParameterModifier(uint param, ushort modifier)
        {
            if (modifier == 100) return;
            var paramIndex = SetParameters.IndexOf(x => x.Param == param);
            if (paramIndex != -1)
            {
                //Svc.Log.Debug($"[{LuminaSheets.BaseParamSheet[param].Name}] Modifier: {(modifier / 100d)}");
                //Svc.Log.Debug($"[{LuminaSheets.BaseParamSheet[param].Name}] Old value: {SetParameters[paramIndex].Value}");
                var statValue = SetParameters[paramIndex].Value;
                var modifiedValue = statValue * (modifier / 100d);
                SetParameters[paramIndex].Value = (short)Math.Floor((double)modifiedValue);
                //Svc.Log.Debug($"[{LuminaSheets.BaseParamSheet[param].Name}] Old value: {SetParameters[paramIndex].Value}");
            }
        }
    
        private bool TankJob()
        {
            var jobData = LuminaSheets.ClassJobSheet[Job.GetValueOrDefault()];
            return jobData.ClassJobCategory.Value.WAR || jobData.ClassJobCategory.Value.PLD || jobData.ClassJobCategory.Value.DRK || jobData.ClassJobCategory.Value.GNB;
        }
    }

    public class JobBis_Item
    {
        public int Id { get; set; }
        public string ItemName => LuminaSheets.ItemSheet[(uint)Id].Name.ExtractText() ?? string.Empty;
        public string ItemSet => LuminaSheets.ItemSheet[(uint)Id].ItemSeries.Value.Name.ExtractText() ?? string.Empty;
        public CharacterEquippedGearSlotIndex GearSlot { get; set; }
        public List<JobBis_ItemMateria>? Materia { get; set; }

        [NonSerialized]
        public List<JobBis_Parameter> BaseParameters;
        [NonSerialized]
        public List<JobBis_Parameter> SpecialParameters;


        public JobBis_Item() { }
        public JobBis_Item(XivGearApp_Item? item, CharacterEquippedGearSlotIndex slot)
        {
            Id = item?.Id ?? 0;
            GearSlot = slot;
            Materia = new List<JobBis_ItemMateria>();

            if (item != null && item.Materia != null)
            {
                foreach (var materia in item.Materia)
                {
                    Materia.Add(new JobBis_ItemMateria(materia));
                }
            }

            if (Id != 0)
                SetupParams();
        }

        public JobBis_Item(EtroItem? item)
        {
            Id = item?.Id ?? 0;
            GearSlot = item?.GearSlot ?? CharacterEquippedGearSlotIndex.SoulCrystal;
            Materia = new List<JobBis_ItemMateria>();

            if (item != null && item.Materia != null)
            {
                if (item.Materia.MateriaSlot1 != 0)
                    Materia.Add(new(item.Materia.MateriaSlot1));

                if (item.Materia.MateriaSlot2 != 0)
                    Materia.Add(new(item.Materia.MateriaSlot2));

                if (item.Materia.MateriaSlot3 != 0)
                    Materia.Add(new(item.Materia.MateriaSlot3));

                if (item.Materia.MateriaSlot4 != 0)
                    Materia.Add(new(item.Materia.MateriaSlot4));

                if (item.Materia.MateriaSlot5 != 0)
                    Materia.Add(new(item.Materia.MateriaSlot5));
            }

            if (Id != 0)
                SetupParams();
        }

        public void SetupParams()
        {
            BaseParameters = new List<JobBis_Parameter>();

            //Svc.Log.Debug($"[{ItemName}] Use HQ Stats? {LuminaSheets.ItemSheet[(uint)Id]?.CanBeHq}");
            var useHq = false;
            var paramItem = LuminaSheets.ItemSheet[(uint)Id];

            foreach (var baseParam in paramItem.BaseParam)
            {
                if (baseParam.Value.RowId != 0)
                {
                    var index = LuminaSheets.ItemSheet[(uint)Id].BaseParam.IndexOf(x => x.Value.RowId == baseParam.Value.RowId);
                    if (index != null)
                    {
                        short val = paramItem.BaseParamValue[index];

                        //Svc.Log.Debug($"[{ItemName}] {LuminaSheets.BaseParamSheet[baseParam.Value.RowId]?.Name} ({baseParam.Value.RowId}): {val}");
                        BaseParameters.Add(new JobBis_Parameter()
                        {
                            Param = baseParam.Value.RowId,
                            Value = val
                        });
                    }
                }
            }
            

            foreach (var materia in Materia)
            {
                materia.SetupParams();
            }
        }

        public List<JobBis_Parameter>? GetParametersWithMateria()
        {
            if (BaseParameters == null) { return null; }
            var finalParameters = new List<JobBis_Parameter>();

            foreach (var param in BaseParameters)
            {
                finalParameters.Add(new(param));
            }

            //var itemLevel = LuminaSheets.ItemSheet[(uint)Id].ItemSeries.Value;
            //PropertyInfo[] properties = typeof(ItemSeries).GetProperties();
            //foreach (var property in properties)
            //{
            //    Svc.Log.Debug($"[{ItemName}][ItemSeries] Prop: {property.Name} | Val: {property.GetValue(itemLevel)}");
            //}

            //Svc.Log.Debug($"[{ItemName}] Materia Count: {Materia.Count}");

            if (Materia != null)
            {
                foreach (var materia in Materia)
                {
                    if (materia.MateriaParameter == null) continue;
                    //Svc.Log.Debug($"[{ItemName}] Materia {LuminaSheets.BaseParamSheet[materia.MateriaParameter.Param].Name.ExtractText()}: {materia.MateriaParameter.Value}");

                    int paramIndex = finalParameters.FindIndex(x => x.Param == materia.MateriaParameter.Param);
                    if (paramIndex > -1)
                    {
                        var itemParam = finalParameters[paramIndex];
                        var value = itemParam.Value + materia.MateriaParameter.Value;

                        var maxVal = LuminaSheets.GetMaxStatForItem((uint)Id, itemParam.Param);
                        if (maxVal == null) continue;

                        itemParam.Value = (short) Math.Min(maxVal.Value, value);

                        finalParameters[paramIndex] = itemParam;
                        continue;
                    }

                    var newParam = new JobBis_Parameter()
                    {
                        Param = materia.MateriaParameter.Param,
                        Value = materia.MateriaParameter.Value
                    };
                    finalParameters.Add(newParam);
                }
            }

            //foreach(var param in finalParameters)
            //{
            //    Svc.Log.Debug($"[{ItemName}] [{LuminaSheets.BaseParamSheet[param.Param].Name} (Id {param.Param})] Base: {BaseParameters.FirstOrDefault(x => x.Param == param.Param)?.Value ?? 0} | Total: {param.Value}");
            //}

            return finalParameters;
        }
    }

    public class JobBis_ItemMateria
    {
        public int Id { get; set; }
        public string ItemName => LuminaSheets.ItemSheet[(uint)Id].Name.ExtractText();

        [NonSerialized]
        public JobBis_Parameter? MateriaParameter;

        public JobBis_ItemMateria() { }

        public JobBis_ItemMateria(XivGearApp_Materia materia)
        { 
            Id = materia.Id;
            SetupParams();
        }

        public JobBis_ItemMateria(int itemId)
        {
            Id = itemId;
            SetupParams();
        }

        public void SetupParams()
        {
            var luminaMateria = LuminaSheets.GetMateriaFromSpecificMateria(Id);
            if (luminaMateria == null) return;

            var materiaIndex = luminaMateria.Value.Item.IndexOf(x => x.Value.RowId == (uint)Id);
            if (materiaIndex == -1) return;

            MateriaParameter = new JobBis_Parameter()
            {
                Param = (byte)luminaMateria.Value.BaseParam.Value!.RowId,
                Value = luminaMateria.Value.Value[materiaIndex]
            };
        }

        public string GetMateriaLabel()
        {
            if (MateriaParameter == null || P.Config.UseMateriaNameInsteadOfMateriaValue)
                return LuminaSheets.ItemSheet[(uint)Id].Name.ExtractText();

            return $"{LuminaSheets.BaseParamSheet[(uint)MateriaParameter.Param].Name.ExtractText()} +{MateriaParameter.Value}";
        }
    }

    public class JobBis_Parameter
    {
        public uint Param { get; set; }
        public short Value { get; set; }

        public JobBis_Parameter() { }
        public JobBis_Parameter(JobBis_Parameter copy)
        {
            Param = copy.Param;
            Value = copy.Value;
        }
    }
}
