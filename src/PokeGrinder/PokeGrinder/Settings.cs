#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using AllEnum;
using PokemonGo.RocketAPI.Enums;

#endregion

namespace PokemonGo.RocketAPI.Window
{
    public class Settings : ISettings
    {
        private static volatile Settings _instance;


        public string TransferType => "Duplicate";

        public int TransferCPThreshold => 0;
        public int TransferIVThreshold => 0;
        public string PtcUsername { get; }
        public bool EvolveAllGivenPokemons => false;


        public AuthType AuthType => AuthType.Google;


        public double DefaultLatitude
        {
            get { return double.Parse("-33.8595794"); }
            set {  }
        }


        public double DefaultLongitude
        {
            get { return double.Parse("151.2135318"); }
            set {  }
        }


        public string LevelOutput => "time";

        public int LevelTimeInterval => 600;

        public bool Recycler => true;

        ICollection<KeyValuePair<ItemId, int>> ISettings.ItemRecycleFilter => new[]
        {
            new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 20),
            new KeyValuePair<ItemId, int>(ItemId.ItemGreatBall, 50),
            new KeyValuePair<ItemId, int>(ItemId.ItemUltraBall, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemMasterBall, 200),
            new KeyValuePair<ItemId, int>(ItemId.ItemRazzBerry, 20),
            new KeyValuePair<ItemId, int>(ItemId.ItemRevive, 20),
            new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemSuperPotion, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemHyperPotion, 50),
            new KeyValuePair<ItemId, int>(ItemId.ItemMaxPotion, 100)
        };

        public int RecycleItemsInterval => 60;

        public string Language => "english";

        public string RazzBerryMode =>"cp";

        public double RazzBerrySetting => 500;

        public string GoogleRefreshToken
        {
            get { return "nope"; }
            set
            {
                
            }
        }

        public string PtcPassword { get; }
    }
}
