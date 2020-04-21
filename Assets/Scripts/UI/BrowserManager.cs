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

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrowserManager : MonoBehaviour
{
    private enum TextureSortingMethod
    {
        alphabetical,
        size,
        modification_time
    }

    private enum NextDirection
    {
        up = -1,
        down = 1
    }

    #region unity properties
    [Header("Object pools")]
    [SerializeField]
    private ObjectPool texturePool;
    [SerializeField]
    private ObjectPool modFolderPool;
    [SerializeField]
    private ObjectPool folderObjectPool;

    [Header("Prefabs")]
    [SerializeField]
    private ModFolderObject ModFolderObjectPrefab = default;
    [SerializeField]
    private TextureObject TextureObjectPrefab = default;

    [Header("List parents")]
    [SerializeField]
    private Transform folderList = default;
    [SerializeField]
    private Transform textureGrid = default;

    [Header("Texture type buttons")]
    [SerializeField]
    private Image walBtnImage = default;
    [SerializeField]
    private Image pngjpgBtnImage = default;

    [Header("Other")]
    [SerializeField]
    private TMP_InputField searchField = default;
    [SerializeField]
    private GameObject gridView = default;
    #endregion

    private const int maxCachedFolders = 5;

    private List<TextureObject> textureObjects = new List<TextureObject>();
    private List<ModFolderObject> modFolderObjects = new List<ModFolderObject>();
    private List<FolderObject> folderSelection = new List<FolderObject>();
    private List<FolderObject> cachedFolders = new List<FolderObject>(maxCachedFolders);
    private FolderObject rootFolderSelection;

    private List<FolderObject> toSelect = new List<FolderObject>();
    private List<FolderObject> toDeselect = new List<FolderObject>();

    private TextureSortingMethod currentSortingMethod = TextureSortingMethod.alphabetical;
    private TextureImageType currentImageType = TextureImageType.jpgpng | TextureImageType.wal;

    private bool loadingRangeResume = false;

    #region singleton

    public static BrowserManager Instance { get; private set; }

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
    private void Start()
    {
        SetTextureTypeButtonColors();    
    }

    private void Update()
    {
        if(folderSelection.Count == 1 && !Popup.Instance.gameObject.activeSelf)
        {
            NextDirection direction;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = NextDirection.up;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = NextDirection.down;
            }
            else
            {
                return;
            }

            //find sibling
            if(TryFindNextFolder(folderSelection[0].transform.GetSiblingIndex(), out FolderObject next, direction))
            {
                ForceDeselect(folderSelection[0]);
                AddFolderSelection(next);
            }
        }
    }

    private bool TryFindNextFolder(int index, out FolderObject folder, NextDirection direction)
    {
        int increment = (int)direction;
        int last = folderList.childCount - 1;
        folder = null;
        while (!folder || !folder.gameObject.activeSelf)
        {
            index += increment;

            if(index < 0 || index > last)
            {
                return false;
            }

            folder = folderList.GetChild(index).GetComponent<FolderObject>();
        }
        return true;
    }

    private void OnEnable()
    {
        if(!Application.isPlaying)
        {
            Debug.LogAssertion("Called OnEnable in editor on a disabled MonoBehaviour...", this);
            return;
        }
        //create the folder list
        foreach(ModFolder m in FileList.Instance.ModFolders)
        {
            ModFolderObject mfo = modFolderPool.Get<ModFolderObject>();
            mfo.Name = m.Name;
            mfo.folderObjectPool = folderObjectPool;
            mfo.folderListParent = folderList;
            mfo.SetModFolder(m);

            modFolderObjects.Add(mfo);
        }
    }

    private void OnDisable()
    {
        //clear all loaded data and destroy objects
        for (int i = 0; i < textureObjects.Count; i++)
        {
            textureObjects[i].ClearForPool();
            texturePool.Return(textureObjects[i]);
        }
        textureObjects.Clear();

        for (int i = 0; i < modFolderObjects.Count; i++)
        {
            modFolderObjects[i].ClearForPool();
            modFolderPool.Return(modFolderObjects[i]);
        }
        modFolderObjects.Clear();

        folderSelection.Clear();
        cachedFolders.Clear();
    }

    /// <summary>
    /// Performs a search and filters out textures by disabling their gameobjects.
    /// </summary>
    /// <param name="search">Regex pattern for search.</param>
    public void OnSearchStringChanged(string search)
    {
        //activate all type matched textures
        foreach (var t in textureObjects)
        {
            Debug.Assert(!t.isCached, "Found a cached texture!", t);
            if (!t.typeUnmatched && !t.isCached)
            {
                t.gameObject.SetActive(true);
            }
            t.regexUnmatched = false;
        }

        try
        {
            Regex pattern = new Regex(search);
            var nonMatchingTextures = textureObjects.Where(i => !pattern.IsMatch(i.Q2Texture.Name));

            //disable all textures not matching this regex pattern
            foreach(var t in nonMatchingTextures)
            {
                t.gameObject.SetActive(false);
                t.regexUnmatched = true;
            }

            //make text yellow if all textures were hidden
            if(nonMatchingTextures.Count() == textureObjects.Count)
            {
                searchField.textComponent.color = Color.yellow;
            }
            else
            {
                searchField.textComponent.color = Color.white;
            }
        }
        catch (System.Exception)
        {
            //failed to make a regex pattern - set text color to red
            searchField.textComponent.color = Color.red;
            searchField.caretColor = Color.white;
        }
    }

    private void FreeCacheItem()
    {
        if(cachedFolders.Count == 0)
        {
            Debug.LogAssertion("Tried freeing empty cache", this);
            return;
        }

        FolderObject toRemove = cachedFolders[0];

        cachedFolders.RemoveAt(0);

        for(int i = 0; i < toRemove.textures.Count; i++)
        {
            toRemove.textures[i].ClearForPool();
            texturePool.Return(toRemove.textures[i]);
        }
        toRemove.textures.Clear();
    }

    /// <summary>
    /// Puts folder in the cache so it doesn't need to be reloaded if clicked again.
    /// </summary>
    /// <param name="fo">Folder to be cached.</param>
    private void CacheFolder(FolderObject fo)
    {
        Debug.Assert(!(fo.textures.Count > 0 && fo.textures[0].isCached), "Recached a folder", fo);

        if(cachedFolders.Count == maxCachedFolders)
        {
            FreeCacheItem();
        }

        cachedFolders.Add(fo);
        fo.CacheTextures();
        foreach(TextureObject t in fo.textures)
        {
            textureObjects.Remove(t);
        }
    }

    private bool RestoreFromCache(FolderObject fo)
    {
        if (cachedFolders.Contains(fo))
        {
            fo.RestoreCache();
            foreach (TextureObject t in fo.textures)
            {
                SetTextureType(t);
                textureObjects.Add(t);
            }
            cachedFolders.Remove(fo);
            return true;
        }
        return false;
    }

    public void ForceDeselect(FolderObject f)
    {
        if (!folderSelection.Contains(f))
        {
            return;
        }
        CacheFolder(f);
        f.OnDeselected();
        folderSelection.Remove(f);

        if (f == rootFolderSelection)
        {
            rootFolderSelection = null;
        }
    }

    /// <summary>
    /// Processes folder selection.
    /// </summary>
    /// <param name="folderObject">Folder to be selected or deselected.</param>
    public void AddFolderSelection(FolderObject folderObject)
    {
        toSelect.Clear();
        toDeselect.Clear();
        FolderObject[] children;

        bool addMode = Input.GetKey(KeyCode.LeftControl);
        bool rangeMode = Input.GetKey(KeyCode.LeftShift) && !addMode;
        bool singleMode = !rangeMode && !addMode;

        if (rangeMode && rootFolderSelection != null)
        {
            if(rootFolderSelection == folderObject)
            {
                //don't select range when the root folder was clicked
                return;
            }
            //find all folders between the root and the folderObject
            int start = rootFolderSelection.transform.GetSiblingIndex();
            int end = folderObject.transform.GetSiblingIndex();
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
            children = new FolderObject[end - start + 1];
            for (int i = start, j = 0; i <= end; i++, j++)
            {
                children[j] = folderList.GetChild(i).GetComponent<FolderObject>();
                if (!children[j] || !children[j].gameObject.activeSelf)
                {
                    //skip mod folder name and contracted folders
                    children[j] = null;
                    continue;
                }
            }
            toDeselect.AddRange(folderSelection.Where(f => !children.Contains(f)));
            toSelect.AddRange(children.Where(f => !folderSelection.Contains(f)));
        }
        else
        {
            if (folderSelection.Contains(folderObject))
            {
                if (addMode)
                {
                    //deselect single
                    toDeselect.Add(folderObject);
                }
                else
                {
                    //deselect all
                    toDeselect.AddRange(folderSelection.Where(i => i != folderObject));
                }
            }
            else
            {
                //select this folder
                toSelect.Add(folderObject);
                if (!rangeMode)
                {
                    //make it root
                    rootFolderSelection = folderObject;
                }
                if (!addMode)
                {
                    //deselect everything else
                    toDeselect.AddRange(folderSelection.Where(i => i != folderObject));
                }
            }
        }

        //deselection
        if(rangeMode || toSelect.Count > 0 || (toSelect.Count == 0 && toDeselect.Count > 0) || (addMode && folderSelection.Count > 1))
        {
            foreach(FolderObject f in toDeselect)
            {
                ForceDeselect(f);
            }
        }

        //selection
        if(toSelect.Count == 0)
        {
            return;
        }

        foreach(FolderObject f in toSelect)
        {
            if(f)
            {
                f.OnSelected();
                folderSelection.Add(f);
            }
        }
        StopAllCoroutines();
        StartCoroutine(LoadTexturesRange(toSelect.ToArray()));
    }

    /// <summary>
    /// Loads a range of texture folders.
    /// </summary>
    /// <param name="children">Folder objects to load.</param>
    /// <returns></returns>
    private IEnumerator LoadTexturesRange(FolderObject[] folders)
    {
        Popup.Instance.Display("");
        gridView.SetActive(false);

        for (int i = 0; i < folders.Length; i++)
        {
            List<TextureObject> preload = new List<TextureObject>();
            preload.AddRange(textureObjects);
            if (!folders[i])
            {
                //skip mod folder name
                continue;
            }
            loadingRangeResume = false;
            StartCoroutine(LoadTextures(folders[i].textureFolder, folders[i]));
            while (!loadingRangeResume)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        Popup.Instance.UpdateLabel("Generating texture grid...");
        yield return new WaitForEndOfFrame();

        //apply current regex string and sort the textures
        OnSearchStringChanged(searchField.text);
        SortTextures(currentSortingMethod);

        gridView.SetActive(true);
        Popup.Instance.Hide();
    }

    /// <summary>
    /// Loads all textures in a folder.
    /// </summary>
    /// <param name="folder">Folder to load textures from.</param>
    /// <param name="folderObject">Folder object.</param>
    private IEnumerator LoadTextures(TextureFolder folder, FolderObject folderObject)
    {
        int i = 0;

        //compute yield step
        int accuracy = folder.Textures.Count / 15;
        accuracy = Mathf.Clamp(accuracy, 1, 8);

        //update the popup
        Popup.Instance.UpdateLabel("Loading folder: " + folder.Name + "\n");

        if (!RestoreFromCache(folderObject))
        {
            foreach (Q2Texture q2t in folder.Textures)
            {
                //update popup text
                Popup.Instance.UpdateLabel("Loading folder: " + folder.Name + "\n" + ((float)(i++) / folder.Textures.Count * 100f).ToString("F0") + "%");

                //load textures
                TextureObject to = texturePool.Get<TextureObject>();
                to.LoadTextureFile(q2t);
                SetTextureType(to);
                to.gameObject.SetActive(false);

                textureObjects.Add(to);
                folderObject.textures.Add(to);

                //yield if step is reached
                if(i % accuracy == 0)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            folderObject.SetCount();
        }

        loadingRangeResume = true;
        textureGrid.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sort textures click action.
    /// </summary>
    /// <param name="method">Method integer corresponding to TextureSortingMethod enum.</param>
    public void SortTextures(int method)
    {
        SortTextures((TextureSortingMethod)method);
    }

    /// <summary>
    /// Sorts the textures using provided method.
    /// </summary>
    /// <param name="method">Texture sorting method.</param>
    private void SortTextures(TextureSortingMethod method)
    {
        currentSortingMethod = method;

        if (method == TextureSortingMethod.alphabetical)
        {
            textureObjects = textureObjects.OrderBy(i => i.Q2Texture.Name).ToList();
        }
        else if(method == TextureSortingMethod.size)
        {
            textureObjects = textureObjects.OrderBy(i => i.Q2Texture.FileSize).ToList();
        }
        else if(method == TextureSortingMethod.modification_time)
        {
            textureObjects = textureObjects.OrderBy(i => i.Q2Texture.ModificationDate).ToList();
        }

        for (int i = 0; i < textureObjects.Count; i++)
        {
            textureObjects[i].GetComponent<Transform>().SetAsLastSibling();
        }
    }

    /// <summary>
    /// Set texture types click action.
    /// </summary>
    /// <param name="type">Image type corresponding to TextureImageType enum.</param>
    public void SetTextureTypes(int type)
    {
        currentImageType ^= (TextureImageType)type;
        SetAllTextureTypes();

        SetTextureTypeButtonColors();
    }

    /// <summary>
    /// Sets WAL and PNG/JPG button colors.
    /// </summary>
    private void SetTextureTypeButtonColors()
    {
        if (currentImageType.HasFlag(TextureImageType.jpgpng))
        {
            pngjpgBtnImage.color = Color.green;
        }
        else
        {
            pngjpgBtnImage.color = Color.white;
        }

        if (currentImageType.HasFlag(TextureImageType.wal))
        {
            walBtnImage.color = Color.green;
        }
        else
        {
            walBtnImage.color = Color.white;
        }
    }

    /// <summary>
    /// Applies texture type to all texture objects previews or hides them.
    /// </summary>
    private void SetAllTextureTypes()
    {
        for(int i = 0; i < textureObjects.Count; i++)
        {
            SetTextureType(textureObjects[i]);
        }
    }

    /// <summary>
    /// Sets texture type for a texture object so the thumbnail represents correct texture type.
    /// If the type is PNG/JPG only then .wal textures will be hidden.
    /// </summary>
    /// <param name="o">Texture object to be set.</param>
    private void SetTextureType(TextureObject o)
    {
        if (!currentImageType.HasFlag(TextureImageType.wal))
        {
            //no wal textures, disable
            o.gameObject.SetActive(false);
            o.typeUnmatched = true;

            if ((!o.Q2Texture.HasJPG && !o.Q2Texture.HasPNG) || !currentImageType.HasFlag(TextureImageType.jpgpng))
            {
                //doesn't have a png or jpg version
                return;
            }
        }

        //obey the regex pattern status for this texture
        Debug.Assert(!o.isCached, "Found a cached texture!", o);
        if (!o.regexUnmatched && !o.isCached)
        {
            o.gameObject.SetActive(true);
        }
        o.typeUnmatched = false;

        //apply the correct thumbnail
        o.SetWalImage(currentImageType);
    }
}
