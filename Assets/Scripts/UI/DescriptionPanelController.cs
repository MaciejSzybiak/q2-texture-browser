﻿/**
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
using UnityEngine.EventSystems;

/// <summary>
/// Controls tooltip panels for right panel buttons.
/// </summary>
public class DescriptionPanelController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject descriptionPanel = default;

    public void OnPointerEnter(PointerEventData eventData)
    {
        //activate the panel
        descriptionPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //deactivate the panel
        descriptionPanel.SetActive(false);
    }
}
