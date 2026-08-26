namespace Settings
{
    // Banked currency, stored in the shared save profile.
    public static class CurrencySettings
    {
        public static int Balance
        {
            get => SaveSystem.Data.currencyBalance;
            set => SaveSystem.Data.currencyBalance = value;
        }

        // The setter only writes to the loaded profile, so a change is lost unless this is called.
        public static void Save() => SaveSystem.Save();
    }
}