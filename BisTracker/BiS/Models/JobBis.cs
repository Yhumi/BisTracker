using BisTracker.RawInformation.Character;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.BiS.Models
{
    public class JobBis
    {
        public uint? Job { get; set; }
        public string? Fight { get; set; }
        public string? Name { get; set; }
        public int? Food { get; set; }

        //Better system
        public List<JobBis_Item>? BisItems { get; set; }

        //Old system for updating
        public string SelectedXivGearAppSet { get; set; }
        public XivGearApp_SetItems XivGearAppSetItems { get; set; }


        public void PopulateBisItemsFromXIVGearApp(XivGearAppResponse xivGearAppResponse, string? selectedSetName)
        {
            if (xivGearAppResponse == null) return;
            if (xivGearAppResponse.Sets != null)
            {
                if (selectedSetName == null) return;
                var selectedSet = GetSetFromSelectedSetName(xivGearAppResponse.Sets, selectedSetName);
                if (selectedSet == null) return;

                CreateBisItemsFromXivGearAppSetItems(selectedSet);
                return;
            }

            if (xivGearAppResponse.Items != null)
            {
                CreateBisItemsFromXivGearAppSetItems(xivGearAppResponse.Items);
                return;
            }
        }

        private XivGearApp_SetItems? GetSetFromSelectedSetName(XivGearApp_Set[] xivGearAppSets, string selectedSetName)
        {
            return xivGearAppSets.Where(x => x.Name.ToLower() == selectedSetName.ToLower()).FirstOrDefault()?.Items ?? null;
        }

        public void CreateBisItemsFromXivGearAppSetItems(XivGearApp_SetItems setItems)
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
        }

        public void PopulateBisItemsFromEtro(EtroResponse etroResponse)
        {
            if (etroResponse == null) return;
            if (BisItems == null) BisItems = new List<JobBis_Item>();
            if (etroResponse.SetItems == null) return;

            foreach (var item in etroResponse.SetItems)
            {
                BisItems.Add(new(item));
            }
        }
    }

    public class JobBis_Item
    {
        public int Id { get; set; }
        public CharacterEquippedGearSlotIndex GearSlot { get; set; }
        public List<JobBis_ItemMateria>? Materia { get; set; }

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
        }

        public JobBis_Item(EtroItem? item)
        {
            Id = item?.Id ?? 0;
            GearSlot = item?.GearSlot ?? CharacterEquippedGearSlotIndex.SoulCrystal;
            Materia = new List<JobBis_ItemMateria>();

            if (item != null && item.Materia != null)
            {
                Materia.Add(new(item.Materia.MateriaSlot1));
                Materia.Add(new(item.Materia.MateriaSlot2));
                Materia.Add(new(item.Materia.MateriaSlot3));
                Materia.Add(new(item.Materia.MateriaSlot4));
                Materia.Add(new(item.Materia.MateriaSlot5));
            }
        }
    }

    public class JobBis_ItemMateria
    {
        public int Id { get; set; }

        public JobBis_ItemMateria() { }

        public JobBis_ItemMateria(XivGearApp_Materia materia)
        {
            Id = materia.Id; 
        }

        public JobBis_ItemMateria(int itemId)
        {
            Id = itemId;
        }
    }
}
