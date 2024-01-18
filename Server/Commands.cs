using GTANetworkAPI;

public class Commands : Script
{
    [Command("getpos")]
    public void Cmd_GetPos(Player player)
    {
        Vector3 playerPosition = player.Position;
        Vector3 playerRotation = player.Rotation;
        NAPI.Util.ConsoleOutput($"{playerPosition.X}, {playerPosition.Y}, {playerPosition.Z}");
        NAPI.Util.ConsoleOutput($"{playerRotation.X}, {playerRotation.Y}, {playerRotation.Z}");
    }
}