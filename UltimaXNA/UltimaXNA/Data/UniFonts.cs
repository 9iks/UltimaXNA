﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UltimaXNA.Data
{
    internal sealed class UniCharacter
    {
        bool hasTexture
        {
            get
            {
                return (_textureData == null) ? false : true;
            }
        }
        public int XOffset = 0, YOffset = 0, Width = 0, Height = 0;
        public Color[] _textureData;

        public UniCharacter()
        {

        }

        public unsafe void WriteToBuffer(Color* rPtr, int dx, int dy, int linewidth, int maxHeight, int baseLine, bool isBold, bool isItalic, bool isUnderlined, Color color)
        {
            if (hasTexture)
            {
                fixed (Color* cPtr = _textureData)
                {
                    for (int iy = 0; (iy < Height) && (iy < maxHeight); iy++)
                    {
                        Color* src = ((Color*)cPtr) + (Width * iy);
                        Color* dest = (((Color*)rPtr) + (linewidth * (iy + dy + YOffset)) + dx);
                        if (isItalic)
                        {
                            dest += (baseLine - YOffset - iy - 1) / 2;
                        }

                        for (int k = 0; k < Width; k++)
                        {
                            if (*src != Color.TransparentBlack)
                            {
                                *dest = color;
                                if (isBold)
                                {
                                    *(dest + 1) = color;
                                }
                            }
                            dest++;
                            src++;
                        }
                    }
                }
            }

            if (isUnderlined)
            {
                if (baseLine >= maxHeight)
                    return;
                Color* dest = (((Color*)rPtr) + (linewidth * (baseLine)) + dx);
                int w = isBold ? Width + 2 : Width + 1;
                for (int k = 0; k < w; k++)
                {
                    *dest++ = color;
                }
            }
        }

        public void LoadCharacter(BinaryReader reader, GraphicsDevice graphics)
        {
            int readerStart = (int)reader.BaseStream.Position;

            this.XOffset = reader.ReadByte();
            this.YOffset = reader.ReadByte();
            this.Width = reader.ReadByte();
            this.Height = reader.ReadByte();

            // only read data if there is data...
            if ((this.Width > 0) && (this.Height > 0))
            {
                // At this point, we know we have data, so go ahead and start reading!
                _textureData = new Color[Width * Height];

                unsafe
                {
                    fixed (Color* p = _textureData)
                    {
                        for (int y = 0; y < Height; ++y)
                        {
                            byte[] scanline = reader.ReadBytes(((Width - 1) / 8) + 1);
                            int bitX = 7;
                            int byteX = 0;
                            for (int x = 0; x < Width; ++x)
                            {
                                Color color = Color.TransparentBlack;
                                if ((scanline[byteX] & (byte)Math.Pow(2, bitX)) != 0)
                                {
                                    color = Color.White;
                                }

                                p[x + y * Width] = color;
                                bitX--;
                                if (bitX < 0)
                                {
                                    bitX = 7;
                                    byteX++;
                                }
                            }
                        }
                    }
                }
            }

            Metrics.ReportDataRead((int)reader.BaseStream.Position - readerStart);
        }
    }

    internal sealed class UniFont
    {
        GraphicsDevice _graphics = null;
        BinaryReader _reader = null;
        private UniCharacter[] _characters;

        private int _height = 0;
        public int Height { get { return _height; } set { _height = value; } }
        private int _baseline = 0;
        public int Baseline { get { return _baseline; } set { _baseline = value; } }

        public UniFont()
        {
            _characters = new UniCharacter[0x10000];
        }

        public void Initialize(GraphicsDevice graphicsDevice, BinaryReader reader)
        {
            _graphics = graphicsDevice;
            _reader = reader;
            // We load the first 128 characters to 'seed' the font with correct spacing values.
            for (int iChar = 0; iChar < 128; iChar++)
            {
                GetCharacter(iChar);
            }
            // Determine the width of the space character - arbitrarily .333 the width of capital M (.333 em?).
            GetCharacter(' ').Width = GetCharacter('M').Width / 3;
            Baseline = GetCharacter('M').Height + GetCharacter('M').YOffset;
        }

        public UniCharacter GetCharacter(char character)
        {
            return GetCharacter(((int)character) & 0xFFFFF);
        }

        public UniCharacter GetCharacter(int index)
        {
            if (_characters[index] == null)
            {
                _characters[index] = loadCharacter(index);
                if (index < 128 && (_characters[index].Height + _characters[index].YOffset + 2) > Height)
                {
                    Height = _characters[index].Height + _characters[index].YOffset + 2;
                }
            }
            return _characters[index];
        }

        UniCharacter loadCharacter(int index)
        {
            // get the lookup table - 0x10000 ints.
            _reader.BaseStream.Position = index * 4;
            int lookup = _reader.ReadInt32();

            UniCharacter character = new UniCharacter();

            if (lookup == 0)
            {
                // no character - so we just return an empty character
                return character;
            }
            else
            {
                _reader.BaseStream.Position = lookup;
                character.LoadCharacter(_reader, _graphics);
                return character;
            }
        }

        public int GetWidth(char ch)
        {
            return GetCharacter(ch).Width;
        }

        public int GetWidth(string text)
        {
            if (text == null || text.Length == 0) { return 0; }

            int width = 0;

            for (int i = 0; i < text.Length; ++i)
            {
                width += GetCharacter(text[i]).Width;
            }

            return width;
        }
    }

    public static class UniText
    {
        private static UniFont[] _fonts = new UniFont[7];
        private static bool _initialized;
        private static GraphicsDevice _graphicsDevice;

        static UniText()
        {

        }

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if (!_initialized)
            {
                _initialized = true;
                _graphicsDevice = graphicsDevice;
                int maxHeight = 0;
                for (int iFont = 0; iFont < 7; iFont++)
                {
                    string path = FileManager.GetFilePath("unifont" + (iFont == 0 ? "" : iFont.ToString()) + ".mul");
                    if (path != null)
                    {
                        _fonts[iFont] = new UniFont();
                        _fonts[iFont].Initialize(_graphicsDevice, new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)));
                        if (_fonts[iFont].Height > maxHeight)
                            maxHeight = _fonts[iFont].Height;
                    }
                }

                for (int iFont = 0; iFont < 7; iFont++)
                {
                    if (_fonts[iFont] == null)
                        continue;
                    _fonts[iFont].Height = maxHeight;
                }
            }
        }

        static Dictionary<string, Texture2D> _TextureCache;
        static Dictionary<Texture2D, HREFRegions> _hrefRegionsCache;

        public static Texture2D GetTexture(string text)
        {
            return getTexture(text, 0, 0);
        }

        public static Texture2D GetTexture(string text, int width, int height)
        {
            return getTexture(text, width, height);
        }

        public static Texture2D GetTexture(string text, int width, int height, ref HREFRegions regions)
        {
            Texture2D texture = getTexture(text, width, height);
            regions = _hrefRegionsCache[texture];
            return texture;
        }

        static Texture2D getTexture(string text, int width, int height)
        {
            string hash = string.Format("text:{0}", text);

            if (_TextureCache == null)
            {
                _TextureCache = new Dictionary<string, Texture2D>();
                _hrefRegionsCache = new Dictionary<Texture2D, HREFRegions>();
            }

            if (!_TextureCache.ContainsKey(hash))
            {
                HREFRegions r = new HREFRegions();
                Texture2D texture = writeTexture(text, width, height, r);
                _TextureCache.Add(hash, texture);
                _hrefRegionsCache.Add(texture, r);
            }
            return _TextureCache[hash];
        }

        static Texture2D writeTexture(string textToRender, int w, int h, HREFRegions regions)
        {
            HTMLReader reader = new HTMLReader(textToRender);

            int width = 0, height = 0;
            if (w == 0)
            {
                getTextDimensions(reader, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height, out width, out height);
            }
            else
            {
                getTextDimensions(reader, w, h, out width, out height);
            }

            if (width == 0) // empty text string
                return new Texture2D(_graphicsDevice, 1, 1);

            Color[] resultData = new Color[width * height];
            int dy = 0, lineheight = 0;

            unsafe
            {
                fixed (Color* rPtr = resultData)
                {
                    int[] alignedTextX = new int[3];
                    List<HTMLCharacter>[] alignedText = new List<HTMLCharacter>[3];
                    for (int i = 0; i < 3; i++)
                        alignedText[i] = new List<HTMLCharacter>();

                    for (int i = 0; i < reader.Length; i++)
                    {
                        HTMLCharacter c = reader.Characters[i];
                        alignedText[(int)c.Alignment].Add(c);

                        if (c.Character == '\n' || (i == reader.Length - 1))
                        {
                            // write left aligned text.
                            int dx;
                            if (alignedText[0].Count > 0)
                            {
                                alignedTextX[0] = dx = 0;
                                writeTexture_Line(alignedText[0], rPtr, ref dx, dy, width, ref lineheight, true);
                            }

                            // centered text. We need to get the width first. Do this by drawing the line with var draw = false.
                            if (alignedText[1].Count > 0)
                            {
                                dx = 0;
                                writeTexture_Line(alignedText[1], rPtr, ref dx, dy, width, ref lineheight, false);
                                alignedTextX[1] = dx = width / 2 - dx / 2;
                                writeTexture_Line(alignedText[1], rPtr, ref dx, dy, width, ref lineheight, true);
                            }

                            // right aligned text.
                            if (alignedText[2].Count > 0)
                            {
                                dx = 0;
                                writeTexture_Line(alignedText[2], rPtr, ref dx, dy, width, ref lineheight, false);
                                alignedTextX[2] = dx = width - dx;
                                writeTexture_Line(alignedText[2], rPtr, ref dx, dy, width, ref lineheight, true);
                            }

                            // get HREF regions for html.
                            if (regions != null)
                                getHREFRegions(regions, alignedText, alignedTextX, dy);

                            // clear the aligned text lists so we can fill them up in our next pass.
                            for (int j = 0; j < 3; j++)
                            {
                                alignedText[j].Clear();
                            }

                            dy += lineheight;
                        }
                    }
                }
            }

            Texture2D result = new Texture2D(_graphicsDevice, width, height, 1, TextureUsage.None, SurfaceFormat.Color);
            result.SetData<Color>(resultData);
            return result;
        }

        static void getHREFRegions(HREFRegions regions, List<HTMLCharacter>[] text, int[] x, int y)
        {
            for (int alignment = 0; alignment < 3; alignment++)
            {
                // variables for the open href region
                bool hrefRegionOpen = false;
                Rectangle hrefRegion = new Rectangle();
                string hrefCurrent = string.Empty;
                Point hrefOrigin = new Point();
                int hrefHeight = 0;

                int dx = x[alignment];
                for (int i = 0; i < text[alignment].Count; i++)
                {
                    HTMLCharacter c = text[alignment][i];
                    UniFont font = _fonts[(int)c.Font];
                    UniCharacter character = font.GetCharacter(c.Character);

                    if (c.HREF != hrefCurrent)
                    {
                        // close the current href tag if one is open.
                        if (hrefRegionOpen)
                        {
                            hrefRegion.Width = (dx - hrefOrigin.X);
                            hrefRegion.Height = (y + hrefHeight - hrefOrigin.Y);
                            regions.AddRegion(hrefRegion, hrefCurrent);
                            hrefRegionOpen = false;
                            hrefCurrent = string.Empty;
                        }

                        // did we open a href?
                        if (c.HREF != string.Empty)
                        {
                            hrefRegionOpen = true;
                            hrefCurrent = c.HREF;
                            hrefOrigin = new Point(dx, y);
                            hrefRegion = new Rectangle(dx, y, 0, 0);
                            hrefHeight = 0;
                        }
                    }

                    dx += (c.IsBold) ? character.Width + 2 : character.Width + 1;
                    if (hrefRegionOpen && font.Height > hrefHeight)
                        hrefHeight = font.Height;
                }

                // close the current href tag if one is open.
                if (hrefRegionOpen)
                {
                    hrefRegion.Width = (dx - hrefOrigin.X);
                    hrefRegion.Height = (y + hrefHeight - hrefOrigin.Y);
                    regions.AddRegion(hrefRegion, hrefCurrent);
                }
            }
        }

        // pass bool = false to get the width of the line to be drawn without actually drawing anything. Useful for aligning text.
        static unsafe void writeTexture_Line(List<HTMLCharacter> text, Color* rPtr, ref int x, int y, int linewidth, ref int lineheight, bool draw)
        {
            for (int i = 0; i < text.Count; i++)
            {
                HTMLCharacter c = text[i];
                UniFont font = _fonts[(int)c.Font];
                UniCharacter character = font.GetCharacter(c.Character);
                if (draw)
                {
                    Color color = c.IsHREF ? new Color(255, 255, 255) : c.Color; // HREF links should be colored white.
                    character.WriteToBuffer(rPtr, x, y, linewidth, font.Height, font.Baseline, c.IsBold, c.IsItalic, c.IsUnderlined, color);
                }
                lineheight = font.Baseline;
                x += (c.IsBold) ? character.Width + 2 : character.Width + 1;
            }
        }

        static void getTextDimensions(HTMLReader reader, int maxwidth, int maxheight, out int width, out int height)
        {
            width = 0;
            height = _fonts[0].Height;
            int widestline = 0;
            int italicwidth = 0; // for italic characters, which need a little more room for their slant.
            List<HTMLCharacter> word = new List<HTMLCharacter>();

            for (int i = 0; i < reader.Length; ++i)
            {
                HTMLCharacter c = reader.Characters[i];
                UniFont font = _fonts[(int)c.Font];

                if (((int)c.Character) > 32)
                {
                    word.Add(c);
                }

                if (c.Alignment != enumHTMLAlignments.Left)
                    widestline = maxwidth;

                if (c.Character == ' ' || i == reader.Length - 1 || c.Character == '\n')
                {
                    // Size the word, character by character.
                    int wordwidth = 0;

                    if (word.Count > 0)
                    {
                        for (int j = 0; j < word.Count; j++)
                        {
                            int charwidth = _fonts[(int)word[j].Font].GetCharacter(word[j].Character).Width;
                            // bold characters are one pixel wider than normal characters.
                            if (c.IsBold)
                                charwidth++;

                            // italic characters need a little extra width if they are at the end of the line.
                            if (c.IsItalic)
                                italicwidth = font.Height / 2;
                            else
                            {
                                italicwidth -= charwidth;
                                if (italicwidth < 0)
                                    italicwidth = 0;
                            }

                            wordwidth += charwidth + 1;
                        }
                    }

                    // Now make sure this line can fit the word.
                    if (width + wordwidth + italicwidth <= maxwidth)
                    {
                        // it can fit!
                        width += wordwidth + italicwidth;
                        word.Clear();
                        // if this word is followed by a space, does it fit? If not, drop it entirely and insert \n after the word.
                        if (c.Character == ' ')
                        {
                            int charwidth = _fonts[(int)c.Font].GetCharacter(c.Character).Width;
                            if (width + charwidth + 1 <= maxwidth)
                            {
                                // we can fit an extra space here.
                                width += charwidth + 1;
                            }
                            else
                            {
                                // can't fit an extra space on the end of the line. replace the space with a \n.
                                reader.Characters[i] = new HTMLCharacter('\n');
                                i--;
                            }
                        }
                    }
                    else
                    {
                        // this word is too big, so we insert a \n character before the word... and try again.
                        reader.Characters.Insert(i - word.Count, new HTMLCharacter('\n'));
                        i = i - word.Count - 1;
                        word.Clear();
                    }

                    if (c.Character == '\n')
                    {
                        if (width + italicwidth > widestline)
                            widestline = width + italicwidth;
                        height += font.Baseline;
                        width = 0;
                    }
                }
            }

            width += italicwidth;
            if (widestline > width)
                width = widestline;
        }
    }
}