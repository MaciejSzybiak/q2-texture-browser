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
/// Folder item in the left panel.
/// </summary>
public class FolderObject : MonoBehaviour, IClearableForPool
{
    #region unity properties
    [SerializeField]
    private TMP_Text label = default;
    [SerializeField]
    private TMP_Text countLabel = default;
    [SerializeField]
    private Image background = default;
    public TextureFolder textureFolder;
    #endregion

    public bool IsSelected { get; private set; } = false;

    public List<TextureObject> textures = new List<TextureObject>();

    public string Name
    {
        get
        {
            return label.text;
        }
        set
        {
            label.text = value;
        }
    }

    /// <summary>
    /// Select action handler. Sets panel color to yellow.
    /// </summary>
    public void OnSelected()
    {
        background.color = Color.yellow;
        IsSelected = true;
    }

    /// <summary>
    /// Deselect action handler. Sets panel color to white and clears texture list.
    /// </summary>
    public void OnDeselected()
    {
        background.color = Color.white;
        IsSelected = false;
    }

    private void OnDestroy()
    {
        ClearTextures();
    }

    public void SetCount()
    {
        countLabel.text = textureFolder.Textures.Count.ToString();
    }

    private void ClearTextures()
    {
        textures.Clear();
    }

    public void CacheTextures()
    {
        foreach(TextureObject t in textures)
        {
            t.gameObject.SetActive(false);
            t.isCached = true;
        }
    }

    public void RestoreCache()
    {
        foreach (TextureObject t in textures)
        {
            t.gameObject.SetActive(true);
            t.isCached = false;
        }
    }

    /// <summary>
    /// Mouse click action.
    /// </summary>
    public void OnClick()
    {
        BrowserManager.Instance.AddFolderSelection(this);
    }

    public void ClearForPool()
    {
        ClearTextures();
        OnDeselected();
    }
}
