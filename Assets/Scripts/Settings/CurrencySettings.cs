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

        public static void Save() => SaveSystem.Save();
    }
}
