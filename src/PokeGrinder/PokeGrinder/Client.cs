#region

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Helpers;
using PokemonGo.RocketAPI.Login;
using AllEnum;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using PokemonGo.RocketAPI.Exceptions;
using System.Text;
using System.IO;

#endregion

namespace PokemonGo.RocketAPI
{
    public class Client
    {
        private readonly HttpClient _httpClient;
        private ISettings _settings;
        private string _accessToken;
        private string _apiUrl;
        private AuthType _authType = AuthType.Google;

        private double _currentLat;
        private double _currentLng;
        private Request.Types.UnknownAuth _unknownAuth;
        public static string AccessToken { get; set; } = string.Empty;

        public Client(ISettings settings)
        {
            _settings = settings;
            SetCoordinates(_settings.DefaultLatitude, _settings.DefaultLongitude);

            //Setup HttpClient and create default headers
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false
            };

            _httpClient = new HttpClient(new RetryHandler(handler));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Niantic App");
            //"Dalvik/2.1.0 (Linux; U; Android 5.1.1; SM-G900F Build/LMY48G)");
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type",
                "application/x-www-form-urlencoded");
        }


        public async Task DoGoogleLogin()
        {
            _authType = AuthType.Google;
            GoogleLogin.TokenResponseModel tokenResponse = null;

            if (string.IsNullOrEmpty(_settings.GoogleRefreshToken) && string.IsNullOrEmpty(AccessToken))
            {
                var deviceCode = await GoogleLogin.GetDeviceCode();
                tokenResponse = await GoogleLogin.GetAccessToken(deviceCode);
                _accessToken = tokenResponse.id_token;
                Debug.WriteLine($"Put RefreshToken in settings for direct login: {tokenResponse.refresh_token}");
                //ColoredConsoleWrite(ConsoleColor.White, $"Put RefreshToken in settings for direct login: {tokenResponse.refresh_token}");
                _settings.GoogleRefreshToken = tokenResponse.refresh_token;
                AccessToken = tokenResponse.refresh_token;
            }
            else
            {
                if (!string.IsNullOrEmpty(_settings.GoogleRefreshToken))
                    tokenResponse = await GoogleLogin.GetAccessToken(_settings.GoogleRefreshToken);
                else
                    tokenResponse = await GoogleLogin.GetAccessToken(AccessToken);
                _accessToken = tokenResponse.id_token;
            }
        }

        public async Task DoPtcLogin(string username, string password)
        {
            try
            {
                _accessToken = await PtcLogin.GetAccessToken(username, password);
                _authType = AuthType.Ptc;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                //ColoredConsoleWrite(ConsoleColor.White, "Json Reader Exception - Server down? - Restarting"); 
                Debug.WriteLine("Json Reader Exception - Server down? - Restarting");
                DoPtcLogin(username, password);
                //ColoredConsoleWrite(ConsoleColor.White, "Json Reader Exception - Server down? - Restarting"); DoPtcLogin(username, password);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString() + "Exception - Please report - Restarting");

                //ColoredConsoleWrite(ConsoleColor.White, ex.ToString() + "Exception - Please report - Restarting");
                DoPtcLogin(username, password);
            }
        }

        public async Task<EvolvePokemonOut> EvolvePokemon(ulong pokemonId)
        {
            var customRequest = new EvolvePokemon
            {
                PokemonId = pokemonId
            };

            var releasePokemonRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 30,
                new Request.Types.Requests
                {
                    Type = (int)RequestType.EVOLVE_POKEMON,
                    Message = customRequest.ToByteString()
                });
            return
                await
                    _httpClient.PostProtoPayload<Request, EvolvePokemonOut>($"https://{_apiUrl}/rpc",
                        releasePokemonRequest);
        }

        public async Task<GetInventoryResponse> GetInventory()
        {
            var inventoryRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 30,
                RequestType.GET_INVENTORY);
            return
                await
                    _httpClient.PostProtoPayload<Request, GetInventoryResponse>($"https://{_apiUrl}/rpc",
                        inventoryRequest);
        }

        public async Task<GetPlayerResponse> GetProfile()
        {
            var profileRequest = RequestBuilder.GetInitialRequest(_accessToken, _authType, _currentLat, _currentLng, 10,
                new Request.Types.Requests { Type = (int)RequestType.GET_PLAYER });
            return
                await _httpClient.PostProtoPayload<Request, GetPlayerResponse>($"https://{_apiUrl}/rpc", profileRequest);
        }

        public async Task<DownloadSettingsResponse> GetSettings()
        {
            var settingsRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 10,
                RequestType.DOWNLOAD_SETTINGS);
            return
                await
                    _httpClient.PostProtoPayload<Request, DownloadSettingsResponse>($"https://{_apiUrl}/rpc",
                        settingsRequest);
        }

        /*num Holoholo.Rpc.Types.FortSearchOutProto.Result {
         NO_RESULT_SET = 0;
         SUCCESS = 1;
         OUT_OF_RANGE = 2;
         IN_COOLDOWN_PERIOD = 3;
         INVENTORY_FULL = 4;
        }*/


        private void SetCoordinates(double lat, double lng)
        {
            _currentLat = lat;
            _currentLng = lng;
//            _settings.DefaultLatitude = lat;
//            _settings.DefaultLongitude = lng;
        }

        public async Task SetServer()
        {
            var serverRequest = RequestBuilder.GetInitialRequest(_accessToken, _authType, _currentLat, _currentLng, 10,
                RequestType.GET_PLAYER, RequestType.GET_HATCHED_OBJECTS, RequestType.GET_INVENTORY,
                RequestType.CHECK_AWARDED_BADGES, RequestType.DOWNLOAD_SETTINGS);
            var serverResponse = await _httpClient.PostProto(Resources.RpcUrl, serverRequest);
            _unknownAuth = new Request.Types.UnknownAuth
            {
                Unknown71 = serverResponse.Auth.Unknown71,
                Timestamp = serverResponse.Auth.Timestamp,
                Unknown73 = serverResponse.Auth.Unknown73
            };

            _apiUrl = serverResponse.ApiUrl;
        }

        public async Task<TransferPokemonOut> TransferPokemon(ulong pokemonId)
        {
            var customRequest = new TransferPokemon
            {
                PokemonId = pokemonId
            };

            var releasePokemonRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 30,
                new Request.Types.Requests
                {
                    Type = (int)RequestType.RELEASE_POKEMON,
                    Message = customRequest.ToByteString()
                });
            return
                await
                    _httpClient.PostProtoPayload<Request, TransferPokemonOut>($"https://{_apiUrl}/rpc",
                        releasePokemonRequest);
        }

        public async Task<PlayerUpdateResponse> UpdatePlayerLocation(double lat, double lng)
        {
            SetCoordinates(lat, lng);
            var latlng = _currentLat + ":" + _currentLng;

            var customRequest = new Request.Types.PlayerUpdateProto
            {
                Lat = Utils.FloatAsUlong(_currentLat),
                Lng = Utils.FloatAsUlong(_currentLng)
            };

            var updateRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 10,
                new Request.Types.Requests
                {
                    Type = (int)RequestType.PLAYER_UPDATE,
                    Message = customRequest.ToByteString()
                });
            var updateResponse =
                await
                    _httpClient.PostProtoPayload<Request, PlayerUpdateResponse>($"https://{_apiUrl}/rpc", updateRequest);
            return updateResponse;
        }




        public async Task<IEnumerable<Item>> GetItemsToRecycle(ISettings settings, Client client)
        {
            var myItems = await GetItems(client);

            return myItems
                .Where(x => settings.ItemRecycleFilter.Any(f => f.Key == ((ItemId)x.Item_) && x.Count > f.Value))
                .Select(x => new Item { Item_ = x.Item_, Count = x.Count - settings.ItemRecycleFilter.Single(f => f.Key == (AllEnum.ItemId)x.Item_).Value, Unseen = x.Unseen });
        }

        public async Task RecycleItems(Client client)
        {
            var items = await GetItemsToRecycle(_settings, client);

            foreach (var item in items)
            {
                var transfer = await RecycleItem((AllEnum.ItemId)item.Item_, item.Count);
                //ColoredConsoleWrite(ConsoleColor.DarkCyan, $"Recycled {item.Count}x {((AllEnum.ItemId)item.Item_).ToString().Substring(4)}");
                await Task.Delay(500);
            }
            await Task.Delay(_settings.RecycleItemsInterval * 1000);
            RecycleItems(client);
        }

        public async Task<Response.Types.Unknown6> RecycleItem(AllEnum.ItemId itemId, int amount)
        {
            var customRequest = new InventoryItemData.RecycleInventoryItem
            {
                ItemId = (AllEnum.ItemId)Enum.Parse(typeof(AllEnum.ItemId), itemId.ToString()),
                Count = amount
            };

            var releasePokemonRequest = RequestBuilder.GetRequest(_unknownAuth, _currentLat, _currentLng, 30,
                new Request.Types.Requests()
                {
                    Type = (int)RequestType.RECYCLE_INVENTORY_ITEM,
                    Message = customRequest.ToByteString()
                });
            return await _httpClient.PostProtoPayload<Request, Response.Types.Unknown6>($"https://{_apiUrl}/rpc", releasePokemonRequest);
        }

        public async Task<IEnumerable<Item>> GetItems(Client client)
        {
            var inventory = await client.GetInventory();
            return inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .Where(p => p != null);
        }
    }
}
