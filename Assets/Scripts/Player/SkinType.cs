namespace Player
{
    // The looks the player can wear, one per skin the shop sells. Default is the one they start in.
    // Serialized by enum value in the save profile, so this is append-only: reordering would remap bought skins.
    public enum SkinType
    {
        Default,
        Solider,
        King,
        Unknown
    }
}