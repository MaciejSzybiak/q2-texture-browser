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
/// Mod folder gameobject in the left panel.
/// </summary>
public class ModFolderObject : MonoBehaviour, IClearableForPool
{
    #region unity properties
    [SerializeField]
    private TMP_Text label = default;
    [SerializeField]
    private TMP_Text countLabel = default;
    [SerializeField]
    private Image background = default;

    [Header("Prefabs")]
    [SerializeField]
    private FolderObject folderObjectPrefab = default;
    #endregion

    public Transform folderListParent;
    public ObjectPool folderObjectPool;

    public ModFolder ModFolder { get; private set; }
    private List<FolderObject> folders = new List<FolderObject>();

    public bool IsExpanded { get; private set; } = false;

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

    private void OnDestroy()
    {
        ClearForPool();
    }

    public void ClearForPool()
    {
        //clear all subfolders
        for (int i = 0; i < folders.Count; i++)
        {
            folderObjectPool.Return(folders[i]);
        }
        folders.Clear();
        ModFolder = null;
        IsExpanded = false;
        background.color = Color.gray;
    }

    /// <summary>
    /// Creates subfolder objects for this mod folder.
    /// </summary>
    /// <param name="mf">Mod folder data class.</param>
    public void SetModFolder(ModFolder mf)
    {
        ModFolder = mf;

        for(int i = 0; i < ModFolder.TextureFolders.Count; i++)
        {
            FolderObject fo = folderObjectPool.Get<FolderObject>();
            fo.Name = ModFolder.TextureFolders[i].Name;
            fo.textureFolder = ModFolder.TextureFolders[i];
            fo.gameObject.SetActive(false);
            fo.SetCount();
            fo.transform.SetAsLastSibling();
            folders.Add(fo);
        }
        countLabel.text = ModFolder.TextureFolders.Count.ToString();
    }

    public void OnToggle()
    {
        if (IsExpanded)
        {
            Contract();
        }
        else
        {
            Expand();
        }
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Expands the folders that this modFolderObject contains.
    /// </summary>
    private void Expand()
    {
        background.color = Color.yellow;
        foreach(FolderObject f in folders)
        {
            f.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Contracts the folders that this modFolderObject contains.
    /// </summary>
    private void Contract()
    {
        background.color = Color.gray;
        foreach (FolderObject f in folders)
        {
            BrowserManager.Instance.ForceDeselect(f);
            f.gameObject.SetActive(false);
        }
    }
}
