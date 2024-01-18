using System.ComponentModel.DataAnnotations.Schema;

[Table("users_customization")]
public class UserCustomization
{
    public ulong Id { get; set; }
    public int Parent1 { get; set; }
    public int Parent2 { get; set; }
    public double ShapeMix { get; set; }
    public double SkinMix { get; set; }
    public double NoseWidth { get; set; }
    public double NoseHeight { get; set; }
    public double NoseLength { get; set; }
    public double NoseBridge { get; set; }
    public double NoseTip { get; set; }
    public double NoseBridgeShift { get; set; }
    public double BrowHeight { get; set; }
    public double BrowWidth { get; set; }
    public double CheckBoneHeight { get; set; }
    public double CheckBoneWidth { get; set; }
    public double CheckWidth { get; set; }
    public double Eyes { get; set; }
    public double Lips { get; set; }
    public double JawWidth { get; set; }
    public double JawHeight { get; set; }
    public double ChinLength { get; set; }
    public double ChinPosition { get; set; }
    public double ChinShape { get; set; }
    public double NeckWidth { get; set; }
    public int EyeColor { get; set; }
    public int FirstHairColor { get; set; }
    public int SecondHairColor { get; set; }
    public int Tops { get; set; }
    public int Torso { get; set; }
    public int UnderShirt { get; set; }
    public int Legs { get; set; }
    public int Shoes { get; set; }
    public int HairStyle { get; set; }
    public double ChinWidth { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }
}
