using GTANetworkAPI;
using MySql.Data.MySqlClient;
using System.Data;

public class Events : Script
{
    [ServerEvent(Event.ResourceStart)]
    public void OnResourceStart()
    {
    }

    [ServerEvent(Event.PlayerSpawn)]
    public void OnPlayerSpawn(Player player)
    {
        player.Position = new Vector3(-1847.6251, 4571.128, 5.5569506);
        player.Rotation = new Vector3(0, 0, -9.241396);
        player.Dimension = player.Id;
    }

}