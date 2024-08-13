using BisTracker.RawInformation.Character;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BisTracker.BiS.Models
{
    public class EtroResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public bool Error = false;

        public List<EtroItem>? SetItems { get; set; } = null;

        public EtroResponse() { }

        public EtroResponse(bool error)
        {
            Error = error;
        }

        public EtroResponse(string jsonObject)
        {
            dynamic? etroObject = JsonConvert.DeserializeObject(jsonObject);

            if (etroObject == null) { return; }
            Id = etroObject["id"];
            Name = etroObject["name"];
        }

        public void BuildItemsFromEtroResponse(string jsonObject) 
        {
            Svc.Log.Debug($"Building items from: {jsonObject}");
            if (SetItems == null) SetItems = new List<EtroItem>();
            dynamic? etroObject = JsonConvert.DeserializeObject(jsonObject);
            if (etroObject == null) { return; }

            BuildItem(etroObject["weapon"], CharacterEquippedGearSlotIndex.MainHand, jsonObject);
            BuildItem(etroObject["offHand"], CharacterEquippedGearSlotIndex.OffHand, jsonObject);

            BuildItem(etroObject["head"], CharacterEquippedGearSlotIndex.Head, jsonObject);
            BuildItem(etroObject["body"], CharacterEquippedGearSlotIndex.Body, jsonObject);
            BuildItem(etroObject["hands"], CharacterEquippedGearSlotIndex.Gloves, jsonObject);
            BuildItem(etroObject["legs"], CharacterEquippedGearSlotIndex.Legs, jsonObject);
            BuildItem(etroObject["feet"], CharacterEquippedGearSlotIndex.Feet, jsonObject);

            BuildItem(etroObject["ears"], CharacterEquippedGearSlotIndex.Ears, jsonObject);
            BuildItem(etroObject["neck"], CharacterEquippedGearSlotIndex.Neck, jsonObject);
            BuildItem(etroObject["wrists"], CharacterEquippedGearSlotIndex.Wrists, jsonObject);
            BuildItem(etroObject["fingerR"], CharacterEquippedGearSlotIndex.RightRing, jsonObject);
            BuildItem(etroObject["fingerL"], CharacterEquippedGearSlotIndex.LeftRing, jsonObject);
        }

        private void BuildItem(int itemId, CharacterEquippedGearSlotIndex gearSlot, string jsonObject)
        {
            EtroItem item = new EtroItem(itemId, gearSlot);
            item.BuildMateriaFromEtroResponse(jsonObject);
            SetItems!.Add(item);
        }
    }

    public class EtroItem
    {
        public int Id { get; set; }
        public CharacterEquippedGearSlotIndex GearSlot { get; set; }
        public EtroItemMateria? Materia { get; set; }

        public EtroItem() { }
        public EtroItem(int itemId, CharacterEquippedGearSlotIndex gearSlot)
        {
            Id = itemId;
            GearSlot = gearSlot;
            Materia = null;
        }

        public void BuildMateriaFromEtroResponse(string jsonObject)
        {
            Svc.Log.Debug($"Create materia from: {jsonObject}");
            dynamic? obj = JsonConvert.DeserializeObject(jsonObject);
            if (obj == null) { return; }
            if (obj["materia"] == null) { return; }

            switch (GearSlot)
            {
                case CharacterEquippedGearSlotIndex.RightRing:
                    AddMateria(obj, $"{Id}R");
                    break;
                case CharacterEquippedGearSlotIndex.LeftRing:
                    AddMateria(obj, $"{Id}L");
                    break;
                default:
                    AddMateria(obj, Id.ToString());
                    break;
            }
        }

        private void AddMateria(dynamic obj, string itemId)
        {
            Svc.Log.Debug($"Add materia: {obj["materia"][itemId]}");
            if (obj["materia"][itemId] == null) Materia = null;
            EtroItemMateria itemMateria = new EtroItemMateria(obj["materia"][itemId]);
            Materia = itemMateria;
        }
    }

    public class EtroItemMateria
    {
        public int MateriaSlot1 { get; set; }
        public int MateriaSlot2 { get; set; }
        public int MateriaSlot3 { get; set; }
        public int MateriaSlot4 { get; set; } 
        public int MateriaSlot5 { get; set; }

        public EtroItemMateria() { }
        public EtroItemMateria(dynamic materiaObject) 
        {
            MateriaSlot1 = ((JValue) materiaObject["1"]).Value<int?>().GetValueOrDefault();
            MateriaSlot2 = ((JValue)materiaObject["2"]).Value<int?>().GetValueOrDefault();
            MateriaSlot3 = ((JValue)materiaObject["3"]).Value<int?>().GetValueOrDefault();
            MateriaSlot4 = ((JValue)materiaObject["4"]).Value<int?>().GetValueOrDefault();
            MateriaSlot5 = ((JValue)materiaObject["5"]).Value<int?>().GetValueOrDefault();
        }
    }
}
