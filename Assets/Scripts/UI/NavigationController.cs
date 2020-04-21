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

/// <summary>
/// Controls panel naviagtion by button and the ESC key.
/// </summary>
public class NavigationController : MonoBehaviour
{
    #region unity properties
    [Header("Panels")]
    [SerializeField]
    private GameObject BrowserCanvas = default;
    [SerializeField]
    private GameObject PopupCanvas = default;
    [SerializeField]
    private GameObject SortPanel = default;
    [SerializeField]
    private GameObject TextureViewCanvas = default;
    #endregion

    //back actions are stored in a stack
    private delegate void BackAction();
    private Stack<BackAction> backActions = new Stack<BackAction>();

    #region singleton

    public static NavigationController Instance { get; private set; }
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

        //let the popup initialize itself
        PopupCanvas.SetActive(true);
    }

    #endregion

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !PopupCanvas.activeSelf && backActions.Count > 0)
        {
            //undo last panel change action
            backActions.Pop().Invoke();
        }
    }

    /// <summary>
    /// Handles application quit action.
    /// </summary>
    public void QuitClick()
    {
        Application.Quit();
    }

    #region "to" actions
    //reversible actions

    /// <summary>
    /// Action for opening the texture browser.
    /// </summary>
    public void To_BrowserCanvasFromMenu()
    {
        if (!GamePathManager.IsGamePathCorrect)
        {
            //don't do it if the game path is invalid
            return;
        }
        BrowserCanvas.SetActive(true);
        backActions.Push(Back_FromBrowserToMenu); //register back action
    }

    /// <summary>
    /// Action for opening texture sorting panel.
    /// </summary>
    public void To_SortPanel()
    {
        SortPanel.SetActive(true);
        backActions.Push(Back_FromSortPanel);
    }

    /// <summary>
    /// Action for opening texture view panel.
    /// </summary>
    public void To_TextureView()
    {
        TextureViewCanvas.SetActive(true);
        backActions.Push(Back_FromTextureView);
    }

    #endregion

    #region back actions
    //action reverse actions

    /// <summary>
    /// Deactivates texture browser panel.
    /// </summary>
    public void Back_FromBrowserToMenu()
    {
        BrowserCanvas.SetActive(false);
    }

    /// <summary>
    /// Deactivates sorting panel and pops itself if it was called manually from code.
    /// </summary>
    public void Back_FromSortPanel()
    {
        SortPanel.SetActive(false);

        if(backActions.Peek() == Back_FromSortPanel)
        {
            backActions.Pop();
        }
    }

    /// <summary>
    /// Deactivates texture view panel and pops itself if it was called manually from code.
    /// </summary>
    public void Back_FromTextureView()
    {
        TextureViewCanvas.SetActive(false);

        if (backActions.Peek() == Back_FromSortPanel)
        {
            backActions.Pop();
        }
    }

    #endregion
}
