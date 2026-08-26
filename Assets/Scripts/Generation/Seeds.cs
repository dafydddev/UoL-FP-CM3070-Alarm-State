namespace Generation
{
    // Derives RNG seeds from one base seed, one per subsystem and per level.
    // Systems sharing a master seed never replay each other's sequences.
    public static class Seeds
    {
        // One id per subsystem that owns its own RNG stream. Ids are what the streams are distinguished by.
        public const int Mission = 1;
        public const int Rooms = 2;
        public const int Cover = 3;
        public const int Keys = 4;
        public const int Distraction = 5;
        public const int Terrain = 6;
        public const int Tiles = 7;
        public const int MiniGames = 8;
        public const int Drops = 9;
        public const int Adaptive = 10;
        public const int Lasers = 11;
        public const int Alarm = 12;
        public const int Detail = 13;

        // Mixes the base seed with a subsystem id and an optional salt (e.g. the level number).
        // Based on research from the following sources:
        // - https://steveasleep.com/pcg32-the-perfect-prng-for-roguelikes.html
        // - https://prng.di.unimi.it/
        // - https://arvid.io/2018/07/02/better-cxx-prng/
        public static int For(int seed, int subsystem, int salt = 0)
        {
            unchecked
            {
                var h = (uint)seed;
                h ^= (uint)subsystem * 0x9E3779B9u;
                h ^= (uint)salt * 0x85EBCA6Bu;
                h ^= h >> 16;
                h *= 0x21F0AAADu;
                h ^= h >> 15;
                h *= 0x735A2D97u;
                h ^= h >> 15;
                return (int)h;
            }
        }
    }
}