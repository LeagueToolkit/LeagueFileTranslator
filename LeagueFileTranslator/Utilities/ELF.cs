namespace LeagueFileTranslator.Utilities
{
    public static class ELF
    {
        public static uint Hash(string toHash)
        {
            toHash = toHash.ToLower();

            uint hash = 0;
            uint high = 0;
            for (int i = 0; i < toHash.Length; i++)
            {
                hash = (hash << 4) + ((byte)toHash[i]);

                if ((high = hash & 0xF0000000) != 0)
                {
                    hash ^= (high >> 24);
                }

                hash &= ~high;
            }

            return hash;
        }
    }
}
