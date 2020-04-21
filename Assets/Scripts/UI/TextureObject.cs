/**
Copyright (C) 2020 Maciej Szybiak

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see https://www.gnu.org/licenses/.
*/

using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Flags]
public enum TextureImageType
{
    wal = 1,
    jpgpng = 2
}

/// <summary>
/// Texture gameobject in the right panel. Shows texture's thumbnail, its name
/// and jpg/png indicator.
/// Responsible for loading the actual texture files.
/// </summary>
public class TextureObject : MonoBehaviour, IClearableForPool
{
    #region unity properties
    [SerializeField]
    private Image browserImage = default;
    [SerializeField]
    private TMP_Text browserLabel = default;
    [SerializeField]
    private GameObject PNGJPGIndicator = default;
    #endregion
    
    //texture data
    public Q2Texture Q2Texture { get; private set; }

    private Texture2D walTexture;
    private Texture2D jpgTexture;
    private Texture2D pngTexture;

    public Sprite WalSprite { get; private set; }
    public Sprite JpgSprite { get; private set; }
    public Sprite PngSprite { get; private set; }

    //list filtering properties
    public bool regexUnmatched = false;
    public bool typeUnmatched = false;
    public bool isCached = false;

    /// <summary>
    /// Stores .wal file structure.
    /// </summary>
    class WalFile
    {
        public char[] name = new char[32];

        public int[] offset = new int[4];

        public char[] next_name = new char[32];

        public uint flags;
        public uint contents;
        public uint value;

        public byte[] mip0_data;
        public byte[] mip1_data;
        public byte[] mip2_data;
        public byte[] mip3_data;

        public byte[] colordata;
    }

    /// <summary>
    /// Loads texture file using provided data and applies it to the UI items.
    /// </summary>
    /// <param name="q2t">Texture data.</param>
    public void LoadTextureFile(Q2Texture q2t)
    {
        Q2Texture = q2t;
        browserLabel.text = q2t.Name; //set label text
        LoadTextureFile();
    }

    public void OnClick()
    {
        //open the texture view panel
        NavigationController.Instance.To_TextureView();
        TextureViewController.Instance.SetTextureOnOpen(this);
    }

    /// <summary>
    /// Sets image sprite according to the type (if available).
    /// </summary>
    /// <param name="type"></param>
    public void SetWalImage(TextureImageType type)
    {
        browserImage.sprite = null;
        if (type.HasFlag(TextureImageType.jpgpng) && Q2Texture.HasPNG)
        {
            browserImage.sprite = PngSprite;
        }
        else if (type.HasFlag(TextureImageType.jpgpng) && Q2Texture.HasJPG)
        {
            browserImage.sprite = JpgSprite;
        }
        else
        {
            //no jpg or png
            browserImage.sprite = WalSprite;
        }
    }

    /// <summary>
    /// Loads wal, png and jpg textures and creates image sprites.
    /// </summary>
    public void LoadTextureFile()
    {
        BinaryReader reader;

        if (Q2Texture.Name.Length == 0)
        {
            Debug.LogAssertion("failed to load the texture: name is empty", this);
            return;
        }

        //****
        //load wal
        //****

        WalFile wal = new WalFile();
        try
        {
            reader = new BinaryReader(File.OpenRead(Q2Texture.Path + ".wal"));

            //read .wal header
            wal.name = reader.ReadChars(32);

            Q2Texture.Dimensions.x = (int)reader.ReadUInt32();
            Q2Texture.Dimensions.y = (int)reader.ReadUInt32();

            if (Q2Texture.Dimensions.x > 2048 || Q2Texture.Dimensions.y > 2048)
            {
                Logging.LogWarning(Q2Texture.Path + ".wal is a bad wal file.");
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                wal.offset[i] = reader.ReadInt32();
            }

            wal.next_name = reader.ReadChars(32);

            wal.flags = reader.ReadUInt32();
            Q2Texture.flags = wal.flags;

            wal.contents = reader.ReadUInt32();
            Q2Texture.contents = wal.contents;

            wal.value = reader.ReadUInt32();
            Q2Texture.value = wal.value;

            //read color data for all mips
            reader.BaseStream.Position = wal.offset[0];
            wal.mip0_data = reader.ReadBytes(Q2Texture.Dimensions.x * Q2Texture.Dimensions.y);

            reader.BaseStream.Position = wal.offset[1];
            wal.mip1_data = reader.ReadBytes(Q2Texture.Dimensions.x * Q2Texture.Dimensions.y / 2);

            reader.BaseStream.Position = wal.offset[2];
            wal.mip2_data = reader.ReadBytes(Q2Texture.Dimensions.x * Q2Texture.Dimensions.y / 4);

            reader.BaseStream.Position = wal.offset[3];
            wal.mip3_data = reader.ReadBytes(Q2Texture.Dimensions.x * Q2Texture.Dimensions.y / 8);

            int colorlen = wal.mip0_data.Length * 4;
            wal.colordata = new byte[colorlen];

            //flip bottom left alignment to top left alignment
            FlipWalData(ref wal.mip0_data, Q2Texture.Dimensions.x, Q2Texture.Dimensions.y);
            FlipWalData(ref wal.mip1_data, Q2Texture.Dimensions.x / 2, Q2Texture.Dimensions.y / 2);
            FlipWalData(ref wal.mip2_data, Q2Texture.Dimensions.x / 4, Q2Texture.Dimensions.y / 4);
            FlipWalData(ref wal.mip3_data, Q2Texture.Dimensions.x / 8, Q2Texture.Dimensions.y / 8);

            //parse colors
            Color c;
            for (int i = 0; i < colorlen; i += 4)
            {
                c = GamePathManager.colormap[wal.mip0_data[i / 4]];

                wal.colordata[i] = (byte)(c.r * 255);
                wal.colordata[i + 1] = (byte)(c.g * 255);
                wal.colordata[i + 2] = (byte)(c.b * 255);
                wal.colordata[i + 3] = (byte)(c.a * 255);
            }

            //create texture object
            walTexture = new Texture2D(Q2Texture.Dimensions.x, Q2Texture.Dimensions.y, TextureFormat.RGBA32, true);
            walTexture.LoadRawTextureData(wal.colordata.Concat(wal.mip0_data).Concat(wal.mip1_data).Concat(wal.mip2_data).Concat(wal.mip3_data).ToArray());
            walTexture.filterMode = FilterMode.Point;
            walTexture.Apply(true);

            Q2Texture.FileSize = (int)reader.BaseStream.Length;

            reader.Close();

            Q2Texture.ModificationDate = File.GetLastWriteTime(Q2Texture.Path + ".wal");

            //create sprite
            WalSprite = Sprite.Create(walTexture, new Rect(0.0f, 0.0f, walTexture.width, walTexture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception e)
        {
            Logging.LogError("Failed loading .wal texture: " + e.Message + "\n", this);
            return;
        }

        //****
        //load jpg
        //****

        if (Q2Texture.HasJPG)
        {
            try
            {
                reader = new BinaryReader(File.OpenRead(Q2Texture.Path + ".jpg"));
                byte[] data = new byte[reader.BaseStream.Length];
                reader.Read(data, 0, (int)reader.BaseStream.Length);

                //create texture object
                jpgTexture = new Texture2D(2, 2, TextureFormat.BGRA32, true);
                jpgTexture.filterMode = FilterMode.Point;
                jpgTexture.LoadImage(data);

                //create sprite
                JpgSprite = Sprite.Create(jpgTexture, new Rect(0.0f, 0.0f, jpgTexture.width, jpgTexture.height), new Vector2(0.5f, 0.5f), 100f);

                PNGJPGIndicator.SetActive(true);
            }
            catch (System.Exception e)
            {
                Logging.LogError("Failed loading .jpg texture: " + e.Message, this);
                return;
            }
        }

        //****
        //load png
        //****

        if (Q2Texture.HasPNG)
        {
            try
            {
                reader = new BinaryReader(File.OpenRead(Q2Texture.Path + ".png"));
                byte[] data = new byte[reader.BaseStream.Length];
                reader.Read(data, 0, (int)reader.BaseStream.Length);

                //create texture object
                pngTexture = new Texture2D(2, 2, TextureFormat.BGRA32, true);
                pngTexture.filterMode = FilterMode.Point;
                pngTexture.LoadImage(data);

                //create sprite
                PngSprite = Sprite.Create(pngTexture, new Rect(0.0f, 0.0f, pngTexture.width, pngTexture.height), new Vector2(0.5f, 0.5f), 100f);

                PNGJPGIndicator.SetActive(true);
            }
            catch (System.Exception e)
            {
                Logging.LogError("Failed loading .png texture: " + e.Message + "\n", this);
                return;
            }
        }
    }

    //flips color array upside down
    private void FlipWalData(ref byte[] data, int w, int h)
    {
        byte temp;
        int a, b;

        for(int rowOffset = 0; rowOffset < data.Length / 2; rowOffset += w)
        {
            for(int i = 0; i < w; i++)
            {
                a = rowOffset + i;
                b = data.Length - rowOffset - w + i;
                temp = data[a];
                data[a] = data[b];
                data[b] = temp;
            }
        }
    }

    private void OnDestroy()
    {
        ClearForPool(); //make sure everything is destroyed properly
    }

    public void ClearForPool()
    {
        Q2Texture = null;

        walTexture = null;
        Destroy(walTexture);
        walTexture = null;
        Destroy(jpgTexture);
        jpgTexture = null;
        Destroy(pngTexture);
        pngTexture = null;

        WalSprite = null;
        JpgSprite = null;
        PngSprite = null;

        regexUnmatched = false;
        typeUnmatched = false;
        isCached = false;
    }
}
