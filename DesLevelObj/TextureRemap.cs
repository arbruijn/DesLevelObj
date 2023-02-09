using System.Collections.Generic;

namespace DesLevelObj
{
    public class TextureRemapRoot
    {
        public TextureRemapRoot()
        {
            TextureRemap = new List<TextureRemapEntry>();
        }

        public IList<TextureRemapEntry> TextureRemap { get; set; }
    }

    public class TextureRemapEntry
    {
        public TextureRemapEntry()
        {
            Textures = new List<string>();
            RemapTo = new TextureRemapTo();
        }

        public IList<string> Textures { get; set; }
        public TextureRemapTo RemapTo { get; set; }
    }

    public class TextureRemapTo
    {
        public string Material { get; set; }
        public string Texture { get; set; }
    }
}
