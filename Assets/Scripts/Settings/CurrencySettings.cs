using UnityEngine;

namespace Settings
{
    public static class CurrencySettings
    {
        private const string BalanceKey = "CurrencyBalance";

        public static int Balance
        {
            get => PlayerPrefs.GetInt(BalanceKey, 0);
            set => PlayerPrefs.SetInt(BalanceKey, value);
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
