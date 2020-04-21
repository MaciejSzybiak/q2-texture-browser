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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mangages the texture view panel's behaviour.
/// </summary>
public class TextureViewController : MonoBehaviour
{
    private readonly Dictionary<uint, string> surfaceFlags = new Dictionary<uint, string>
    {
        { 0x1 , "light" },
        { 0x2 , "slick" },
        { 0x4 , "sky" },
        { 0x8 , "warp" },
        { 0x10, "trans33" },
        { 0x20, "trans66" },
        { 0x40, "flowing" },
        { 0x80, "nodraw" }
    };

    private readonly Dictionary<uint, string> brushContents = new Dictionary<uint, string>
    {
        { 1 , "solid" },
        { 2 , "window" },
        { 4 , "aux" },
        { 8 , "lava" },
        { 16, "slime" },
        { 32, "water" },
        { 64, "mist" },

        { 0x8000 , "areaportal" },
        { 0x10000, "playerclip" },
        { 0x20000, "monsterclip" },

        { 0x40000 , "current_0" },
        { 0x80000 , "current_90" },
        { 0x100000, "current_180" },
        { 0x200000, "current_270" },
        { 0x400000, "current_up" },
        { 0x800000, "current_down" },

        { 0x1000000, "origin" },

        { 0x2000000 , "monster" },
        { 0x4000000 , "deadmonster" },
        { 0x8000000 , "detail" },
        { 0x10000000, "translucent" },
        { 0x20000000, "ladder" },
    };

    #region unity properties
    [SerializeField]
    private Image texImage = default;
    [SerializeField]
    private TMP_Text detailsText = default;
    [SerializeField]
    private Button walBtn = default;
    [SerializeField]
    private Button jpgpngBtn = default;
    #endregion

    private TextureObject texObj;

    private const string sizeLarge = "<size=115%>";
    private const string sizeNormal = "<size=100%>";
    private const string sizeSmall = "<size=80%>";

    private const string boldStart = "<b>";
    private const string boldEnd = "</b>";

    private const string italicStart = "<i>";
    private const string italicEnd = "</i>";

    private const string smallcapsStart = "<smallcaps>";
    private const string smallcapsEnd = "</smallcaps>";

    private const string uppercaseStart = "<uppercase>";
    private const string uppercaseEnd = "</uppercase>";

    private const string intendation = "  ";
    private const string intendationBig = "    ";

    private const string newLine = "\n";
    private const string newBigLine = "\n\n";

    private const string styleOStart = sizeLarge + boldStart + uppercaseStart;
    private const string style0End = boldEnd + uppercaseEnd;

    private const string style1Start = sizeNormal + intendation + boldStart + smallcapsStart;
    private const string style1End = boldEnd + smallcapsEnd;
    
    private const string style2Start = sizeSmall + intendationBig;
    private const string style2End = "";

    private TextureImageType currentImageType = TextureImageType.jpgpng | TextureImageType.wal;

    #region singleton

    public static TextureViewController Instance { get; private set; }
    private void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    #endregion

    /// <summary>
    /// Change texture type action.
    /// </summary>
    /// <param name="type">Texture type corresponding to TextureImageType enum.</param>
    public void OnTextureTypeClick(int type)
    {
        if(type == 2 && (texObj.Q2Texture.HasJPG || texObj.Q2Texture.HasPNG))
        {
            currentImageType = TextureImageType.jpgpng | TextureImageType.wal;
        }
        else
        {
            currentImageType = TextureImageType.wal;
        }
        SetTexture(texObj);
        SetButtonColors();
    }

    /// <summary>
    /// Called when the panel is opened.
    /// </summary>
    /// <param name="tex">Texture to be set.</param>
    public void SetTextureOnOpen(TextureObject tex)
    {
        if(!tex.Q2Texture.HasJPG && !tex.Q2Texture.HasPNG)
        {
            currentImageType = TextureImageType.wal;
        }
        else
        {
            currentImageType = TextureImageType.jpgpng | TextureImageType.wal;
        }

        SetTexture(tex);
        SetButtonColors();
    }

    /// <summary>
    /// Sets window elements such as texture image sprite and texture description.
    /// </summary>
    /// <param name="tex">Texture to be set.</param>
    public void SetTexture(TextureObject tex)
    {
        //set description
        MakeDetailsString(tex.Q2Texture);

        texImage.sprite = null; //fixes unity sprite aspect bug
        if (currentImageType.HasFlag(TextureImageType.jpgpng) && tex.Q2Texture.HasPNG)
        {
            texImage.sprite = tex.PngSprite;
        }
        else if (currentImageType.HasFlag(TextureImageType.jpgpng) && tex.Q2Texture.HasJPG)
        {
            texImage.sprite = tex.JpgSprite;
        }
        else
        {
            texImage.sprite = tex.WalSprite;
        }
        
        texObj = tex;
    }

    /// <summary>
    /// Sets texture type buttons colors.
    /// </summary>
    private void SetButtonColors()
    {
        if (currentImageType.HasFlag(TextureImageType.jpgpng))
        {
            jpgpngBtn.image.color = Color.green;
            walBtn.image.color = Color.white;
        }
        else
        {
            if(texObj.Q2Texture.HasPNG || texObj.Q2Texture.HasJPG)
            {
                jpgpngBtn.image.color = Color.white;
            }
            else
            {
                jpgpngBtn.image.color = Color.gray;
            }
            walBtn.image.color = Color.green;
        }
    }

    /// <summary>
    /// Creates texture description text.
    /// </summary>
    /// <param name="tex">Texture data.</param>
    private void MakeDetailsString(Q2Texture tex)
    {
        string folder = tex.Path.Substring(0, tex.Path.LastIndexOf('/'));
        folder = folder.Substring(folder.LastIndexOf('/') + 1);

        string details = styleOStart + tex.Name + style0End + newBigLine +
                         style1Start + "Folder" + style1End + newLine +
                         style2Start + folder + style2End + newBigLine +
                         style1Start + "File size" + style1End + newLine +
                         style2Start + tex.FileSize / 1000 + "kB" + style2End + newBigLine +
                         style1Start + "Date modified" + style1End + newLine +
                         style2Start + tex.ModificationDate.ToShortTimeString() + " " + tex.ModificationDate.ToShortDateString() + style2End + newBigLine +
                         style1Start + "Wal dimensions" + style1End + newLine +
                         style2Start + tex.Dimensions.x + "x" + tex.Dimensions.y + style2End + newBigLine +
                         style1Start + "Flags" + style1End + newLine +
                         style2Start + GetFlagsString(tex.flags) + style2End + newBigLine +
                         style1Start + "Contents" + style1End + newLine +
                         style2Start + GetContentsString(tex.contents) + style2End + newBigLine +
                         style1Start + "Value" + style1End + newLine +
                         style2Start + tex.value + style2End + newBigLine;

        detailsText.text = details;
    }

    /// <summary>
    /// Creates flags string.
    /// </summary>
    /// <param name="flags">Flags mask.</param>
    /// <returns>Formatted flags string.</returns>
    private string GetFlagsString(uint flags)
    {
        string text = "";
        bool addComma = false;
        
        foreach (var c in surfaceFlags)
        {
            if((c.Key & flags) != 0)
            {
                if (addComma)
                {
                    text += ", ";
                }
                text += c.Value;
                addComma = true;
            }
        }

        return text.Length == 0 ? "none" : text;
    }

    /// <summary>
    /// Creates contents string.
    /// </summary>
    /// <param name="flags">Contents mask.</param>
    /// <returns>Formatted contents string.</returns>
    private string GetContentsString(uint contents)
    {
        string text = "";
        bool addComma = false;

        foreach (var c in brushContents)
        {
            if ((c.Key & contents) != 0)
            {
                if (addComma)
                {
                    text += ", ";
                }
                text += c.Value;
                addComma = true;
            }
        }

        return text.Length == 0 ? "none" : text;
    }
}
