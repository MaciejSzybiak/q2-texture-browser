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

using UnityEngine;
using TMPro;

/// <summary>
/// Manages the popup window.
/// </summary>
public class Popup : MonoBehaviour
{
    #region unity properties
    [SerializeField]
    private TMP_Text Label = default;
    [SerializeField]
    private GameObject ThinkImage = default;
    [SerializeField]
    private Canvas popupCanvas = default;
    #endregion

    private float thinkRotation = 0f;

    #region singleton

    public static Popup Instance { get; private set; }

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

        Hide();
    }

    #endregion

    void Update()
    {
        //update thinker's rotation
        thinkRotation += 100f * Time.deltaTime;
        if(thinkRotation > 360)
        {
            thinkRotation -= 360;
        }
        ThinkImage.transform.localRotation = Quaternion.Euler(0f, 0f, -thinkRotation);
    }

    /// <summary>
    /// Displays the popup and sets its label.
    /// </summary>
    /// <param name="label">The popup label to be set.</param>
    public void Display(string label)
    {
        UpdateLabel(label);
        Display();
    }

    /// <summary>
    /// Displays the popup.
    /// </summary>
    public void Display()
    {
        gameObject.SetActive(true);
        popupCanvas.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the popup.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        popupCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates popup label.
    /// </summary>
    /// <param name="label"></param>
    public void UpdateLabel(string label)
    {
        Label.text = label;
    }
}
