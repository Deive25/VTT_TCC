using System;
using System.Collections.Generic;

[Serializable]
public class CharacterRecord
{
    public string id;
    public string system;
    public string name;
    public string subText;
    public string statsStr;
    public string avatarFileName;
    public CharacterType characterType = CharacterType.Player;
    public CharacterState state = CharacterState.Active;
    public AvatarCropData avatarCrop = new AvatarCropData();
    public bool defaultRenderInProjection = true;
    public List<CharField> fields = new List<CharField>();
}
