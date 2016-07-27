using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Window;
using Xamarin.Forms;

namespace PokeGrinder
{
    public class App : Application
    {
        public static ISettings ClientSettings;
        private static int Currentlevel = -1;
        private static int TotalExperience = 0;
        private static int TotalPokemon = 0;
        private static DateTime TimeStarted = DateTime.Now;
        public static DateTime InitSessionDateTime = DateTime.Now;
        private static double Speed = 60; //in km/h

        Client client;
        public static double GetRuntime()
        {
            return ((DateTime.Now - TimeStarted).TotalSeconds) / 3600;
        }

        public App()
        {
            ClientSettings = new Settings();
            client = new Client(ClientSettings);

            Button button = new Button
            {
                Text = "Click Me!",
                Font = Font.SystemFontOfSize(NamedSize.Large),
                BorderWidth = 1,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.CenterAndExpand
            };
            button.Clicked += OnButtonClicked;

            // The root page of your application
            var content = new ContentPage
            {
                Title = "PokeGrinder",
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        new Label {
                            HorizontalTextAlignment = TextAlignment.Center,
                            Text = "Welcome to Xamarin Forms!"
                        }, button
                    }
                }
            };

            MainPage = new NavigationPage(content);

        }

        async void OnButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await client.DoGoogleLogin();
                await client.SetServer();

                var inventory = await client.GetInventory();

                var allpokemons =
                     inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                     .Where(p => p != null && p?.PokemonId > 0).ToList();

                var profile = await client.GetProfile();
                var settings = await client.GetSettings();


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected async override void OnStart()
        {
            // Handle when your app starts


        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
