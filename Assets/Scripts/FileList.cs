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
using System.IO;
using System;
using UnityEngine;

public class FileList : MonoBehaviour
{
    #region singleton

    public static FileList Instance { get; private set; }

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

    public List<ModFolder> ModFolders { get; private set; }

    /// <summary>
    /// Triggers loading folders structure when path is changed.
    /// </summary>
    /// <param name="path">Source path.</param>
    public void OnGamePathSet(string path)
    {
        DestroyFolders();
        StopAllCoroutines();
        StartCoroutine(ReadFolderStructure(path));
    }

    /// <summary>
    /// Reads game folder structure and finds all the textures present in it.
    /// </summary>
    /// <param name="path">Path to read.</param>
    private IEnumerator ReadFolderStructure(string path)
    {
        //display the popup
        Popup.Instance.Display("Reading directories...");

        if (ModFolders == null)
        {
            ModFolders = new List<ModFolder>();
        }

        //read mod folders
        string[] list = GetSubDirectories(path);
        Array.Sort(list);
        string entry;

        for (int i = 0; i < list.Length; i++)
        {
            entry = list[i] + "/textures";

            if (Directory.Exists(entry))
            {
                //mod has the "textures" folder
                int index = list[i].LastIndexOf("/") + 1;
                ModFolder mf = new ModFolder(list[i].Substring(index), entry);

                ModFolders.Add(mf);
            }
            //let the popup refresh every 10 folders. This rarely happens.
            if (i % 10 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        //read mod folders contents

        string[] fileList;
        string texName;
        string noExtensionName;

        for(int i = 0; i < ModFolders.Count; i++)
        {
            list = GetSubDirectories(ModFolders[i].Path);
            Array.Sort(list);

            //calculate yield step
            int accuracy = list.Length / 20;
            accuracy = Mathf.Clamp(accuracy, 1, 10);

            //read current mod folder's texture subfolders
            for (int j = 0; j < list.Length; j++)
            {
                //update popup text
                Popup.Instance.UpdateLabel("Reading directory: " + ModFolders[i].Name + " " + ((float)j / list.Length * 100f).ToString("F0") + "%");

                fileList = Directory.GetFiles(list[j], "*.wal", SearchOption.TopDirectoryOnly);
                if(fileList.Length == 0)
                {
                    //no files in this folder
                    continue;
                }

                TextureFolder tf = new TextureFolder(list[j].Substring(list[j].LastIndexOf("/") + 1), list[j]);

                ModFolders[i].AddTextureFolder(tf);

                //read textures inside this filder
                for(int k = 0; k < fileList.Length; k++)
                {
                    fileList[k] = fileList[k].Replace('\\', '/');
                    noExtensionName = fileList[k].Substring(0, fileList[k].Length - 4);
                    texName = noExtensionName.Substring(noExtensionName.LastIndexOf("/") + 1);

                    Q2Texture tex = new Q2Texture(texName, noExtensionName,
                        File.Exists(noExtensionName + ".jpg"),
                        File.Exists(noExtensionName + ".png"));

                    tf.AddTexture(tex);
                }

                //yield each time the step was reached
                if(j % accuracy == 0)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        //hide the popup
        Popup.Instance.Hide();
    }

    /// <summary>
    /// Get all subdirectories for the given path.
    /// </summary>
    /// <param name="path">Destination path.</param>
    /// <returns>Array of folder names.</returns>
    private string[] GetSubDirectories(string path)
    {
        string[] list = Directory.GetDirectories(path);

        for(int i = 0; i < list.Length; i++)
        {
            list[i] = list[i].Replace('\\', '/');
        }

        return list;
    }

    /// <summary>
    /// Clears mod folders list.
    /// </summary>
    private void DestroyFolders()
    {
        if (ModFolders != null)
        {
            ModFolders.Clear();
        }
    }
}
