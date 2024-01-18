using GTANetworkAPI;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public class RemoteEvents : Script
{
    private readonly Crypto crypto = new Crypto();

    [RemoteEvent("CLIENT:SERVER::CLIENT_CREATE_WAYPOINT")]
    public void OnClientCreateWaypoint(Player player, float posX, float posY, float posZ)
    {
        player.Position = new Vector3(posX, posY, posZ);
    }

    [RemoteEvent("CLIENT:SERVER::REGISTER_BUTTON_CLICKED")]
    public async void OnCefRegisterButtonClicked(Player player, string username, string password, string email)
    {
        using (var context = new MyDbContext())
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null)
            {
                NAPI.Util.ConsoleOutput("Такой пользователь уже существует");
                NAPI.Task.Run(() =>
                {
                    NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::REGISTER_USER", true);
                }, 10);
            } 
            else
            {
                string salt = Convert.ToBase64String(Crypto.GenerateSalt(16));
                string hashPassword = crypto.Ecnryption(password, salt);
                ulong socialClubId = await GetSocialClubIdAsync(player);
                var newUser = new User
                {
                    Username = username,
                    Password = hashPassword,
                    Email = email,
                    Salt = salt,
                    SocialClubId = socialClubId
                };

                context.Users.Add(newUser);

                await context.SaveChangesAsync();
                NAPI.Util.ConsoleOutput("Сохранил пользователя");

                CreateCharacter(player, username);
            }
        }
    }

    //TODO рассмотреть вариант хранения хэша пароля в кэше, что бы не делать запросы в БД
    [RemoteEvent("CLIENT:SERVER::LOGIN_BUTTON_CLICKED")]
    public async void OnCefLoginButtonClicked(Player player, string username, string password)
    {
        using (var context = new MyDbContext())
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) 
            {
                NAPI.Util.ConsoleOutput("Такого пользователя не существует");
                NAPI.Task.Run(() =>
                {
                    NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::LOGIN_USER", true);
                }, 10);
            } 
            else
            {
                NAPI.Util.ConsoleOutput("Пользователь найден, проверяю пароль");
                var hashPass = crypto.Ecnryption(password, user.Salt);

                if (hashPass != user.Password)
                {
                    NAPI.Util.ConsoleOutput("Пароли не совпадают");
                    NAPI.Task.Run(() =>
                    {
                        NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::LOGIN_USER", true);
                    }, 10);
                }
                else
                {
                    NAPI.Util.ConsoleOutput("Пароли совпадают");
                    if (user.Name == null || user.Name.Length == 0)
                    {
                        CreateCharacter(player, username);
                    }
                    else
                    {
                        float[] faceFeatures =
                        {
                              (float)user.UserCustomization.NoseWidth,       (float)user.UserCustomization.NoseHeight,   (float)user.UserCustomization.NoseLength, (float)user.UserCustomization.NoseBridge,       (float)user.UserCustomization.NoseTip,
                              (float)user.UserCustomization.NoseBridgeShift, (float)user.UserCustomization.BrowHeight,   (float)user.UserCustomization.BrowWidth,   (float)user.UserCustomization.CheckBoneHeight, (float)user.UserCustomization.CheckBoneWidth,
                              (float)user.UserCustomization.CheckWidth,      (float)user.UserCustomization.Eyes,         (float)user.UserCustomization.Lips,       (float)user.UserCustomization.JawWidth,         (float)user.UserCustomization.JawHeight,
                              (float)user.UserCustomization.ChinLength,      (float)user.UserCustomization.ChinPosition, (float)user.UserCustomization.ChinWidth,  (float)user.UserCustomization.ChinShape,        (float)user.UserCustomization.NeckWidth
                        };

                        NAPI.Task.Run(() =>
                        {
                            NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::LOGIN_USER", false);
                            player.SetData<string>("player_username", user.Username);
                            NAPI.Util.ConsoleOutput("Работаю с юзером: " + user.ToString());
                            SetPersonCustomization(player, 
                                (byte)user.UserCustomization.Parent1, 
                                (byte)user.UserCustomization.Parent2, 
                                (float)user.UserCustomization.ShapeMix,
                                (float)user.UserCustomization.SkinMix,
                                faceFeatures,
                                (byte)user.UserCustomization.EyeColor,
                                (byte)user.UserCustomization.FirstHairColor,
                                (byte)user.UserCustomization.SecondHairColor,
                                user.Gender == "mail");

                            player.SetClothes(11, user.UserCustomization.Tops, 0);
                            player.SetClothes(3, user.UserCustomization.Torso, 0);
                            player.SetClothes(8, user.UserCustomization.UnderShirt, 0);
                            player.SetClothes(4, user.UserCustomization.Legs, 0);
                            player.SetClothes(6, user.UserCustomization.Shoes, 0);
                            player.SetClothes(2, (byte)user.UserCustomization.HairStyle, 0);
                        }, 10);
                    }
                }


            }
        }
    }

    [RemoteEvent("CLIENT:SERVER::PERSON_CREATE_BUTTON_CLICKED")]
    public async Task OnCefPersonCreateButtonClicked(Player player, string name, string secondName, string age, string gender, string customizationJson, int tops, int torso,
        int underShirt, int legs, int shoes)
    {
        if (player.HasData("player_username"))
        {
            if (string.IsNullOrEmpty(customizationJson)) return;
            dynamic customizationInfo = JsonConvert.DeserializeObject(customizationJson);

            string username = player.GetData<string>("player_username");
            using (var context = new MyDbContext())
            {
                var user = await context.Users
                    .Include(u => u.UserCustomization)
                    .FirstOrDefaultAsync(u => u.Username == username);
                NAPI.Util.ConsoleOutput("Персонаж получен " + user.ToString());
                user.Name = name;
                user.SecondName = secondName;
                user.Age = age;
                user.Gender = gender;

                user.UserCustomization = new UserCustomization();
                user.UserCustomization.Parent1 = (byte)customizationInfo.firstParent;
                user.UserCustomization.Parent2 = (byte)customizationInfo.secondParent;
                user.UserCustomization.ShapeMix = (float)customizationInfo.shapeMix;
                user.UserCustomization.SkinMix = (float)customizationInfo.skinMix;
                user.UserCustomization.NoseWidth = (float)customizationInfo.noseWidth;
                user.UserCustomization.NoseHeight = (float)customizationInfo.noseHeight;
                user.UserCustomization.NoseLength = (float)customizationInfo.noseLength;
                user.UserCustomization.NoseBridge = (float)customizationInfo.noseBridge;
                user.UserCustomization.NoseTip = (float)customizationInfo.noseTip;
                user.UserCustomization.NoseBridgeShift = (float)customizationInfo.noseBridgeShift;
                user.UserCustomization.BrowHeight = (float)customizationInfo.browHeight;
                user.UserCustomization.BrowWidth = (float)customizationInfo.browWidth;
                user.UserCustomization.CheckBoneHeight = (float)customizationInfo.checkBoneHeight;
                user.UserCustomization.CheckBoneWidth = (float)customizationInfo.checkBoneWidth;
                user.UserCustomization.CheckWidth = (float)customizationInfo.checkWidth;
                user.UserCustomization.Eyes = (float)customizationInfo.eyes;
                user.UserCustomization.Lips = (float)customizationInfo.lips;
                user.UserCustomization.JawWidth = (float)customizationInfo.jawWidth;
                user.UserCustomization.ChinLength = (float)customizationInfo.chinLength;
                user.UserCustomization.ChinPosition = (float)customizationInfo.chinPosition;
                user.UserCustomization.ChinWidth = (float)customizationInfo.chinWidth;
                user.UserCustomization.ChinShape = (float)customizationInfo.chinShape;
                user.UserCustomization.NeckWidth = (float)customizationInfo.neckWidth;

                user.UserCustomization.EyeColor = (byte)customizationInfo.eyeColor;
                user.UserCustomization.FirstHairColor = (byte)customizationInfo.firstHairColor;
                user.UserCustomization.SecondHairColor = (byte)customizationInfo.secondHairColor;
                 
                user.UserCustomization.Tops = customizationInfo.tops;
                user.UserCustomization.Torso = customizationInfo.torso;
                user.UserCustomization.UnderShirt = customizationInfo.underShirt;
                user.UserCustomization.Legs = customizationInfo.legs;
                user.UserCustomization.Shoes = customizationInfo.shoes;
                user.UserCustomization.HairStyle = customizationInfo.hairStyle;

                NAPI.Util.ConsoleOutput("перед сохранением " + user.ToString());

                await context.SaveChangesAsync();

                NAPI.Task.Run(() =>
                {
                    SetPersonCustomization(player, customizationInfo, gender);
                    player.SetClothes(11, tops, 0);
                    player.SetClothes(3, torso, 0);
                    player.SetClothes(8, underShirt, 0);
                    player.SetClothes(4, legs, 0);
                    player.SetClothes(6, shoes, 0);
                    player.SetClothes(2, (byte)customizationInfo.hairStyle, 0);
                    NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::CREATE_PERSON");
                    NAPI.Util.ConsoleOutput("Данные по персонажу сохранены ТЕСТ");
                }, 10);
            }
        }
    }

    [RemoteEvent("CLIENT:SERVER::PERSON_CREATE_GENDER_SWITCH_BUTTON_CLICKED")]
    public void OnCefPersonCreateGenderSwitchButtonClicked(Player player, string gender)
    {
        NAPI.Player.SetPlayerSkin(player, gender.ToLower() == "male" ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);
        NAPI.Task.Run(() =>
        {
            NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::UPDATE_SAVED_CUSTOMIZATION", gender);
            NAPI.Util.ConsoleOutput("Данные по персонажу сохранены");
        }, 10);
    }


    private async Task<ulong> GetSocialClubIdAsync(Player player)
    {
        var tcs = new TaskCompletionSource<ulong>();

        NAPI.Task.Run(() =>
        {
            try
            {
                ulong socialClubId = player.SocialClubId;
                tcs.SetResult(socialClubId);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    private void SetPersonCustomization(Player player, dynamic customizationInfo, string gender)
    {
        byte firstParent = (byte)customizationInfo.firstParent;
        byte secondParent = (byte)customizationInfo.secondParent;
        float shapeMix = (float)customizationInfo.shapeMix;
        float skinMix = (float)customizationInfo.skinMix;

        float[] faceFeatures =
        {
            (float)customizationInfo.noseWidth, (float)customizationInfo.noseHeight, (float)customizationInfo.noseLength, (float)customizationInfo.noseBridge, (float)customizationInfo.noseTip,
            (float)customizationInfo.noseBridgeShift, (float)customizationInfo.browHeight, (float)customizationInfo.broWidth, (float)customizationInfo.checkBoneHeight, (float)customizationInfo.checkBoneWidth,
            (float)customizationInfo.checkWidth, (float)customizationInfo.eyes, (float)customizationInfo.lips, (float)customizationInfo.jawWidth, (float)customizationInfo.jawHeight,
            (float)customizationInfo.chinLength, (float)customizationInfo.chinPosition, (float)customizationInfo.chinWidth, (float)customizationInfo.chinShape, (float)customizationInfo.neckWidth
        };

        SetPersonCustomization(player,
            firstParent,
            secondParent,
            shapeMix,
            skinMix,
            faceFeatures,
            (byte)customizationInfo.eyeColor,
            (byte)customizationInfo.firstHairColor,
            (byte)customizationInfo.secondHairColor,
            gender == "mail");
    }

    private void SetPersonCustomization(Player player, byte first, byte second, float shapeMix, float skinMix, 
        float[] faceFeatures, byte eyeColor, byte firstHairColor, byte secondHairColor, bool gender)
    {
        HeadBlend headBlend = new HeadBlend()
        {
            ShapeFirst = first,
            ShapeSecond = second,
            ShapeThird = 0,
            SkinFirst = first,
            SkinSecond = second,
            SkinThird = 0,
            ShapeMix = shapeMix,
            SkinMix = skinMix,
            ThirdMix = 0
        };

        Dictionary<int, HeadOverlay> headOverlays = new Dictionary<int, HeadOverlay>();

        player.SetCustomization(gender, headBlend, eyeColor, firstHairColor, secondHairColor, faceFeatures, headOverlays, new Decoration[] { });
    }

    private void CreateCharacter(Player player, string username)
    {
        NAPI.Task.Run(() =>
        {
            NAPI.ClientEvent.TriggerClientEvent(player, "SERVER:CLIENT::REGISTER_USER", false);
            player.Position = new Vector3(-1642.6818, -1092.0542, 13.52933);
            player.Rotation = new Vector3(0, 0, 50.111305);
            player.Dimension = player.Id;
            player.SetData<string>("player_username", username);
        }, 10);
    }
}