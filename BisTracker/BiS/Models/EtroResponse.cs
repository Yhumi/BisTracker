using BisTracker.RawInformation.Character;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BisTracker.BiS.Models
{
    public class EtroApiReponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("materia")]
        public Dictionary<string, EtroApiMateria?> Materia { get; set; }

        [JsonProperty("food")]
        public int? Food { get; set; }

        [JsonProperty("medicine")]
        public int? Medicine { get; set; }

        [JsonProperty("job")]
        public int? Job { get; set; }


        [JsonProperty("weapon")]
        public int? Weapon { get; set; }

        [JsonProperty("offHand")]
        public int? OffHand { get; set; }


        [JsonProperty("head")]
        public int? Head { get; set; }

        [JsonProperty("body")]
        public int? Body { get; set; }

        [JsonProperty("hands")]
        public int? Hands { get; set; }

        [JsonProperty("legs")]
        public int? Legs { get; set; }

        [JsonProperty("feet")]
        public int? Feet { get; set; }


        [JsonProperty("ears")]
        public int? Ears { get; set; }

        [JsonProperty("neck")]
        public int? Neck { get; set; }

        [JsonProperty("wrists")]
        public int? Wrists { get; set; }

        [JsonProperty("fingerL")]
        public int? RingL { get; set; }

        [JsonProperty("fingerR")]
        public int? RingR { get; set; }
    }

    public class EtroApiMateria
    {
        [JsonProperty("1")]
        public int? Materia1 { get; set; }

        [JsonProperty("2")]
        public int? Materia2 { get; set; }

        [JsonProperty("3")]
        public int? Materia3 { get; set; }  

        [JsonProperty("4")] 
        public int? Materia4 { get; set; }

        [JsonProperty("5")]
        public int? Materia5 { get; set; }
    }

    public class EtroResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Food { get; set; } = null;
        public int? Medicine { get; set; } = null;
        public int? Job { get; set; } = null; 

        public bool Error = false;

        public List<EtroItem>? SetItems { get; set; } = null;

        public EtroResponse() { }

        public EtroResponse(bool error)
        {
            Error = error;
        }

        public EtroResponse(EtroApiReponse apiReponse)
        {
            Id = apiReponse.Id;
            Name = apiReponse.Name;

            Food = apiReponse.Food;
            Medicine = apiReponse.Medicine;

            Job = apiReponse.Job;

            SetItems = new();

            AddEtroItem(apiReponse.Weapon, CharacterEquippedGearSlotIndex.MainHand, apiReponse.Materia);
            AddEtroItem(apiReponse.OffHand, CharacterEquippedGearSlotIndex.OffHand, apiReponse.Materia);

            AddEtroItem(apiReponse.Head, CharacterEquippedGearSlotIndex.Head, apiReponse.Materia);
            AddEtroItem(apiReponse.Body, CharacterEquippedGearSlotIndex.Body, apiReponse.Materia);
            AddEtroItem(apiReponse.Hands, CharacterEquippedGearSlotIndex.Gloves, apiReponse.Materia);
            AddEtroItem(apiReponse.Legs, CharacterEquippedGearSlotIndex.Legs, apiReponse.Materia);
            AddEtroItem(apiReponse.Feet, CharacterEquippedGearSlotIndex.Feet, apiReponse.Materia);

            AddEtroItem(apiReponse.Ears, CharacterEquippedGearSlotIndex.Ears, apiReponse.Materia);
            AddEtroItem(apiReponse.Neck, CharacterEquippedGearSlotIndex.Neck, apiReponse.Materia);
            AddEtroItem(apiReponse.Wrists, CharacterEquippedGearSlotIndex.Wrists, apiReponse.Materia);
            AddEtroItem(apiReponse.RingR, CharacterEquippedGearSlotIndex.RightRing, apiReponse.Materia);
            AddEtroItem(apiReponse.RingL, CharacterEquippedGearSlotIndex.LeftRing, apiReponse.Materia);
        }

        private void AddEtroItem(int? itemId, CharacterEquippedGearSlotIndex gearSlot, Dictionary<string, EtroApiMateria?> materia)
        {
            EtroItem item = new EtroItem(itemId, gearSlot);
            if (materia != null)
                item.BuildMateriaFromEtroApiResponse(materia);
            else
                item.Materia = new();

            if (SetItems == null) { SetItems = new(); }
            SetItems.Add(item);
        }
    }

    public class EtroItem
    {
        public int Id { get; set; }
        public CharacterEquippedGearSlotIndex GearSlot { get; set; }
        public EtroItemMateria? Materia { get; set; }

        public EtroItem() { }
        public EtroItem(int? itemId, CharacterEquippedGearSlotIndex gearSlot)
        {
            Id = itemId ?? 0;
            GearSlot = gearSlot;
            Materia = null;
        }

        public void BuildMateriaFromEtroApiResponse(Dictionary<string, EtroApiMateria?> materia)
        {
            if (materia == null) return;
                
            switch (GearSlot)
            {
                case CharacterEquippedGearSlotIndex.RightRing:
                    AddMateria(materia, $"{Id}R");
                    break;
                case CharacterEquippedGearSlotIndex.LeftRing:
                    AddMateria(materia, $"{Id}L");
                    break;
                default:
                    AddMateria(materia, Id.ToString());
                    break;
            }
        }

        private void AddMateria(Dictionary<string, EtroApiMateria?> materia, string itemId)
        {
            if (!materia.ContainsKey(itemId))
            {
                Materia = new();
                return;
            }

            Materia = new (materia[itemId]);
        }
    }

    public class EtroItemMateria
    {
        public int MateriaSlot1 { get; set; } = 0;
        public int MateriaSlot2 { get; set; } = 0;
        public int MateriaSlot3 { get; set; } = 0;
        public int MateriaSlot4 { get; set; } = 0;
        public int MateriaSlot5 { get; set; } = 0;

        public EtroItemMateria() { }

        public EtroItemMateria(EtroApiMateria? apiMateria)
        {
            if (apiMateria == null) return;
            MateriaSlot1 = apiMateria.Materia1 ?? 0;
            MateriaSlot2 = apiMateria.Materia2 ?? 0;
            MateriaSlot3 = apiMateria.Materia3 ?? 0;
            MateriaSlot4 = apiMateria.Materia4 ?? 0;
            MateriaSlot5 = apiMateria.Materia5 ?? 0;
        }
    }
}
