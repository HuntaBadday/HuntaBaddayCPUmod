using System;
using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using HuntaBaddayCPUmod.CustomData;
using LogicUI.MenuParts;
using LogicUI.MenuParts.TextResizing;
using LogicWorld.UI;
using TMPro;
using UnityEngine.Events;

namespace HuntaBaddayCPUmod;

public class AddressDecoderMenu : EditComponentMenu<IAddressDecoderData>, IAssignMyFields {
    public static void Init() {
        WS.window("Address Decoder Menu")
            .setYPosition(150)
            .configureContent(content => content
                .layoutVertical()
                .addContainer("StartAddrBox", container => container
                    .layoutGrowGapVerticalInner()
                    .add(WS.textLine.setLocalizationKey("HuntaBaddayCPUmod.AddrContMenuStart"))
                    .add(WS.inputField
                        .injectionKey(nameof(startAddress)))
                        .fixedSize(500, 150)
                )
                .addContainer("EndAddrBox", container => container
                    .layoutGrowGapVerticalInner()
                    .add(WS.textLine.setLocalizationKey("HuntaBaddayCPUmod.AddrContMenuEnd"))
                    .add(WS.inputField
                        .injectionKey(nameof(endAddress)))
                        .fixedSize(500, 150)
                )
            )
            .add<AddressDecoderMenu>()
            .build();
    }
    
    [AssignMe]
    public TMP_InputField startAddress;
    [AssignMe]
    public TMP_InputField endAddress;

    protected override void OnStartEditing() {
        string s = FirstComponentBeingEdited.Data.StartAddressText;
        string e = FirstComponentBeingEdited.Data.EndAddressText;
        startAddress.SetTextWithoutNotify(s);
        endAddress.SetTextWithoutNotify(e);
    }

    public override void Initialize() {
        base.Initialize();
        startAddress.onValueChanged.AddListener(startAddressChanged);
        endAddress.onValueChanged.AddListener(endAddressChanged);
    }
    
    private void startAddressChanged(string text) {
        FirstComponentBeingEdited.Data.StartAddressText = text;
        
        ushort x;
        try {
            x = (ushort)(text.Contains("0x")
                ? Convert.ToInt32(text, 16)
                : Convert.ToInt32(text));
        } catch (FormatException) {
            x = 0;
        }
        FirstComponentBeingEdited.Data.StartAddress = x;
    }
    
    private void endAddressChanged(string text) {
        FirstComponentBeingEdited.Data.EndAddressText = text;
        
        ushort x;
        try {
            x = (ushort)(text.Contains("0x")
                ? Convert.ToInt32(text, 16)
                : Convert.ToInt32(text));
        } catch (FormatException) {
            x = 0;
        }
        FirstComponentBeingEdited.Data.EndAddress = x;
    }
}