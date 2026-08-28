namespace SpectralXGLX.BWP
{
    public static class SpectralPropDefs
    {
        public static readonly Dictionary<string, ISpectralProps> All = new()
        {
            { "Tree",          new SpectralPropDef("Tree",          164f, 188f, 1.2f) },
            { "Rock",          new SpectralPropDef("Rock",          74f,  74f,  0.5f) },
            { "FenceBroken",   new SpectralPropDef("FenceBroken",   100f, 64f,  1.0f) },

            { "TorchNew01",    new SpectralPropDef("TorchNew01",    48f,  64f,  0.3f) },
            { "Chest",         new SpectralPropDef("Chest",         48f,  48f,  0.3f) },
            { "GStone",        new SpectralPropDef("GStone",        36f,  48f,  0.3f) },
            { "GStoneCross",   new SpectralPropDef("GStoneCross",   36f,  48f,  0.3f) },
            { "Mushroom",      new SpectralPropDef("Mushroom",      36f,  36f,  0.2f) },
            { "Skelcorpse001", new SpectralPropDef("Skelcorpse001", 112f, 48f,  0.5f) },
            { "Grass01",       new SpectralPropDef("Grass01",       32f,  32f,  0.2f) },
            { "SkullONStick",  new SpectralPropDef("SkullONStick",  48f,  64f,  0.3f) },
            { "Urn",           new SpectralPropDef("Urn",           36f,  36f,  0.2f) },
            { "Rose01",        new SpectralPropDef("Rose01",        16f,  16f,  0.15f) },
            { "Barrel01",      new SpectralPropDef("Barrel01",      64f,  64f,  0.4f) },
            { "Bush01",        new SpectralPropDef("Bush01",        88f,  88f,  0.6f) },
               { "STree",           new SpectralPropDef("STree",          164f, 188f, 1.2f) },
    { "SRock001",    new SpectralPropDef("SRock001",          74f,  74f,  0.5f) },
    { "Sbrokenfence01",       new SpectralPropDef("Sbrokenfence01",   100f, 64f,  1.0f) },
     { "SBush01",        new SpectralPropDef("SBush01",        88f,  88f,  0.6f) },
            // ── Town buildings — sized against the same 84px=1 unit baseline ──
            { "Tavern001",     new SpectralPropDef("Tavern001",     630f, 402f, 4.0f) },
            { "House001",      new SpectralPropDef("House001",      440f, 335f, 3.0f) },
            { "Fountain001",   new SpectralPropDef("Fountain001",   128f, 128f, 1.0f) },
            { "Shrine001",     new SpectralPropDef("Shrine001",     128f, 128f, 1.0f) },
        };

        public static Dictionary<string, int> ForestDictionary() => new()
        {
            { "Tree", 75 }, { "Rock", 50 }, { "FenceBroken", 10 }, { "TorchNew01", 2 },
            { "Chest", 2 }, { "GStone", 4 }, { "GStoneCross", 4 }, { "Mushroom", 4 },
            { "Skelcorpse001", 1 }, { "Grass01", 15 }, { "SkullONStick", 3 },
            { "Urn", 3 }, { "Rose01", 4 }, { "Barrel01", 3 }, { "Bush01", 35 },
        };

        public static Dictionary<string, int> TownDictionary() => new()
{
    { "Tree", 35},
    { "Rock", 25 },
    { "FenceBroken", 20 },
    { "TorchNew01", 5 },
    { "Chest", 2 },
    { "GStone", 5 },
    { "GStoneCross", 5 },
    { "Mushroom", 25 },
    { "Skelcorpse001", 0 },
    { "Grass01", 250 },
    { "SkullONStick", 0 },
    { "Urn", 0 },
    { "Rose01", 60 },
    { "Barrel01", 15 },
    { "Bush01", 40 }
};
        public static Dictionary<string, int> DarkForestDictionary() => new()
{
    { "Tree", 100 },
    { "Rock", 45 },
    { "FenceBroken", 10 },
    { "TorchNew01", 5 },
    { "Chest", 4 },
    { "GStone", 10 },
    { "GStoneCross", 10 },
    { "Mushroom", 10 },
    { "Skelcorpse001", 5 },
    { "Grass01", 50 },
    { "SkullONStick", 10 },
    { "Urn", 10 },
    { "Rose01", 0 },
    { "Barrel01", 0 },
    { "Bush01", 25 }
};
        public static Dictionary<string, int> SnowForestDictionary() => new()
{
    { "STree", 50},
    { "SRock001", 25 },
    { "Sbrokenfence01", 15 },
    { "TorchNew01", 8 },
    { "Chest", 6 },
    { "GStone", 12 },
    { "GStoneCross", 12 },
    { "Mushroom", 20 },
    { "Skelcorpse001", 0 },
    { "Grass01", 25},
    { "SkullONStick", 0 },
    { "Urn", 12 },
    { "Rose01", 15 },
    { "Barrel01", 12 },
    { "SBush01", 25 }
};
        public static Dictionary<string, int> GYDictionary() => new()
{
    { "Tree", 25 },
    { "Rock", 15 },
    { "FenceBroken", 15 },
    { "TorchNew01", 0 },
    { "Chest", 2 },
    { "GStone", 50 },
    { "GStoneCross", 65 },
    { "Mushroom", 25 },
    { "Skelcorpse001", 8 },
    { "Grass01", 50 },
    { "SkullONStick", 20 },
    { "Urn", 5 },
    { "Rose01", 5 },
    { "Barrel01", 0 },
    { "Bush01", 25 }
};
        public static Dictionary<string, int> CaveDictionary() => new()
{
    { "Tree", 0},
    { "Rock", 25 },
    { "FenceBroken", 15 },
    { "TorchNew01", 15 },
    { "Chest", 5 },
    { "GStone", 20 },
    { "GStoneCross", 20 },
    { "Mushroom", 45 },
    { "Skelcorpse001", 10 },
    { "Grass01", 0 },
    { "SkullONStick", 25 },
    { "Urn", 5 },
    { "Rose01", 0 },
    { "Barrel01", 5 },
    { "Bush01", 0 }
};


    }
}